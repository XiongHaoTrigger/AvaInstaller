namespace AvaInstaller.Models;

/// <summary>
/// 安装状态机可接收的事件。
/// </summary>
public enum InstallerEvent
{
    /// <summary>开始安装。</summary>
    BeginInstall,

    /// <summary>安装成功完成。</summary>
    CompleteInstall,

    /// <summary>安装失败。</summary>
    FailInstall
}
