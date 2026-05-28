namespace AvaInstaller.Models;

// 用户级固定位置的安装状态。安装器再次启动时依赖它判断是否已安装，不写注册表。
public sealed record InstallState
{
    public string AppName { get; init; } = string.Empty;
    public string AppId { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string InstallLocation { get; init; } = string.Empty;
    public string ManifestPath { get; init; } = string.Empty;
    public DateTimeOffset InstalledAt { get; init; }
    public string MainExe { get; init; } = string.Empty;
    public string Uninstaller { get; init; } = string.Empty;
}
