using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaInstaller.ViewModels.Pages;

/// <summary>
/// 卸载完成页面 ViewModel。
/// </summary>
public sealed partial class UninstallCompletePageViewModel : ViewModelBase
{
    /// <summary>卸载日志路径。</summary>
    [ObservableProperty]
    private string? _uninstallLogPath;
}
