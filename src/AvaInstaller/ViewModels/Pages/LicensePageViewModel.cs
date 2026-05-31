using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaInstaller.ViewModels.Pages;

/// <summary>
/// 许可协议页面 ViewModel。
/// 当前安装流程尚未强制启用协议页，保留该类型作为标准向导步骤扩展点。
/// </summary>
public sealed partial class LicensePageViewModel : ViewModelBase
{
    /// <summary>用户是否接受许可协议。</summary>
    [ObservableProperty]
    private bool _isAccepted;

    /// <summary>协议摘要文本。</summary>
    public string LicenseSummary => "继续安装即表示你接受本软件的安装和使用条款。";
}
