namespace AvaInstaller.Models;

/// <summary>
/// 安装清单记录。
/// 存储在安装目录下，卸载时依据此清单清理安装器创建的内容，
/// 只删除相对路径列出的文件与目录，避免误删用户数据。
/// </summary>
public sealed record InstallManifest
{
    /// <summary>应用程序名称</summary>
    public string AppName { get; init; } = string.Empty;

    /// <summary>应用程序唯一标识符</summary>
    public string AppId { get; init; } = string.Empty;

    /// <summary>安装的版本号</summary>
    public string Version { get; init; } = string.Empty;

    /// <summary>发布者名称</summary>
    public string Publisher { get; init; } = string.Empty;

    /// <summary>安装目录的绝对路径</summary>
    public string InstallLocation { get; init; } = string.Empty;

    /// <summary>主程序可执行文件名（相对路径）</summary>
    public string MainExe { get; init; } = string.Empty;

    /// <summary>卸载程序可执行文件名（相对路径）</summary>
    public string Uninstaller { get; init; } = string.Empty;

    /// <summary>安装时间戳</summary>
    public DateTimeOffset InstalledAt { get; init; }

    /// <summary>安装的所有文件相对路径列表</summary>
    public List<string> Files { get; init; } = [];

    /// <summary>安装创建的所有目录相对路径列表</summary>
    public List<string> Directories { get; init; } = [];

    /// <summary>创建的快捷方式完整路径列表</summary>
    public List<string> Shortcuts { get; init; } = [];

    /// <summary>
    /// 卸载时保留的项列表。
    /// 支持文件/目录的相对路径或名称模式匹配（如 "user.config"、"logs"）。
    /// </summary>
    public List<string> Preserve { get; init; } = [];
}
