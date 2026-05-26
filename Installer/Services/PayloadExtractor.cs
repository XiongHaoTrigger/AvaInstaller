using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Reflection;
using Installer.Models;

namespace Installer.Services;

public sealed class PayloadExtractor : IPayloadExtractor
{
    private const string PayloadResourceName = "Installer.Resources.payload.zip";
    private const int BufferSize = 128 * 1024;

    public async Task ExtractAsync(
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
        }

        progress.Report(new InstallProgress(100, string.Empty, requiredBytes, requiredBytes));
    }

    private static Stream OpenPayloadStream()
    {
        var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(PayloadResourceName);
        if (stream is null)
        {
            throw new FileNotFoundException(
                $"Embedded payload resource '{PayloadResourceName}' was not found. Run build-installer.ps1 before publishing.");
        }

        return stream;
    }

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
}
