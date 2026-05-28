using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using AvaInstaller.Models;

namespace AvaInstaller.Services;

public sealed class PayloadExtractor : IPayloadExtractor
{
    private const string PayloadFileName = "payload.zip";
    private const int BufferSize = 128 * 1024;

    // Bootstrapper 模式：安装器 exe 与 payload.zip 分离，避免大资源嵌入导致 CSC 编译失败。
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

        await using var payloadStream = OpenPayloadStream();
        using var archive = new ZipArchive(payloadStream, ZipArchiveMode.Read, leaveOpen: false);

        var entries = archive.Entries
            .Where(entry => !string.IsNullOrEmpty(entry.Name))
            .ToArray();
        var extractedFiles = new List<string>();
        var extractedDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 解压前先估算目标盘空间，给 64MB 安全余量，避免安装到一半才失败。
        var requiredBytes = entries.Sum(entry => Math.Max(0, entry.Length));
        EnsureEnoughDiskSpace(normalizedTarget, requiredBytes);

        long extractedBytes = 0;
        progress.Report(new InstallProgress(0, string.Empty, 0, requiredBytes));

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

    private static Stream OpenPayloadStream()
    {
        // payload.zip 必须与安装器 exe 位于同一目录：dist\MyAvaloniaAppInstaller.exe + dist\payload.zip。
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

    private static string GetSafeDestinationPath(string targetDirectory, string entryName)
    {
        // 防止 zip 内出现 ..\ 之类路径穿越条目，把文件写到安装目录之外。
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

    private static int CalculatePercent(long extractedBytes, long totalBytes)
    {
        if (totalBytes <= 0)
        {
            return 100;
        }

        return (int)Math.Clamp(extractedBytes * 100 / totalBytes, 0, 100);
    }

    private static void EnsureEnoughDiskSpace(string targetDirectory, long requiredBytes)
    {
        if (!TryGetDriveInfo(targetDirectory, out var driveInfo) || !driveInfo.IsReady)
        {
            return;
        }

        const long safetyMarginBytes = 64L * 1024 * 1024;
        if (driveInfo.AvailableFreeSpace < requiredBytes + safetyMarginBytes)
        {
            throw new IOException(
                $"Not enough disk space. Required: {FormatBytes(requiredBytes + safetyMarginBytes)}, available: {FormatBytes(driveInfo.AvailableFreeSpace)}.");
        }
    }

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

    private static string NormalizeRelativePath(string path)
    {
        return path
            .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)
            .Trim(Path.DirectorySeparatorChar);
    }

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
