using System.Diagnostics;
using AvaInstaller.Models;

namespace AvaInstaller.Services;

/// <summary>
/// 卸载服务。
/// 负责完整的卸载流程：
/// 1. 读取安装清单（install-manifest.json）
/// 2. 检查目标应用是否仍在运行（避免文件占用）
/// 3. 删除快捷方式
/// 4. 删除文件（跳过 Preserve 列表中的项）
/// 5. 删除空目录
/// 6. 清理安装状态文件
/// 7. 调度自删除脚本（延迟删除卸载器自身）
/// 
/// 整个过程记录到日志文件，便于排查问题。
/// </summary>
public sealed class UninstallService
{
    private readonly InstallManifestService _manifestService;
    private readonly InstallStateService _stateService;
    private readonly ShortcutService _shortcutService;
    private readonly SelfDeleteService _selfDeleteService;

    /// <summary>
    /// 创建卸载服务实例。
    /// </summary>
    /// <param name="manifestService">安装清单读写服务</param>
    /// <param name="stateService">安装状态读写服务</param>
    /// <param name="shortcutService">快捷方式服务</param>
    /// <param name="selfDeleteService">自删除服务</param>
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

    /// <summary>
    /// 执行异步卸载。
    /// </summary>
    /// <param name="manifestPath">安装清单文件路径</param>
    /// <param name="preserveUserData">是否保留用户数据（Preserve 列表中的文件/目录）</param>
    /// <param name="silent">是否静默卸载（不影响删除流程，仅影响日志详细程度）</param>
    /// <param name="progress">进度报告回调（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>卸载结果，包含成功/失败状态和日志路径</returns>
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

        // 读取安装清单
        InstallManifest manifest;
        try
        {
            manifest = await _manifestService.ReadAsync(manifestPath, cancellationToken);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
                                        or InvalidDataException or System.Text.Json.JsonException)
        {
            log.Error("Failed to read install manifest.", ex);
            return new UninstallResult(false, $"Unable to read install manifest: {manifestPath}", log.LogPath);
        }

        // 检查目标应用是否仍在运行
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

        // 分步骤执行卸载，每个阶段报告进度
        progress?.Report(new UninstallProgress(10, "Removing shortcuts..."));
        DeleteShortcuts(manifest, log);

        progress?.Report(new UninstallProgress(30, "Removing files..."));
        DeleteFiles(manifest, preserve, log);

        progress?.Report(new UninstallProgress(65, "Removing directories..."));
        DeleteDirectories(manifest, preserve, log);

        progress?.Report(new UninstallProgress(80, "Removing installer state..."));
        TryDeleteFile(manifestPath, log, "manifest");
        TryDeleteState(manifest.AppId, log);

        // 收集需要延迟删除的残留文件
        var uninstallerPath = Path.Combine(installLocation, manifest.Uninstaller);
        var delayedDeletePaths = manifest.Files
            .Select(file => Path.Combine(installLocation, file))
            .Where(File.Exists)
            .Where(path => !path.Equals(uninstallerPath, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        // 启动自删除脚本
        progress?.Report(new UninstallProgress(90, "Scheduling cleanup..."));
        _selfDeleteService.ScheduleSelfDelete(uninstallerPath, installLocation, delayedDeletePaths);

        progress?.Report(new UninstallProgress(100, "Uninstall completed."));
        log.Info("Uninstall completed.");
        return new UninstallResult(true, null, log.LogPath);
    }

    /// <summary>
    /// 检查目标应用的主进程是否仍在运行。
    /// 排除当前进程（卸载器自身）以免误判。
    /// </summary>
    /// <param name="manifest">安装清单</param>
    /// <returns>目标应用是否在运行</returns>
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

    /// <summary>
    /// 删除清单中记录的所有快捷方式。
    /// 单个快捷方式删除失败不影响后续删除。
    /// </summary>
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

    /// <summary>
    /// 删除清单中记录的所有文件。
    /// Preserve 列表中的文件会被跳过。
    /// </summary>
    private static void DeleteFiles(
        InstallManifest manifest, IReadOnlyList<string> preserve, UninstallLogService log)
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

    /// <summary>
    /// 删除清单中记录的所有目录。
    /// 按路径长度降序删除（先深层后浅层），
    /// 只删除空目录（非空则跳过）。
    /// Preserve 列表中的目录会被跳过。
    /// </summary>
    private static void DeleteDirectories(
        InstallManifest manifest, IReadOnlyList<string> preserve, UninstallLogService log)
    {
        // 按路径长度降序：先删除深层目录
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

    /// <summary>
    /// 尝试删除安装状态文件。
    /// </summary>
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

    /// <summary>
    /// 尝试删除单个文件，失败时记录错误日志（不中断流程）。
    /// </summary>
    /// <param name="path">文件路径</param>
    /// <param name="log">日志服务</param>
    /// <param name="kind">文件类型描述（如 "file"、"manifest"）</param>
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

    /// <summary>
    /// 尝试删除空目录。
    /// 目录存在但非空时记录跳过日志（不报错）。
    /// </summary>
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

    /// <summary>
    /// 判断相对路径是否在保留列表中。
    /// 支持精确匹配和前缀匹配（目录下所有内容都被保留）。
    /// </summary>
    /// <param name="relativePath">要检查的相对路径</param>
    /// <param name="preserve">保留列表</param>
    /// <returns>是否应保留</returns>
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
}
