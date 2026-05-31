using AvaInstaller.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaInstaller.ViewModels.Pages;

/// <summary>
/// 已安装维护页面 ViewModel。
/// </summary>
public sealed partial class InstalledPageViewModel : ViewModelBase
{
    /// <summary>已安装应用名称。</summary>
    [ObservableProperty]
    private string _installedAppName = InstallerMetadata.AppName;

    /// <summary>已安装版本号。</summary>
    [ObservableProperty]
    private string _installedVersion = InstallerMetadata.Version;

    /// <summary>已安装目录。</summary>
    [ObservableProperty]
    private string _installedLocation = string.Empty;

    /// <summary>维护操作错误信息。</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    /// <summary>是否存在错误信息。</summary>
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
}
