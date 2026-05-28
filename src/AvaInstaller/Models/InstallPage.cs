namespace AvaInstaller.Models;

// 安装器页面枚举。ViewModel 根据该状态驱动 XAML 中各页面的 IsVisible。
public enum InstallPage
{
    Welcome,
    Directory,
    Progress,
    Complete,
    Installed,
    Uninstall,
    UninstallProgress,
    UninstallComplete
}
