namespace AvaInstaller.Models;

// 安装目录内的清单文件，卸载时只按照这里记录的相对路径清理本安装器创建的内容。
public sealed record InstallManifest
{
    public string AppName { get; init; } = string.Empty;
    public string AppId { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string Publisher { get; init; } = string.Empty;
    public string InstallLocation { get; init; } = string.Empty;
    public string MainExe { get; init; } = string.Empty;
    public string Uninstaller { get; init; } = string.Empty;
    public DateTimeOffset InstalledAt { get; init; }
    public List<string> Files { get; init; } = [];
    public List<string> Directories { get; init; } = [];
    public List<string> Shortcuts { get; init; } = [];
    public List<string> Preserve { get; init; } = [];
}
