using AvaInstaller.Models;

namespace AvaInstaller.ViewModels.Pages;

/// <summary>
/// 欢迎页面 ViewModel。
/// </summary>
public sealed class WelcomePageViewModel : ViewModelBase
{
    /// <summary>当前安装器显示的应用名称。</summary>
    public string AppName => InstallerMetadata.AppName;

    /// <summary>当前安装器版本。</summary>
    public string Version => InstallerMetadata.Version;
}
