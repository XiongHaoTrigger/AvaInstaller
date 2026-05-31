using AvaInstaller.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AvaInstaller.ViewModels.Pages;

/// <summary>
/// 安装路径页面 ViewModel。
/// 负责保存用户选择的安装目录，并把浏览文件夹命令委托给主流程提供的服务。
/// </summary>
public sealed partial class PathPageViewModel : ViewModelBase
{
    private readonly Func<Task> _browseAsync;

    /// <summary>
    /// 设计器使用的无参构造，避免预览时执行真实文件夹选择逻辑。
    /// </summary>
    public PathPageViewModel()
        : this(() => Task.CompletedTask)
    {
    }

    /// <summary>
    /// 创建安装路径页面 ViewModel。
    /// </summary>
    /// <param name="browseAsync">打开文件夹选择器的异步委托。</param>
    public PathPageViewModel(Func<Task> browseAsync)
    {
        _browseAsync = browseAsync;
    }

    /// <summary>当前安装器显示的应用名称。</summary>
    public string AppName => InstallerMetadata.AppName;

    /// <summary>默认安装目录提示文本。</summary>
    public string DefaultFolder => $@"%LocalAppData%\{InstallerMetadata.AppName}";

    /// <summary>当前错误信息，主窗口底部 tips 也会展示同一条信息。</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    /// <summary>用户选择或输入的安装目录。</summary>
    [ObservableProperty]
    private string _installDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        InstallerMetadata.AppName);

    /// <summary>是否存在错误信息。</summary>
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    /// <summary>
    /// 打开文件夹选择器。
    /// </summary>
    [RelayCommand]
    private Task BrowseAsync()
    {
        return _browseAsync();
    }
}
