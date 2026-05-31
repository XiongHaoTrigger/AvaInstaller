using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaInstaller.ViewModels.Pages;

/// <summary>
/// 卸载进度页面 ViewModel。
/// </summary>
public sealed partial class UninstallProgressPageViewModel : ViewModelBase
{
    /// <summary>卸载进度百分比。</summary>
    [ObservableProperty]
    private int _uninstallProgressPercent;

    /// <summary>卸载状态文本。</summary>
    [ObservableProperty]
    private string _uninstallStatusText = "Ready to uninstall.";
}
