using AvaInstaller.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaInstaller.ViewModels.Pages;

/// <summary>
/// 安装完成页面 ViewModel。
/// 保存完成页上的启动应用选项。
/// </summary>
public sealed partial class CompletedPageViewModel : ViewModelBase
{
    /// <summary>当前安装器显示的应用名称。</summary>
    public string AppName => InstallerMetadata.AppName;

    /// <summary>点击完成后是否启动已安装应用。</summary>
    [ObservableProperty]
    private bool _launchAfterInstall = true;
}
