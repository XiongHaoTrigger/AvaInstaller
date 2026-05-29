using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using AvaInstaller.Models;

namespace AvaInstaller.Services;

/// <summary>
/// Payload 解压服务实现。
/// 从与安装器同目录的 payload.zip 中提取文件到目标安装目录。
/// 
/// 安全特性：
/// - 解压前检查目标磁盘剩余空间（含 64MB 安全余量）
/// - 路径穿越防护（拒绝 zip 包中的 ../ 类路径）
/// - Bootstrapper 模式：payload.zip 与安装器 exe 分离存储
/// </summary>
public sealed class PayloadExtractor : IPayloadExtractor
{
    /// <summary>Payload 文件名</summary>
    private const string PayloadFileName = "payload.zip";

    /// <summary>文件复制缓冲区大小（128KB）</summary>
    private const int BufferSize = 128 * 1024;

    /// <inheritdoc />
    /// <remarks>
    /// Bootstrapper 模式：安装器 exe 与 payload.zip 分离，
    /// 避免大资源嵌入导致 CSC 编译失败。
    /// </remarks>
    public async Task<PayloadExtractionResult> ExtractAsync(
        string targetDirectory,
        IProgress<InstallProgress> progress,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(targetDirectory))
        {
            throw new InvalidOperationException("The installation directory is empty.");
        }

        var normalizedTarget = Path.GetFullPath(targetDirectory);
        Directory.CreateDirectory(normalizedTarget);

        // 打开 payload.zip 流
        await using var payloadStream = OpenPayloadStream();
        using var archive = new ZipArchive(payloadStream, ZipArchiveMode.Read, leaveOpen: false);

        var entries = archive.Entries
            .Where(entry => !string.IsNullOrEmpty(entry.Name))
            .ToArray();
        var extractedFiles = new List<string>();
        var extractedDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 解压前先估算目标盘空间，给 64MB 安全余量，避免安装到一半才失败
        var requiredBytes = entries.Sum(entry => Math.Max(0, entry.Length));
        EnsureEnoughDiskSpace(normalizedTarget, requiredBytes);

        long extractedBytes = 0;
        progress.Report(new InstallProgress(0, string.Empty, 0, requiredBytes));

        // 逐个解压 zip 条目
        foreach (var entry in entries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var destinationPath = GetSafeDestinationPath(normalizedTarget, entry.FullName);
            var destinationDirectory = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }

            await using var source = entry.Open();
            await using var destination = new FileStream(
                destinationPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                BufferSize,
                useAsync: true);

            var buffer = new byte[BufferSize];
            int read;
            while ((read = await source.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
            {
                await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                extractedBytes += read;

                progress.Report(new InstallProgress(
                    CalculatePercent(extractedBytes, requiredBytes),
                    entry.FullName,
                    extractedBytes,
                    requiredBytes));
            }

            // 恢复原始修改时间
            File.SetLastWriteTime(destinationPath, entry.LastWriteTime.LocalDateTime);
            var relativeFile = NormalizeRelativePath(entry.FullName);
            extractedFiles.Add(relativeFile);
            AddParentDirectories(relativeFile, extractedDirectories);
        }

        progress.Report(new InstallProgress(100, string.Empty, requiredBytes, requiredBytes));
        return new PayloadExtractionResult(
            extractedFiles.Order(StringComparer.OrdinalIgnoreCase).ToArray(),
            extractedDirectories.Order(StringComparer.OrdinalIgnoreCase).ToArray());
    }

    /// <summary>
    /// 打开 payload.zip 文件流。
    /// payload.zip 必须与安装器 exe 位于同一目录。
    /// </summary>
    /// <exception cref="FileNotFoundException">未找到 payload.zip 时抛出</exception>
    private static Stream OpenPayloadStream()
    {
        // payload.zip 必须与安装器 exe 位于同一目录
        // 例如：dist\MyAvaloniaAppInstaller.exe + dist\payload.zip
        var payloadPath = Path.Combine(AppContext.BaseDirectory, PayloadFileName);
        if (!File.Exists(payloadPath))
        {
            throw new FileNotFoundException(
                $"Payload file '{PayloadFileName}' was not found next to the installer. Expected path: {payloadPath}");
        }

        return new FileStream(
            payloadPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            BufferSize,
            useAsync: true);
    }

