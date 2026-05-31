using AvaInstaller.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaInstaller.ViewModels.Pages;

/// <summary>
/// 卸载确认页面 ViewModel。
/// </summary>
public sealed partial class UninstallConfirmPageViewModel : ViewModelBase
{
    /// <summary>待卸载应用名称。</summary>
    [ObservableProperty]
    private string _installedAppName = InstallerMetadata.AppName;

    /// <summary>待卸载应用版本。</summary>
    [ObservableProperty]
    private string _installedVersion = InstallerMetadata.Version;

    /// <summary>待卸载应用目录。</summary>
    [ObservableProperty]
    private string _installedLocation = string.Empty;

    /// <summary>卸载时是否保留用户数据。</summary>
    [ObservableProperty]
    private bool _preserveUserData = true;

    /// <summary>卸载确认阶段错误信息。</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    /// <summary>是否存在错误信息。</summary>
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
}
