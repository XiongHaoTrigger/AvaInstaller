using System.Diagnostics;
using System.ComponentModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AvaInstaller.Models;
using AvaInstaller.Services;
using AvaInstaller.StateMachines;
using AvaInstaller.ViewModels.Pages;

namespace AvaInstaller.ViewModels;

/// <summary>
/// 主窗口视图模型，负责承载界面数据并通过状态机驱动安装器流程。
/// </summary>
/// <remarks>
/// 页面切换由 CurrentPage 暴露给 XAML；安装任务状态由 InstallerStateMachine 校验。
/// 主 ViewModel 只负责组合服务、页面 VM 和命令，不在 View 中写业务逻辑。
/// </remarks>
public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IPayloadExtractor _payloadExtractor;
    private readonly IFolderPickerService _folderPickerService;
    private readonly InstallCompletionService _installCompletionService;
    private readonly InstallManifestService _manifestService;
    private readonly InstallStateService _stateService;
    private readonly UninstallService _uninstallService;
    private readonly InstallerStateMachine _stateMachine = new();

    private string? _uninstallManifestPath;

    public WelcomePageViewModel WelcomePage { get; }

    public PathPageViewModel PathPage { get; }

    public InstallingPageViewModel InstallingPage { get; }

    public CompletedPageViewModel CompletedPage { get; }

    public InstalledPageViewModel InstalledPage { get; }

    public UninstallConfirmPageViewModel UninstallConfirmPage { get; }

    public UninstallProgressPageViewModel UninstallProgressPage { get; }

    public UninstallCompletePageViewModel UninstallCompletePage { get; }

    [ObservableProperty]
    private ViewModelBase _activePage = null!;

    /// <summary>
    /// 获取当前界面步骤。
    /// XAML 根据该值切换页面、步骤高亮和底部命令。
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(BackCommand))]
    [NotifyCanExecuteChangedFor(nameof(NextCommand))]
    [NotifyCanExecuteChangedFor(nameof(InstallCommand))]
    [NotifyCanExecuteChangedFor(nameof(BrowseCommand))]
    private InstallerStep _currentPage = InstallerStep.Welcome;

    /// <summary>
    /// 获取当前安装流程状态。
    /// </summary>
    [ObservableProperty]
    private InstallerState _installerState = InstallerState.Idle;

    /// <summary>
    /// 获取或设置安装目录。
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(InstallCommand))]
    private string _installDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        InstallerMetadata.AppName);

    /// <summary>
    /// 获取或设置安装进度百分比。
    /// </summary>
    [ObservableProperty]
    private int _progressPercent;

    /// <summary>
    /// 获取或设置当前正在处理的文件。
    /// </summary>
    [ObservableProperty]
    private string _currentFile = string.Empty;

    /// <summary>
    /// 获取或设置安装状态文本。
    /// </summary>
    [ObservableProperty]
    private string _statusText = $"Ready to install {InstallerMetadata.AppName}.";

    /// <summary>
    /// 获取或设置当前错误消息。
    /// </summary>
    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>
    /// 获取或设置安装完成后是否启动应用。
    /// </summary>
    [ObservableProperty]
    private bool _launchAfterInstall = true;

    /// <summary>
    /// 获取或设置已安装应用名称。
    /// </summary>
    [ObservableProperty]
    private string _installedAppName = InstallerMetadata.AppName;

    /// <summary>
    /// 获取或设置已安装应用版本。
    /// </summary>
    [ObservableProperty]
    private string _installedVersion = InstallerMetadata.Version;

    /// <summary>
    /// 获取或设置已安装位置。
    /// </summary>
    [ObservableProperty]
    private string _installedLocation = string.Empty;

    /// <summary>
    /// 获取或设置卸载时是否保留用户数据。
    /// </summary>
    [ObservableProperty]
    private bool _preserveUserData = true;

    /// <summary>
    /// 获取或设置卸载进度百分比。
    /// </summary>
    [ObservableProperty]
    private int _uninstallProgressPercent;

    /// <summary>
    /// 获取或设置卸载状态文本。
    /// </summary>
    [ObservableProperty]
    private string _uninstallStatusText = "Ready to uninstall.";

    /// <summary>
    /// 获取或设置卸载日志路径。
    /// </summary>
    [ObservableProperty]
    private string? _uninstallLogPath;

    /// <summary>
    /// 获取一个值，该值指示当前是否存在错误消息。
    /// </summary>
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    /// <summary>
    /// 初始化 <see cref="MainWindowViewModel"/> 类的新实例。
    /// </summary>
    public MainWindowViewModel()
        : this(
            new PayloadExtractor(),
            new FolderPickerService(),
            new InstallCompletionService(new InstallManifestService(), new InstallStateService()),
            new InstallManifestService(),
            new InstallStateService(),
            new UninstallService(new InstallManifestService(), new InstallStateService(), new ShortcutService(), new SelfDeleteService()))
    {
    }

    /// <summary>
    /// 使用指定服务初始化 <see cref="MainWindowViewModel"/> 类的新实例。
    /// </summary>
    /// <param name="payloadExtractor">payload 解压服务。</param>
    /// <param name="folderPickerService">文件夹选择服务。</param>
    /// <param name="installCompletionService">安装完成收尾服务。</param>
    /// <param name="manifestService">安装清单服务。</param>
    /// <param name="stateService">安装状态服务。</param>
    /// <param name="uninstallService">卸载服务。</param>
    public MainWindowViewModel(
        IPayloadExtractor payloadExtractor,
        IFolderPickerService folderPickerService,
        InstallCompletionService installCompletionService,
        InstallManifestService manifestService,
        InstallStateService stateService,
        UninstallService uninstallService)
    {
        _payloadExtractor = payloadExtractor;
        _folderPickerService = folderPickerService;
        _installCompletionService = installCompletionService;
        _manifestService = manifestService;
        _stateService = stateService;
        _uninstallService = uninstallService;

        WelcomePage = new WelcomePageViewModel();
        PathPage = new PathPageViewModel(BrowseSelectedFolderAsync)
        {
            InstallDirectory = InstallDirectory
        };
        InstallingPage = new InstallingPageViewModel
        {
            ProgressPercent = ProgressPercent,
            CurrentFile = CurrentFile,
            StatusText = StatusText
        };
        CompletedPage = new CompletedPageViewModel
        {
            LaunchAfterInstall = LaunchAfterInstall
        };
        InstalledPage = new InstalledPageViewModel
        {
            InstalledAppName = InstalledAppName,
            InstalledVersion = InstalledVersion,
            InstalledLocation = InstalledLocation
        };
        UninstallConfirmPage = new UninstallConfirmPageViewModel
        {
            InstalledAppName = InstalledAppName,
            InstalledVersion = InstalledVersion,
            InstalledLocation = InstalledLocation,
            PreserveUserData = PreserveUserData
        };
        UninstallProgressPage = new UninstallProgressPageViewModel
        {
            UninstallProgressPercent = UninstallProgressPercent,
            UninstallStatusText = UninstallStatusText
        };
        UninstallCompletePage = new UninstallCompletePageViewModel
        {
            UninstallLogPath = UninstallLogPath
        };

        PathPage.PropertyChanged += OnPagePropertyChanged;
        CompletedPage.PropertyChanged += OnPagePropertyChanged;
        UninstallConfirmPage.PropertyChanged += OnPagePropertyChanged;
        ActivePage = ResolveActivePage(CurrentPage);
    }

    /// <summary>
    /// 在错误消息变化时通知 <see cref="HasError"/>。
    /// </summary>
    /// <param name="value">新的错误消息。</param>
    partial void OnErrorMessageChanged(string? value)
    {
        OnPropertyChanged(nameof(HasError));
        PathPage.ErrorMessage = value;
        InstalledPage.ErrorMessage = value;
        UninstallConfirmPage.ErrorMessage = value;
    }

    partial void OnCurrentPageChanged(InstallerStep value)
    {
        ActivePage = ResolveActivePage(value);
    }

    partial void OnInstallDirectoryChanged(string value)
    {
        if (PathPage.InstallDirectory != value)
        {
            PathPage.InstallDirectory = value;
        }
    }

    partial void OnProgressPercentChanged(int value)
    {
        InstallingPage.ProgressPercent = value;
    }

    partial void OnCurrentFileChanged(string value)
    {
        InstallingPage.CurrentFile = value;
    }

    partial void OnStatusTextChanged(string value)
    {
        InstallingPage.StatusText = value;
    }

    partial void OnLaunchAfterInstallChanged(bool value)
    {
        if (CompletedPage.LaunchAfterInstall != value)
        {
            CompletedPage.LaunchAfterInstall = value;
        }
    }

    partial void OnInstalledAppNameChanged(string value)
    {
        InstalledPage.InstalledAppName = value;
        UninstallConfirmPage.InstalledAppName = value;
    }

    partial void OnInstalledVersionChanged(string value)
    {
        InstalledPage.InstalledVersion = value;
        UninstallConfirmPage.InstalledVersion = value;
    }

    partial void OnInstalledLocationChanged(string value)
    {
        InstalledPage.InstalledLocation = value;
        UninstallConfirmPage.InstalledLocation = value;
    }

    partial void OnPreserveUserDataChanged(bool value)
    {
        if (UninstallConfirmPage.PreserveUserData != value)
        {
            UninstallConfirmPage.PreserveUserData = value;
        }
    }

    partial void OnUninstallProgressPercentChanged(int value)
    {
        UninstallProgressPage.UninstallProgressPercent = value;
    }

    partial void OnUninstallStatusTextChanged(string value)
    {
        UninstallProgressPage.UninstallStatusText = value;
    }

    partial void OnUninstallLogPathChanged(string? value)
    {
        UninstallCompletePage.UninstallLogPath = value;
    }

    /// <summary>
    /// 异步解析启动模式并切换到对应状态。
    /// </summary>
    /// <remarks>
    /// 该方法在主窗口创建后调用，避免 Native AOT 下同步 I/O 阻塞窗口显示。
    /// </remarks>
    public async Task InitializeStartupMode()
    {
        try
        {
            var startupState = await Task.Run(ResolveStartupState);
            await Dispatcher.UIThread.InvokeAsync(() => ApplyStartupState(startupState));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or InvalidOperationException)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ErrorMessage = $"Failed to initialize installer state. {ex.Message}";
                CurrentPage = InstallerStep.Welcome;
            });
        }
    }

    /// <summary>
    /// 打开系统文件夹选择器。
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanBrowse))]
    private async Task BrowseAsync()
    {
        await BrowseSelectedFolderAsync();
    }

    private async Task BrowseSelectedFolderAsync()
    {
        var selectedFolder = await _folderPickerService.PickFolderAsync(InstallDirectory, CancellationToken.None);
        if (!string.IsNullOrWhiteSpace(selectedFolder))
        {
            InstallDirectory = selectedFolder;
            ErrorMessage = null;
        }
    }

    /// <summary>
    /// 返回上一页。
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanGoBack))]
    private void Back()
    {
        ErrorMessage = null;
        CurrentPage = InstallerStep.Welcome;
    }

    /// <summary>
    /// 进入下一页。
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private void Next()
    {
        ErrorMessage = null;
        CurrentPage = InstallerStep.Path;
    }

    /// <summary>
    /// 执行安装流程。
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanInstall))]
    private async Task InstallAsync()
    {
        ErrorMessage = null;
        MoveInstallFlow(InstallerEvent.BeginInstall);
        CurrentPage = InstallerStep.Installing;
        ProgressPercent = 0;
        CurrentFile = string.Empty;
        StatusText = "Preparing installation...";

        try
        {
            var target = Path.GetFullPath(Environment.ExpandEnvironmentVariables(InstallDirectory.Trim()));
            InstallDirectory = target;

            var progress = new Progress<InstallProgress>(OnInstallProgressChanged);
            var extractionResult = await _payloadExtractor.ExtractAsync(target, progress, CancellationToken.None);
            await _installCompletionService.CompleteAsync(target, extractionResult, CancellationToken.None);

            StatusText = "Installation completed.";
            CurrentFile = string.Empty;
            ProgressPercent = 100;
            MoveInstallFlow(InstallerEvent.CompleteInstall);
            CurrentPage = InstallerStep.Completed;
        }
        catch (OperationCanceledException)
        {
            FailInstall("Installation was canceled.");
        }
        catch (UnauthorizedAccessException ex)
        {
            FailInstall($"No permission to write to the selected folder. {ex.Message}");
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or InvalidOperationException or NotSupportedException)
        {
            FailInstall(ex.Message);
        }
    }

    /// <summary>
    /// 从维护页进入卸载确认页。
    /// </summary>
    [RelayCommand]
    private void ShowUninstall()
    {
        ErrorMessage = null;
        CurrentPage = InstallerStep.UninstallConfirm;
    }

    /// <summary>
    /// 升级入口。当前版本仅保留占位。
    /// </summary>
    [RelayCommand]
    private void Upgrade()
    {
        ErrorMessage = "Upgrade is reserved for a later version.";
    }

    /// <summary>
    /// 修复入口。当前版本仅保留占位。
    /// </summary>
    [RelayCommand]
    private void Repair()
    {
        ErrorMessage = "Repair is reserved for a later version.";
    }

    /// <summary>
    /// 执行卸载流程。
    /// </summary>
    [RelayCommand]
    private async Task StartUninstallAsync()
    {
        ErrorMessage = null;
        if (string.IsNullOrWhiteSpace(_uninstallManifestPath))
        {
            FailUninstall("Install manifest was not found. Unable to uninstall.");
            return;
        }

        CurrentPage = InstallerStep.UninstallProgress;
        UninstallProgressPercent = 0;
        UninstallStatusText = "Preparing uninstall...";

        var progress = new Progress<UninstallProgress>(value =>
        {
            UninstallProgressPercent = value.Percent;
            UninstallStatusText = value.Status;
        });

        var manifestPath = _uninstallManifestPath;
        var preserveUserData = PreserveUserData;
        var result = await Task.Run(() => _uninstallService
            .UninstallAsync(
                manifestPath,
                preserveUserData,
                silent: false,
                progress,
                CancellationToken.None)
            .GetAwaiter()
            .GetResult());

        UninstallLogPath = result.LogPath;
        if (result.Succeeded)
        {
            UninstallProgressPercent = 100;
            UninstallStatusText = "Uninstall completed.";
            CurrentPage = InstallerStep.UninstallComplete;
        }
        else
        {
            FailUninstall(result.ErrorMessage ?? "Uninstall failed.");
        }
    }

    /// <summary>
    /// 取消并关闭安装器。
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        Shutdown();
    }

    /// <summary>
    /// 完成当前流程并关闭安装器。
    /// </summary>
    [RelayCommand]
    private void Finish()
    {
        if (CurrentPage == InstallerStep.Completed && LaunchAfterInstall)
        {
            TryLaunchInstalledApp();
        }

        Shutdown();
    }

    [RelayCommand]
    private void ClearError()
    {
        ErrorMessage = null;
    }

    private StartupState ResolveStartupState()
    {
        var args = Environment.GetCommandLineArgs().Skip(1).ToArray();
        var localManifest = Path.Combine(AppContext.BaseDirectory, InstallerMetadata.ManifestFileName);
        var executableName = Path.GetFileName(Environment.ProcessPath);
        var isUninstallerExecutable = executableName?.Equals(
            InstallerMetadata.Uninstaller,
            StringComparison.OrdinalIgnoreCase) == true;

        if (args.Any(arg => arg.Equals("--install", StringComparison.OrdinalIgnoreCase)))
        {
            return StartupState.Install();
        }

        if (args.Any(arg => arg.Equals("--uninstall", StringComparison.OrdinalIgnoreCase)) ||
            isUninstallerExecutable ||
            File.Exists(localManifest))
        {
            return ResolveUninstallStartupState(localManifest);
        }

        var state = TryReadState();
        if (state is not null && File.Exists(state.ManifestPath))
        {
            return StartupState.Installed(state);
        }

        return StartupState.Install();
    }

    private StartupState ResolveUninstallStartupState(string localManifest)
    {
        if (File.Exists(localManifest))
        {
            var manifest = TryReadManifest(localManifest);
            return manifest is null
                ? StartupState.UninstallError($"Unable to read install manifest: {localManifest}")
                : StartupState.Uninstall(localManifest, manifest);
        }

        var state = TryReadState();
        if (state is not null && File.Exists(state.ManifestPath))
        {
            return StartupState.Uninstall(state.ManifestPath, state: state);
        }

        return StartupState.UninstallError("No installed application state or install-manifest.json was found.");
    }

    private Models.InstallState? TryReadState()
    {
        try
        {
            return _stateService.TryReadAsync(CancellationToken.None).GetAwaiter().GetResult();
        }
        catch
        {
            return null;
        }
    }

    private InstallManifest? TryReadManifest(string manifestPath)
    {
        try
        {
            return _manifestService.ReadAsync(manifestPath, CancellationToken.None).GetAwaiter().GetResult();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or System.Text.Json.JsonException)
        {
            ErrorMessage = $"Unable to read install manifest. {ex.Message}";
            return null;
        }
    }

    private void ApplyStartupState(StartupState startupState)
    {
        switch (startupState.Mode)
        {
            case StartupMode.Install:
                CurrentPage = InstallerStep.Welcome;
                break;
            case StartupMode.Installed:
                if (startupState.State is not null)
                {
                    LoadInstalledState(startupState.State);
                    CurrentPage = InstallerStep.Installed;
                }
                break;
            case StartupMode.Uninstall:
                if (startupState.Manifest is not null && !string.IsNullOrWhiteSpace(startupState.ManifestPath))
                {
                    LoadManifestForUninstall(startupState.Manifest, startupState.ManifestPath);
                }
                else if (startupState.State is not null)
                {
                    LoadInstalledState(startupState.State);
                    _uninstallManifestPath = startupState.ManifestPath;
                }

                CurrentPage = InstallerStep.UninstallConfirm;
                break;
            case StartupMode.UninstallError:
                ErrorMessage = startupState.ErrorMessage;
                CurrentPage = InstallerStep.UninstallConfirm;
                break;
        }
    }

    private void LoadManifestForUninstall(InstallManifest manifest, string manifestPath)
    {
        InstalledAppName = manifest.AppName;
        InstalledVersion = manifest.Version;
        InstalledLocation = manifest.InstallLocation;
        InstallDirectory = manifest.InstallLocation;
        _uninstallManifestPath = manifestPath;
    }

    private void LoadInstalledState(Models.InstallState state)
    {
        InstalledAppName = state.AppName;
        InstalledVersion = state.Version;
        InstalledLocation = state.InstallLocation;
        InstallDirectory = state.InstallLocation;
        _uninstallManifestPath = state.ManifestPath;
    }

    private void OnInstallProgressChanged(InstallProgress progress)
    {
        ProgressPercent = progress.Percent;
        CurrentFile = progress.CurrentFile;
        StatusText = string.IsNullOrWhiteSpace(progress.CurrentFile)
            ? "Extracting files..."
            : $"Extracting {progress.CurrentFile}";
    }

    private bool CanBrowse()
    {
        return CurrentPage is InstallerStep.Welcome or InstallerStep.Path;
    }

    private bool CanGoBack()
    {
        return CurrentPage == InstallerStep.Path;
    }

    private bool CanGoNext()
    {
        return CurrentPage == InstallerStep.Welcome;
    }

    private bool CanInstall()
    {
        return CurrentPage == InstallerStep.Path &&
               !string.IsNullOrWhiteSpace(InstallDirectory);
    }

    private void FailInstall(string message)
    {
        ErrorMessage = message;
        StatusText = "Installation failed.";
        MoveInstallFlowIfPossible(InstallerEvent.FailInstall);
        CurrentPage = InstallerStep.Path;
    }

    private void FailUninstall(string message)
    {
        ErrorMessage = message;
        UninstallStatusText = "Uninstall failed.";
        CurrentPage = InstallerStep.UninstallConfirm;
    }

    private void MoveInstallFlow(InstallerEvent installerEvent)
    {
        InstallerState = _stateMachine.Fire(installerEvent);
    }

    private void MoveInstallFlowIfPossible(InstallerEvent installerEvent)
    {
        if (_stateMachine.CanFire(installerEvent))
        {
            MoveInstallFlow(installerEvent);
        }
    }

    private void TryLaunchInstalledApp()
    {
        try
        {
            var appPath = Path.Combine(InstallDirectory, InstallerMetadata.MainExe);
            if (!File.Exists(appPath))
            {
                ErrorMessage = $"Installed application was not found: {appPath}";
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = appPath,
                WorkingDirectory = InstallDirectory,
                UseShellExecute = true
            });
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            ErrorMessage = $"Unable to start {InstallerMetadata.MainExe}. {ex.Message}";
        }
    }

    private ViewModelBase ResolveActivePage(InstallerStep page)
    {
        return page switch
        {
            InstallerStep.Welcome => WelcomePage,
            InstallerStep.Path => PathPage,
            InstallerStep.Installing => InstallingPage,
            InstallerStep.Completed => CompletedPage,
            InstallerStep.Installed => InstalledPage,
            InstallerStep.UninstallConfirm => UninstallConfirmPage,
            InstallerStep.UninstallProgress => UninstallProgressPage,
            InstallerStep.UninstallComplete => UninstallCompletePage,
            _ => WelcomePage
        };
    }

    private void OnPagePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender == PathPage && e.PropertyName == nameof(PathPageViewModel.InstallDirectory))
        {
            InstallDirectory = PathPage.InstallDirectory;
            return;
        }

        if (sender == CompletedPage && e.PropertyName == nameof(CompletedPageViewModel.LaunchAfterInstall))
        {
            LaunchAfterInstall = CompletedPage.LaunchAfterInstall;
            return;
        }

        if (sender == UninstallConfirmPage && e.PropertyName == nameof(UninstallConfirmPageViewModel.PreserveUserData))
        {
            PreserveUserData = UninstallConfirmPage.PreserveUserData;
        }
    }

    private static void Shutdown()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    private enum StartupMode
    {
        Install,
        Installed,
        Uninstall,
        UninstallError
    }

    private sealed record StartupState(
        StartupMode Mode,
        string? ManifestPath = null,
        InstallManifest? Manifest = null,
        Models.InstallState? State = null,
        string? ErrorMessage = null)
    {
        public static StartupState Install()
        {
            return new StartupState(StartupMode.Install);
        }

        public static StartupState Installed(Models.InstallState state)
        {
            return new StartupState(StartupMode.Installed, State: state);
        }

        public static StartupState Uninstall(string manifestPath, InstallManifest? manifest = null, Models.InstallState? state = null)
        {
            return new StartupState(StartupMode.Uninstall, manifestPath, manifest, state);
        }

        public static StartupState UninstallError(string message)
        {
            return new StartupState(StartupMode.UninstallError, ErrorMessage: message);
        }
    }
}