    /// <summary>
    /// 计算安全的解压目标路径，防止路径穿越攻击。
    /// 拒绝 zip 包中包含 ..\ 之类路径穿越条目的文件。
    /// </summary>
    /// <param name="targetDirectory">目标安装目录</param>
    /// <param name="entryName">zip 条目名称（相对路径）</param>
    /// <returns>安全的目标文件路径</returns>
    /// <exception cref="InvalidDataException">检测到不安全路径时抛出</exception>
    private static string GetSafeDestinationPath(string targetDirectory, string entryName)
    {
        var destinationPath = Path.GetFullPath(Path.Combine(targetDirectory, entryName));
        var safeRoot = targetDirectory.EndsWith(Path.DirectorySeparatorChar)
            ? targetDirectory
            : targetDirectory + Path.DirectorySeparatorChar;

        if (!destinationPath.StartsWith(safeRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException($"The payload contains an unsafe path: {entryName}");
        }

        return destinationPath;
    }

    /// <summary>
    /// 计算解压进度百分比。
    /// </summary>
    /// <param name="extractedBytes">已解压字节数</param>
    /// <param name="totalBytes">总字节数</param>
    /// <returns>0-100 的百分比</returns>
    private static int CalculatePercent(long extractedBytes, long totalBytes)
    {
        if (totalBytes <= 0)
        {
            return 100;
        }

        return (int)Math.Clamp(extractedBytes * 100 / totalBytes, 0, 100);
    }

    /// <summary>
    /// 检查目标磁盘剩余空间是否足够。
    /// 需要空间 = payload 大小 + 64MB 安全余量。
    /// </summary>
    /// <param name="targetDirectory">目标目录</param>
    /// <param name="requiredBytes">payload 文件总大小</param>
    /// <exception cref="IOException">磁盘空间不足时抛出</exception>
    private static void EnsureEnoughDiskSpace(string targetDirectory, long requiredBytes)
    {
        if (!TryGetDriveInfo(targetDirectory, out var driveInfo) || !driveInfo.IsReady)
        {
            return;
        }

        const long safetyMarginBytes = 64L * 1024 * 1024; // 64MB 安全余量
        if (driveInfo.AvailableFreeSpace < requiredBytes + safetyMarginBytes)
        {
            throw new IOException(
                $"Not enough disk space. Required: {FormatBytes(requiredBytes + safetyMarginBytes)}, " +
                $"available: {FormatBytes(driveInfo.AvailableFreeSpace)}.");
        }
    }

    /// <summary>
    /// 尝试获取指定路径所在的驱动器信息。
    /// </summary>
    /// <param name="path">文件路径</param>
    /// <param name="driveInfo">输出的驱动器信息</param>
    /// <returns>成功获取返回 true</returns>
    private static bool TryGetDriveInfo(string path, [NotNullWhen(true)] out DriveInfo? driveInfo)
    {
        try
        {
            var root = Path.GetPathRoot(path);
            driveInfo = string.IsNullOrWhiteSpace(root) ? null : new DriveInfo(root);
            return driveInfo is not null;
        }
        catch
        {
            driveInfo = null;
            return false;
        }
    }

    /// <summary>
    /// 格式化字节数为人类可读格式（B/KB/MB/GB/TB）。
    /// </summary>
    /// <param name="bytes">字节数</param>
    /// <returns>格式化的字符串，如 "128.5 MB"</returns>
    private static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        var value = (double)bytes;
        var unit = 0;

        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return $"{value:0.##} {units[unit]}";
    }

    /// <summary>
    /// 规范化相对路径。
    /// 统一使用 DirectorySeparatorChar，去除首尾分隔符。
    /// </summary>
    private static string NormalizeRelativePath(string path)
    {
        return path
            .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)
            .Trim(Path.DirectorySeparatorChar);
    }

    /// <summary>
    /// 将文件的所有父级目录添加到目录集合中。
    /// </summary>
    /// <param name="relativeFile">文件相对路径</param>
    /// <param name="directories">目录集合</param>
    private static void AddParentDirectories(string relativeFile, HashSet<string> directories)
    {
        var directory = Path.GetDirectoryName(relativeFile);
        while (!string.IsNullOrWhiteSpace(directory))
        {
            directories.Add(directory);
            directory = Path.GetDirectoryName(directory);
        }
    }
}
