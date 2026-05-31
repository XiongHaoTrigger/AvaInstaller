using AvaInstaller.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaInstaller.ViewModels.Pages;

/// <summary>
/// 安装中页面 ViewModel。
/// 承载解压进度、当前文件和状态文案。
/// </summary>
public sealed partial class InstallingPageViewModel : ViewModelBase
{
    /// <summary>当前安装器显示的应用名称。</summary>
    public string AppName => InstallerMetadata.AppName;

    /// <summary>安装进度百分比。</summary>
    [ObservableProperty]
    private int _progressPercent;

    /// <summary>当前正在处理的文件路径。</summary>
    [ObservableProperty]
    private string _currentFile = string.Empty;

    /// <summary>安装状态说明。</summary>
    [ObservableProperty]
    private string _statusText = $"Ready to install {InstallerMetadata.AppName}.";
}
