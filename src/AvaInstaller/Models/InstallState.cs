namespace AvaInstaller.Models;

/// <summary>
/// 安装状态记录。
/// 存储在用户级固定位置（%LocalAppData%\AvaInstaller\InstalledApps\{AppId}\install-state.json），
/// 安装器再次启动时依赖它判断是否已安装，不写入注册表。
/// </summary>
public sealed record InstallState
{
    /// <summary>应用程序名称</summary>
    public string AppName { get; init; } = string.Empty;

    /// <summary>应用程序唯一标识符</summary>
    public string AppId { get; init; } = string.Empty;

    /// <summary>安装的版本号</summary>
    public string Version { get; init; } = string.Empty;

    /// <summary>安装目录的绝对路径</summary>
    public string InstallLocation { get; init; } = string.Empty;

    /// <summary>安装清单文件（install-manifest.json）的绝对路径</summary>
    public string ManifestPath { get; init; } = string.Empty;

    /// <summary>安装时间戳</summary>
    public DateTimeOffset InstalledAt { get; init; }

    /// <summary>主程序可执行文件名</summary>
    public string MainExe { get; init; } = string.Empty;

    /// <summary>卸载程序可执行文件名</summary>
    public string Uninstaller { get; init; } = string.Empty;
}
