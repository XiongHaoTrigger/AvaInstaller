namespace AvaInstaller.Options;

/// <summary>
/// 安装器运行配置。
/// 该类型用于集中描述可配置项，避免后续把配置散落在 ViewModel 或服务构造函数中。
/// </summary>
public sealed class InstallerOptions
{
    /// <summary>payload 文件名。</summary>
    public string PayloadFileName { get; init; } = "payload.zip";

    /// <summary>是否创建桌面快捷方式。</summary>
    public bool CreateDesktopShortcut { get; init; } = true;

    /// <summary>是否创建开始菜单快捷方式。</summary>
    public bool CreateStartMenuShortcut { get; init; } = true;
}
