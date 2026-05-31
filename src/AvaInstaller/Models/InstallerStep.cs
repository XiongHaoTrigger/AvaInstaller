namespace AvaInstaller.Models;

/// <summary>
/// 安装器界面步骤。
/// 主窗口通过该枚举判断当前应该显示哪个页面，同时保留维护和卸载相关扩展页面。
/// </summary>
public enum InstallerStep
{
    /// <summary>欢迎页。</summary>
    Welcome,

    /// <summary>安装路径选择页。</summary>
    Path,

    /// <summary>安装执行页。</summary>
    Installing,

    /// <summary>安装完成页。</summary>
    Completed,

    /// <summary>已安装后的维护页。</summary>
    Installed,

    /// <summary>卸载确认页。</summary>
    UninstallConfirm,

    /// <summary>卸载进度页。</summary>
    UninstallProgress,

    /// <summary>卸载完成页。</summary>
    UninstallComplete
}
