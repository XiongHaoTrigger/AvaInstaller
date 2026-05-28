using System.Diagnostics;
using AvaInstaller.Models;

namespace AvaInstaller.Services;

public sealed class UninstallService
{
    private readonly InstallManifestService _manifestService;
    private readonly InstallStateService _stateService;
    private readonly ShortcutService _shortcutService;
    private readonly SelfDeleteService _selfDeleteService;

    public UninstallService(
        InstallManifestService manifestService,
        InstallStateService stateService,
        ShortcutService shortcutService,
        SelfDeleteService selfDeleteService)
    {
        _manifestService = manifestService;
        _stateService = stateService;
        _shortcutService = shortcutService;
        _selfDeleteService = selfDeleteService;
    }

    public async Task<UninstallResult> UninstallAsync(
        string manifestPath,
        bool preserveUserData,
        bool silent,
        IProgress<UninstallProgress>? progress,
        CancellationToken cancellationToken)
    {
        var log = new UninstallLogService();
        log.Info("Uninstall started.");
        log.Info($"Manifest path: {manifestPath}");

        InstallManifest manifest;
        try
        {
            manifest = await _manifestService.ReadAsync(manifestPath, cancellationToken);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or System.Text.Json.JsonException)
        {
            log.Error("Failed to read install manifest.", ex);
            return new UninstallResult(false, $"Unable to read install manifest: {manifestPath}", log.LogPath);
        }

        if (IsMainProcessRunning(manifest))
        {
            const string message = "The application is still running. Please close it before uninstalling.";
            log.Error(message);
            return new UninstallResult(false, message, log.LogPath);
        }

        var installLocation = manifest.InstallLocation;
        var preserve = preserveUserData
            ? manifest.Preserve
            : [];

        progress?.Report(new UninstallProgress(10, "Removing shortcuts..."));
        DeleteShortcuts(manifest, log);

        progress?.Report(new UninstallProgress(30, "Removing files..."));
        DeleteFiles(manifest, preserve, log);

        progress?.Report(new UninstallProgress(65, "Removing directories..."));
        DeleteDirectories(manifest, preserve, log);

        progress?.Report(new UninstallProgress(80, "Removing installer state..."));
        TryDeleteFile(manifestPath, log, "manifest");
        TryDeleteState(manifest.AppId, log);

        var uninstallerPath = Path.Combine(installLocation, manifest.Uninstaller);
        var delayedDeletePaths = manifest.Files
            .Select(file => Path.Combine(installLocation, file))
            .Where(File.Exists)
            .Where(path => !path.Equals(uninstallerPath, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        progress?.Report(new UninstallProgress(90, "Scheduling cleanup..."));
        _selfDeleteService.ScheduleSelfDelete(uninstallerPath, installLocation, delayedDeletePaths);

        progress?.Report(new UninstallProgress(100, "Uninstall completed."));
        log.Info("Uninstall completed.");
        return new UninstallResult(true, null, log.LogPath);
    }

    private static bool IsMainProcessRunning(InstallManifest manifest)
    {
        var processName = Path.GetFileNameWithoutExtension(manifest.MainExe);
        if (string.IsNullOrWhiteSpace(processName))
        {
            return false;
        }

        var currentProcessId = Environment.ProcessId;
        return Process.GetProcessesByName(processName)
            .Any(process => process.Id != currentProcessId);
    }

    private void DeleteShortcuts(InstallManifest manifest, UninstallLogService log)
    {
        foreach (var shortcut in manifest.Shortcuts)
        {
            try
            {
                _shortcutService.DeleteShortcut(shortcut);
                log.Info($"Deleted shortcut: {shortcut}");
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                log.Error($"Failed to delete shortcut: {shortcut}", ex);
            }
        }
    }

    private static void DeleteFiles(InstallManifest manifest, IReadOnlyList<string> preserve, UninstallLogService log)
    {
        foreach (var file in manifest.Files)
        {
            if (IsPreserved(file, preserve))
            {
                log.Info($"Skipped preserved file: {file}");
                continue;
            }

            var path = Path.Combine(manifest.InstallLocation, file);
            TryDeleteFile(path, log, "file");
        }
    }

    private static void DeleteDirectories(InstallManifest manifest, IReadOnlyList<string> preserve, UninstallLogService log)
    {
        foreach (var directory in manifest.Directories.OrderByDescending(path => path.Length))
        {
            if (IsPreserved(directory, preserve))
            {
                log.Info($"Skipped preserved directory: {directory}");
                continue;
            }

            var path = Path.Combine(manifest.InstallLocation, directory);
            TryDeleteEmptyDirectory(path, log);
        }
    }

    private void TryDeleteState(string appId, UninstallLogService log)
    {
        try
        {
            _stateService.Delete(appId);
            log.Info("Deleted install state.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            log.Error("Failed to delete install state.", ex);
        }
    }

    private static void TryDeleteFile(string path, UninstallLogService log, string kind)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
                log.Info($"Deleted {kind}: {path}");
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            log.Error($"Failed to delete {kind}: {path}", ex);
        }
    }

    private static void TryDeleteEmptyDirectory(string path, UninstallLogService log)
    {
        try
        {
            if (Directory.Exists(path) && !Directory.EnumerateFileSystemEntries(path).Any())
            {
                Directory.Delete(path);
                log.Info($"Deleted directory: {path}");
            }
            else if (Directory.Exists(path))
            {
                log.Info($"Skipped non-empty directory: {path}");
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            log.Error($"Failed to delete directory: {path}", ex);
        }
    }

    private static bool IsPreserved(string relativePath, IReadOnlyList<string> preserve)
    {
        var normalized = NormalizeRelativePath(relativePath);
        return preserve.Any(item =>
        {
            var preserved = NormalizeRelativePath(item);
            return normalized.Equals(preserved, StringComparison.OrdinalIgnoreCase) ||
                   normalized.StartsWith(preserved + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        });
    }

    private static string NormalizeRelativePath(string path)
    {
        return path
            .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)
            .Trim(Path.DirectorySeparatorChar);
    }
}
