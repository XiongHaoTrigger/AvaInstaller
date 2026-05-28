using System.Diagnostics;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AvaInstaller.Models;
using AvaInstaller.Services;

namespace AvaInstaller.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IPayloadExtractor _payloadExtractor;
    private readonly IFolderPickerService _folderPickerService;
    private readonly InstallCompletionService _installCompletionService;
    private readonly InstallManifestService _manifestService;
    private readonly InstallStateService _stateService;
    private readonly UninstallService _uninstallService;

    private string? _uninstallManifestPath;

    // 当前安装器页面状态。XAML 通过这些状态派生属性控制各页面显示。
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsWelcomePage))]
    [NotifyPropertyChangedFor(nameof(IsDirectoryPage))]
    [NotifyPropertyChangedFor(nameof(IsProgressPage))]
    [NotifyPropertyChangedFor(nameof(IsCompletePage))]
    [NotifyPropertyChangedFor(nameof(IsInstalledPage))]
    [NotifyPropertyChangedFor(nameof(IsUninstallPage))]
    [NotifyPropertyChangedFor(nameof(IsUninstallProgressPage))]
    [NotifyPropertyChangedFor(nameof(IsUninstallCompletePage))]
    [NotifyPropertyChangedFor(nameof(IsNotCompletePage))]
    [NotifyPropertyChangedFor(nameof(IsBackButtonVisible))]
    [NotifyCanExecuteChangedFor(nameof(BackCommand))]
    [NotifyCanExecuteChangedFor(nameof(NextCommand))]
    [NotifyCanExecuteChangedFor(nameof(InstallCommand))]
    private InstallPage currentPage = InstallPage.Welcome;

    // 默认安装到当前用户目录，避免默认请求管理员权限。
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(InstallCommand))]
    private string installDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        InstallerMetadata.AppName);

    [ObservableProperty]
    private int progressPercent;

    [ObservableProperty]
    private string currentFile = string.Empty;

    [ObservableProperty]
    private string statusText = $"Ready to install {InstallerMetadata.AppName}.";

    [ObservableProperty]
    private string? errorMessage;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(BackCommand))]
    [NotifyCanExecuteChangedFor(nameof(NextCommand))]
    [NotifyCanExecuteChangedFor(nameof(InstallCommand))]
    [NotifyCanExecuteChangedFor(nameof(BrowseCommand))]
    private bool isInstalling;

    [ObservableProperty]
    private bool launchAfterInstall = true;

    [ObservableProperty]
    private string installedAppName = InstallerMetadata.AppName;

    [ObservableProperty]
    private string installedVersion = InstallerMetadata.Version;

    [ObservableProperty]
    private string installedLocation = string.Empty;

    [ObservableProperty]
    private bool preserveUserData = true;

    [ObservableProperty]
    private bool isUninstalling;

    [ObservableProperty]
    private int uninstallProgressPercent;

    [ObservableProperty]
    private string uninstallStatusText = "Ready to uninstall.";

    [ObservableProperty]
    private string? uninstallLogPath;

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
    }

    public bool IsWelcomePage => CurrentPage == InstallPage.Welcome;
    public bool IsDirectoryPage => CurrentPage == InstallPage.Directory;
    public bool IsProgressPage => CurrentPage == InstallPage.Progress;
    public bool IsCompletePage => CurrentPage == InstallPage.Complete;
    public bool IsInstalledPage => CurrentPage == InstallPage.Installed;
    public bool IsUninstallPage => CurrentPage == InstallPage.Uninstall;
    public bool IsUninstallProgressPage => CurrentPage == InstallPage.UninstallProgress;
    public bool IsUninstallCompletePage => CurrentPage == InstallPage.UninstallComplete;
    public bool IsNotCompletePage => CurrentPage is not InstallPage.Complete and not InstallPage.UninstallComplete;
    public bool IsBackButtonVisible => CurrentPage is InstallPage.Directory or InstallPage.Progress;
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    // ErrorMessage 变化时同步通知 HasError，供 XAML 控制错误提示区域显隐。
    partial void OnErrorMessageChanged(string? value)
    {
        OnPropertyChanged(nameof(HasError));
    }

    // 启动时解析命令行。放到窗口创建后执行，避免 AOT 下状态文件 I/O 阻塞主窗口显示。
    public async void InitializeStartupMode()
    {
        var startupState = await Task.Run(ResolveStartupState);
        await Dispatcher.UIThread.InvokeAsync(() => ApplyStartupState(startupState));
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

    private InstallState? TryReadState()
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

    private void LoadManifestForUninstall(InstallManifest manifest, string manifestPath)
    {
        InstalledAppName = manifest.AppName;
        InstalledVersion = manifest.Version;
        InstalledLocation = manifest.InstallLocation;
        InstallDirectory = manifest.InstallLocation;
        _uninstallManifestPath = manifestPath;
    }

    private void LoadInstalledState(InstallState state)
    {
        InstalledAppName = state.AppName;
        InstalledVersion = state.Version;
        InstalledLocation = state.InstallLocation;
        InstallDirectory = state.InstallLocation;
        _uninstallManifestPath = state.ManifestPath;
    }

    private void ApplyStartupState(StartupState startupState)
    {
        switch (startupState.Mode)
        {
            case StartupMode.Install:
                CurrentPage = InstallPage.Welcome;
                break;
            case StartupMode.Installed:
                if (startupState.State is not null)
                {
                    LoadInstalledState(startupState.State);
                    CurrentPage = InstallPage.Installed;
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

                CurrentPage = InstallPage.Uninstall;
                break;
            case StartupMode.UninstallError:
                ErrorMessage = startupState.ErrorMessage;
                CurrentPage = InstallPage.Uninstall;
                break;
        }
    }

    [RelayCommand(CanExecute = nameof(CanBrowse))]
    private async Task BrowseAsync()
    {
        var selectedFolder = await _folderPickerService.PickFolderAsync(InstallDirectory, CancellationToken.None);
        if (!string.IsNullOrWhiteSpace(selectedFolder))
        {
            InstallDirectory = selectedFolder;
            ErrorMessage = null;
        }
    }

    [RelayCommand(CanExecute = nameof(CanGoBack))]
    private void Back()
    {
        ErrorMessage = null;

        CurrentPage = CurrentPage switch
        {
            InstallPage.Directory => InstallPage.Welcome,
            InstallPage.Progress => InstallPage.Directory,
            _ => CurrentPage
        };
    }

    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private void Next()
    {
        ErrorMessage = null;

        CurrentPage = CurrentPage switch
        {
            InstallPage.Welcome => InstallPage.Directory,
            _ => CurrentPage
        };
    }

    [RelayCommand(CanExecute = nameof(CanInstall))]
    private async Task InstallAsync()
    {
        ErrorMessage = null;
        CurrentPage = InstallPage.Progress;
        IsInstalling = true;
        ProgressPercent = 0;
        CurrentFile = string.Empty;
        StatusText = "Preparing installation...";

        try
        {
            // 支持用户输入环境变量路径，例如 %LocalAppData%\MyAvaloniaApp。
            var target = Path.GetFullPath(Environment.ExpandEnvironmentVariables(InstallDirectory.Trim()));
            InstallDirectory = target;

            var progress = new Progress<InstallProgress>(OnInstallProgressChanged);
            var extractionResult = await _payloadExtractor.ExtractAsync(target, progress, CancellationToken.None);
            await _installCompletionService.CompleteAsync(target, extractionResult, CancellationToken.None);

            StatusText = "Installation completed.";
            CurrentFile = string.Empty;
            ProgressPercent = 100;
            CurrentPage = InstallPage.Complete;
        }
        catch (OperationCanceledException)
        {
            ErrorMessage = "Installation was canceled.";
            StatusText = "Installation canceled.";
            CurrentPage = InstallPage.Directory;
        }
        catch (UnauthorizedAccessException ex)
        {
            ErrorMessage = $"No permission to write to the selected folder. {ex.Message}";
            StatusText = "Installation failed.";
            CurrentPage = InstallPage.Directory;
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or InvalidOperationException or NotSupportedException)
        {
            ErrorMessage = ex.Message;
            StatusText = "Installation failed.";
            CurrentPage = InstallPage.Directory;
        }
        finally
        {
            IsInstalling = false;
        }
    }

    [RelayCommand]
    private void ShowUninstall()
    {
        ErrorMessage = null;
        CurrentPage = InstallPage.Uninstall;
    }

    [RelayCommand]
    private void Upgrade()
    {
        ErrorMessage = "Upgrade is reserved for a later version.";
    }

    [RelayCommand]
    private void Repair()
    {
        ErrorMessage = "Repair is reserved for a later version.";
    }

    [RelayCommand]
    private async Task StartUninstallAsync()
    {
        ErrorMessage = null;
        if (string.IsNullOrWhiteSpace(_uninstallManifestPath))
        {
            ErrorMessage = "Install manifest was not found. Unable to uninstall.";
            return;
        }

        IsUninstalling = true;
        UninstallProgressPercent = 0;
        UninstallStatusText = "Preparing uninstall...";
        CurrentPage = InstallPage.UninstallProgress;

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
        IsUninstalling = false;

        if (result.Succeeded)
        {
            UninstallProgressPercent = 100;
            UninstallStatusText = "Uninstall completed.";
            CurrentPage = InstallPage.UninstallComplete;
        }
        else
        {
            ErrorMessage = result.ErrorMessage;
            UninstallStatusText = "Uninstall failed.";
            CurrentPage = InstallPage.Uninstall;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        Shutdown();
    }

    [RelayCommand]
    private void Finish()
    {
        if (LaunchAfterInstall)
        {
            TryLaunchInstalledApp();
        }

        Shutdown();
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
        return !IsInstalling;
    }

    private bool CanGoBack()
    {
        return !IsInstalling && CurrentPage is InstallPage.Directory or InstallPage.Progress;
    }

    private bool CanGoNext()
    {
        return !IsInstalling && CurrentPage == InstallPage.Welcome;
    }

    private bool CanInstall()
    {
        return !IsInstalling &&
               CurrentPage == InstallPage.Directory &&
               !string.IsNullOrWhiteSpace(InstallDirectory);
    }

    private void TryLaunchInstalledApp()
    {
        try
        {
            // 安装完成后只启动目标目录下的主程序，不修改系统 PATH 或注册表。
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
        InstallState? State = null,
        string? ErrorMessage = null)
    {
        public static StartupState Install()
        {
            return new StartupState(StartupMode.Install);
        }

        public static StartupState Installed(InstallState state)
        {
            return new StartupState(StartupMode.Installed, State: state);
        }

        public static StartupState Uninstall(string manifestPath, InstallManifest? manifest = null, InstallState? state = null)
        {
            return new StartupState(StartupMode.Uninstall, manifestPath, manifest, state);
        }

        public static StartupState UninstallError(string message)
        {
            return new StartupState(StartupMode.UninstallError, ErrorMessage: message);
        }
    }
}
