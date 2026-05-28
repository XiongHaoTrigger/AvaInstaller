using System.Diagnostics;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AvaInstaller.Models;
using AvaInstaller.Services;

namespace AvaInstaller.ViewModels;

/// <summary>
/// 主窗口视图模型 - 安装器核心业务逻辑层
/// </summary>
public partial class MainWindowViewModel : ViewModelBase
{
    // ========== 依赖服务 ==========
    // 这些服务通过构造函数注入，负责具体的安装/卸载操作
    
    /// <summary>负载解压服务 - 从安装包中提取文件到目标目录</summary>
    private readonly IPayloadExtractor _payloadExtractor;
    
    /// <summary>文件夹选择服务 - 弹出系统文件夹选择对话框</summary>
    private readonly IFolderPickerService _folderPickerService;
    
    /// <summary>安装完成服务 - 安装后的清理工作（创建快捷方式、写注册表等）</summary>
    private readonly InstallCompletionService _installCompletionService;
    
    /// <summary>安装清单服务 - 读取/写入 install-manifest.json（记录安装了哪些文件）</summary>
    private readonly InstallManifestService _manifestService;
    
    /// <summary>安装状态服务 - 读取/写入 installer-state.json（记录安装位置等信息）</summary>
    private readonly InstallStateService _stateService;
    
    /// <summary>卸载服务 - 执行卸载操作（删除文件、快捷方式、注册表项等）</summary>
    private readonly UninstallService _uninstallService;

    /// <summary>
    /// 卸载时使用的清单文件路径
    /// 从 install-manifest.json 或 installer-state.json 中获取
    /// </summary>
    private string? _uninstallManifestPath;

    // ========== 页面状态属性 ==========
    
    /// <summary>
    /// 当前安装器页面状态
    /// XAML 通过 IsWelcomePage / IsDirectoryPage 等派生属性控制各页面显隐
    /// 
    /// [NotifyPropertyChangedFor] - 当 CurrentPage 变化时，通知这些属性也变了（触发 UI 更新）
    /// [NotifyCanExecuteChangedFor] - 当 CurrentPage 变化时，通知命令重新检查 CanExecute
    /// </summary>
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

    // ========== 安装配置属性 ==========
    
    /// <summary>
    /// 用户选择的安装目录
    /// 默认安装到 %LocalAppData%\{AppName}，避免请求管理员权限
    /// 
    /// [NotifyCanExecuteChangedFor] - 目录变化时，InstallCommand 需要重新检查是否可执行
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(InstallCommand))]
    private string installDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        InstallerMetadata.AppName);

    /// <summary>安装进度百分比 (0-100)</summary>
    [ObservableProperty]
    private int progressPercent;

    /// <summary>当前正在解压的文件名（用于进度条下方显示）</summary>
    [ObservableProperty]
    private string currentFile = string.Empty;

    /// <summary>状态文本（如 "Extracting files..." 或 "Installation completed."）</summary>
    [ObservableProperty]
    private string statusText = $"Ready to install {InstallerMetadata.AppName}.";

    /// <summary>错误信息（非空时显示错误提示）</summary>
    [ObservableProperty]
    private string? errorMessage;

    /// <summary>
    /// 是否正在安装中
    /// 用于禁用按钮、防止重复操作
    /// 
    /// [NotifyCanExecuteChangedFor] - 安装状态时，Back/Next/Install/Browse 命令都需禁用
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(BackCommand))]
    [NotifyCanExecuteChangedFor(nameof(NextCommand))]
    [NotifyCanExecuteChangedFor(nameof(InstallCommand))]
    [NotifyCanExecuteChangedFor(nameof(BrowseCommand))]
    private bool isInstalling;

    /// <summary>安装完成后是否启动应用程序</summary>
    [ObservableProperty]
    private bool launchAfterInstall = true;

    // ========== 已安装信息属性 ==========
    
    /// <summary>已安装的应用名称（用于"已安装"页面显示）</summary>
    [ObservableProperty]
    private string installedAppName = InstallerMetadata.AppName;

    /// <summary>已安装的版本号</summary>
    [ObservableProperty]
    private string installedVersion = InstallerMetadata.Version;

    /// <summary>已安装的位置（安装目录路径）</summary>
    [ObservableProperty]
    private string installedLocation = string.Empty;



    #region Uninstall Properties

    /// <summary>卸载时是否保留用户数据</summary>
    [ObservableProperty]
    private bool preserveUserData = true;

    /// <summary>是否正在卸载中</summary>
    [ObservableProperty]
    private bool isUninstalling;

    /// <summary>卸载进度百分比 (0-100)</summary>
    [ObservableProperty]
    private int uninstallProgressPercent;

    /// <summary>卸载状态文本</summary>
    [ObservableProperty]
    private string uninstallStatusText = "Ready to uninstall.";

    /// <summary>卸载日志文件路径（卸载完成后可查看）</summary>
    [ObservableProperty]
    private string? uninstallLogPath;

    #endregion
    
    

    // ========== 构造函数 ==========
    
    /// <summary>
    /// 无参构造函数 - 用于生产环境（Avalonia 依赖注入）
    /// 创建所有服务的真实实现
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
    /// 带参构造函数 - 用于单元测试（依赖注入）
    /// </summary>
    /// <param name="payloadExtractor">负载解压服务（可 mock）</param>
    /// <param name="folderPickerService">文件夹选择服务（可 mock）</param>
    /// <param name="installCompletionService">安装完成服务</param>
    /// <param name="manifestService">安装清单服务</param>
    /// <param name="stateService">安装状态服务</param>
    /// <param name="uninstallService">卸载服务</param>
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

    // ========== 页面状态派生属性 ==========
    // 这些属性由 CurrentPage 计算而来，XAML 绑定它们来控制页面显隐
    
    /// <summary>当前是否显示欢迎页面</summary>
    public bool IsWelcomePage => CurrentPage == InstallPage.Welcome;
    
    /// <summary>当前是否显示目录选择页面</summary>
    public bool IsDirectoryPage => CurrentPage == InstallPage.Directory;
    
    /// <summary>当前是否显示安装进度页面</summary>
    public bool IsProgressPage => CurrentPage == InstallPage.Progress;
    
    /// <summary>当前是否显示安装完成页面</summary>
    public bool IsCompletePage => CurrentPage == InstallPage.Complete;
    
    /// <summary>当前是否显示已安装页面（应用已安装，显示修复/卸载选项）</summary>
    public bool IsInstalledPage => CurrentPage == InstallPage.Installed;
    
    /// <summary>当前是否显示卸载确认页面</summary>
    public bool IsUninstallPage => CurrentPage == InstallPage.Uninstall;
    
    /// <summary>当前是否显示卸载进度页面</summary>
    public bool IsUninstallProgressPage => CurrentPage == InstallPage.UninstallProgress;
    
    /// <summary>当前是否显示卸载完成页面</summary>
    public bool IsUninstallCompletePage => CurrentPage == InstallPage.UninstallComplete;
    
    /// <summary>当前是否不在完成页面（用于控制某些按钮的显示）</summary>
    public bool IsNotCompletePage => CurrentPage is not InstallPage.Complete and not InstallPage.UninstallComplete;
    
    /// <summary>返回按钮是否可见（仅在 Directory 和 Progress 页面显示）</summary>
    public bool IsBackButtonVisible => CurrentPage is InstallPage.Directory or InstallPage.Progress;
    
    /// <summary>是否有错误（用于控制错误提示区域的显隐）</summary>
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    /// <summary>
    /// ErrorMessage 变化时的部分方法（由 Source Generator 生成）
    /// 手动触发 HasError 属性变更通知，因为 HasError 依赖 ErrorMessage
    /// </summary>
    partial void OnErrorMessageChanged(string? value)
    {
        OnPropertyChanged(nameof(HasError));
    }

    // ========== 启动模式解析 ==========
    
    /// <summary>
    /// 初始化启动模式 - 由 View 在窗口加载后调用
    /// 
    /// 流程：
    /// 1. 在后台线程解析命令行和状态文件（避免阻塞 UI）
    /// 2. 解析完成后切回 UI 线程应用状态
    /// 
    /// 为什么不在构造函数里做？
    /// - AOT 编译下，同步 I/O 会阻塞窗口显示
    /// - 放到窗口创建后执行，用户体验更好
    /// </summary>
    public async void InitializeStartupMode()
    {
        var startupState = await Task.Run(ResolveStartupState);
        await Dispatcher.UIThread.InvokeAsync(() => ApplyStartupState(startupState));
    }

    /// <summary>
    /// 解析启动状态 - 决定安装器应该以哪种模式启动
    /// 
    /// 优先级（从高到低）：
    /// 1. --install 命令行参数 → 强制进入安装模式
    /// 2. --uninstall 参数 / 卸载程序可执行文件 / 本地有 manifest → 进入卸载模式
    /// 3. 有安装状态文件 → 进入"已安装"模式
    /// 4. 默认 → 进入安装模式
    /// 
    /// 判断"是否是卸载程序"：检查可执行文件名是否等于 InstallerMetadata.Uninstaller
    /// （通常卸载程序是单独的可执行文件，如 "MyApp-Uninstaller.exe"）
    /// </summary>
    private StartupState ResolveStartupState()
    {
        var args = Environment.GetCommandLineArgs().Skip(1).ToArray();
        var localManifest = Path.Combine(AppContext.BaseDirectory, InstallerMetadata.ManifestFileName);
        var executableName = Path.GetFileName(Environment.ProcessPath);
        var isUninstallerExecutable = executableName?.Equals(
            InstallerMetadata.Uninstaller,
            StringComparison.OrdinalIgnoreCase) == true;

        // 优先级1: 强制安装模式
        if (args.Any(arg => arg.Equals("--install", StringComparison.OrdinalIgnoreCase)))
        {
            return StartupState.Install();
        }

        // 优先级2: 卸载模式（命令行参数 / 卸载程序可执行文件 / 本地有 manifest）
        if (args.Any(arg => arg.Equals("--uninstall", StringComparison.OrdinalIgnoreCase)) ||
            isUninstallerExecutable ||
            File.Exists(localManifest))
        {
            return ResolveUninstallStartupState(localManifest);
        }

        // 优先级3: 已安装模式（有状态文件）
        var state = TryReadState();
        if (state is not null && File.Exists(state.ManifestPath))
        {
            return StartupState.Installed(state);
        }

        // 优先级4: 默认安装模式
        return StartupState.Install();
    }

    /// <summary>
    /// 解析卸载启动状态
    /// 尝试从本地 manifest 或状态文件读取卸载信息
    /// </summary>
    /// <param name="localManifest">本地 manifest 文件路径（安装目录下）</param>
    private StartupState ResolveUninstallStartupState(string localManifest)
    {
        // 优先使用本地 manifest（安装目录下的 install-manifest.json）
        if (File.Exists(localManifest))
        {
            var manifest = TryReadManifest(localManifest);
            return manifest is null
                ? StartupState.UninstallError($"Unable to read install manifest: {localManifest}")
                : StartupState.Uninstall(localManifest, manifest);
        }

        // 回退：使用状态文件中的 manifest 路径
        var state = TryReadState();
        if (state is not null && File.Exists(state.ManifestPath))
        {
            return StartupState.Uninstall(state.ManifestPath, state: state);
        }

        // 都无法找到 → 卸载错误
        return StartupState.UninstallError("No installed application state or install-manifest.json was found.");
    }

    /// <summary>
    /// 尝试读取安装状态文件（installer-state.json）
    /// 失败返回 null（不抛异常，因为状态文件可能不是关键路径）
    /// </summary>
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

    /// <summary>
    /// 尝试读取安装清单文件（install-manifest.json）
    /// 失败返回 null，并设置错误信息
    /// </summary>
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

    /// <summary>
    /// 从安装清单加载卸载所需信息
    /// 设置 UI 显示的已安装应用名称、版本、位置等
    /// </summary>
    private void LoadManifestForUninstall(InstallManifest manifest, string manifestPath)
    {
        InstalledAppName = manifest.AppName;
        InstalledVersion = manifest.Version;
        InstalledLocation = manifest.InstallLocation;
        InstallDirectory = manifest.InstallLocation;
        _uninstallManifestPath = manifestPath;
    }

    /// <summary>
    /// 从安装状态加载卸载所需信息
    /// 与 LoadManifestForUninstall 类似，但数据源是 installer-state.json
    /// </summary>
    private void LoadInstalledState(InstallState state)
    {
        InstalledAppName = state.AppName;
        InstalledVersion = state.Version;
        InstalledLocation = state.InstallLocation;
        InstallDirectory = state.InstallLocation;
        _uninstallManifestPath = state.ManifestPath;
    }

    /// <summary>
    /// 应用启动状态 - 根据解析结果切换到对应页面
    /// 此方法在 UI 线程调用
    /// </summary>
    private void ApplyStartupState(StartupState startupState)
    {
        switch (startupState.Mode)
        {
            case StartupMode.Install:
                // 全新安装：显示欢迎页面
                CurrentPage = InstallPage.Welcome;
                break;
                
            case StartupMode.Installed:
                // 已安装：显示"已安装"页面（可选修复/卸载）
                if (startupState.State is not null)
                {
                    LoadInstalledState(startupState.State);
                    CurrentPage = InstallPage.Installed;
                }
                break;
                
            case StartupMode.Uninstall:
                // 卸载模式：显示卸载确认页面
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
                // 卸载错误：显示错误信息
                ErrorMessage = startupState.ErrorMessage;
                CurrentPage = InstallPage.Uninstall;
                break;
        }
    }

    // ========== 命令实现 ==========
    // 使用 [RelayCommand] 特性，Source Generator 会自动生成对应的 *Command 属性
    // 例如：[RelayCommand] void BrowseAsync() → 生成 BrowseCommand 属性

    /// <summary>
    /// 浏览文件夹命令 - 打开文件夹选择对话框
    /// CanExecute: !IsInstalling（安装中不允许选择文件夹）
    /// </summary>
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

    /// <summary>
    /// 返回命令 - 返回上一个页面
    /// 
    /// 页面导航逻辑：
    /// - Directory → Welcome
    /// - Progress → Directory
    /// - 其他 → 保持不变
    /// </summary>
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

    /// <summary>
    /// 下一步命令 - 前进到下一个页面
    /// 
    /// 页面导航逻辑：
    /// - Welcome → Directory
    /// - 其他 → 保持不变
    /// </summary>
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

    /// <summary>
    /// 安装命令 - 执行安装操作（核心逻辑）
    /// 
    /// 安装流程：
    /// 1. 切换到进度页面，设置安装状态
    /// 2. 展开环境变量（如 %LocalAppData%）
    /// 3. 调用 _payloadExtractor.ExtractAsync() 解压文件
    /// 4. 调用 _installCompletionService.CompleteAsync() 完成后处理
    ///    （创建快捷方式、写注册表、生成 manifest 等）
    /// 5. 切换到完成页面
    /// 
    /// 异常处理：
    /// - OperationCanceledException: 用户取消
    /// - UnauthorizedAccessException: 权限不足（如写入系统目录）
    /// - IOException/InvalidDataException 等: 文件操作失败
    /// </summary>
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
            // 支持用户输入环境变量路径，例如 %LocalAppData%\MyAvaloniaApp
            var target = Path.GetFullPath(Environment.ExpandEnvironmentVariables(InstallDirectory.Trim()));
            InstallDirectory = target;

            // 创建进度回调，用于更新 UI
            var progress = new Progress<InstallProgress>(OnInstallProgressChanged);
            
            // 步骤1: 解压安装包到目标目录
            var extractionResult = await _payloadExtractor.ExtractAsync(target, progress, CancellationToken.None);
            
            // 步骤2: 安装后处理（快捷方式、注册表、manifest 等）
            await _installCompletionService.CompleteAsync(target, extractionResult, CancellationToken.None);

            // 安装成功
            StatusText = "Installation completed.";
            CurrentFile = string.Empty;
            ProgressPercent = 100;
            CurrentPage = InstallPage.Complete;
        }
        catch (OperationCanceledException)
        {
            // 用户取消安装
            ErrorMessage = "Installation was canceled.";
            StatusText = "Installation canceled.";
            CurrentPage = InstallPage.Directory;
        }
        catch (UnauthorizedAccessException ex)
        {
            // 权限不足（如尝试写入 C:\Program Files 但没有管理员权限）
            ErrorMessage = $"No permission to write to the selected folder. {ex.Message}";
            StatusText = "Installation failed.";
            CurrentPage = InstallPage.Directory;
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or InvalidOperationException or NotSupportedException)
        {
            // 其他可预期的错误（文件损坏、磁盘满等）
            ErrorMessage = ex.Message;
            StatusText = "Installation failed.";
            CurrentPage = InstallPage.Directory;
        }
        finally
        {
            // 无论成功失败，都要重置安装状态
            IsInstalling = false;
        }
    }

    /// <summary>
    /// 显示卸载页面命令 - 从"已安装"页面跳转到卸载确认页面
    /// </summary>
    [RelayCommand]
    private void ShowUninstall()
    {
        ErrorMessage = null;
        CurrentPage = InstallPage.Uninstall;
    }

    /// <summary>
    /// 升级命令 - 预留功能（当前版本未实现）
    /// </summary>
    [RelayCommand]
    private void Upgrade()
    {
        ErrorMessage = "Upgrade is reserved for a later version.";
    }

    /// <summary>
    /// 修复命令 - 预留功能（当前版本未实现）
    /// </summary>
    [RelayCommand]
    private void Repair()
    {
        ErrorMessage = "Repair is reserved for a later version.";
    }

    /// <summary>
    /// 开始卸载命令 - 执行卸载操作
    /// 
    /// 卸载流程：
    /// 1. 检查卸载清单路径是否存在
    /// 2. 切换到卸载进度页面
    /// 3. 调用 _uninstallService.UninstallAsync() 执行卸载
    /// 4. 根据结果切换到卸载完成或卸载页面（显示错误）
    /// </summary>
    [RelayCommand]
    private async Task StartUninstallAsync()
    {
        ErrorMessage = null;
        
        // 安全检查：必须有卸载清单才能卸载
        if (string.IsNullOrWhiteSpace(_uninstallManifestPath))
        {
            ErrorMessage = "Install manifest was not found. Unable to uninstall.";
            return;
        }

        // 初始化卸载状态
        IsUninstalling = true;
        UninstallProgressPercent = 0;
        UninstallStatusText = "Preparing uninstall...";
        CurrentPage = InstallPage.UninstallProgress;

        // 创建进度回调
        var progress = new Progress<UninstallProgress>(value =>
        {
            UninstallProgressPercent = value.Percent;
            UninstallStatusText = value.Status;
        });

        // 在后台线程执行卸载（避免阻塞 UI）
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

        // 保存卸载日志路径（供用户查看）
        UninstallLogPath = result.LogPath;
        IsUninstalling = false;

        if (result.Succeeded)
        {
            // 卸载成功
            UninstallProgressPercent = 100;
            UninstallStatusText = "Uninstall completed.";
            CurrentPage = InstallPage.UninstallComplete;
        }
        else
        {
            // 卸载失败，显示错误并返回卸载确认页面
            ErrorMessage = result.ErrorMessage;
            UninstallStatusText = "Uninstall failed.";
            CurrentPage = InstallPage.Uninstall;
        }
    }

    /// <summary>
    /// 取消命令 - 关闭安装器（任何页面都可以通过取消按钮退出）
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        Shutdown();
    }

    /// <summary>
    /// 完成命令 - 安装/卸载完成后点击"完成"按钮
    /// 
    /// 逻辑：
    /// 1. 如果勾选了"安装后启动"，则启动已安装的应用程序
    /// 2. 关闭安装器
    /// </summary>
    [RelayCommand]
    private void Finish()
    {
        if (LaunchAfterInstall)
        {
            TryLaunchInstalledApp();
        }

        Shutdown();
    }

    // ========== 辅助方法 ==========
    
    /// <summary>
    /// 安装进度变更回调 - 由 _payloadExtractor.ExtractAsync() 的进度报告触发
    /// 更新 UI 上的进度条和状态文本
    /// </summary>
    private void OnInstallProgressChanged(InstallProgress progress)
    {
        ProgressPercent = progress.Percent;
        CurrentFile = progress.CurrentFile;
        StatusText = string.IsNullOrWhiteSpace(progress.CurrentFile)
            ? "Extracting files..."
            : $"Extracting {progress.CurrentFile}";
    }

    /// <summary>
    /// CanExecute 方法 - 控制 BrowseCommand 是否可用
    /// 安装中不允许浏览文件夹
    /// </summary>
    private bool CanBrowse()
    {
        return !IsInstalling;
    }

    /// <summary>
    /// CanExecute 方法 - 控制 BackCommand 是否可用
    /// 仅在 Directory 或 Progress 页面且不在安装中时可用
    /// </summary>
    private bool CanGoBack()
    {
        return !IsInstalling && CurrentPage is InstallPage.Directory or InstallPage.Progress;
    }

    /// <summary>
    /// CanExecute 方法 - 控制 NextCommand 是否可用
    /// 仅在 Welcome 页面且不在安装中时可用
    /// </summary>
    private bool CanGoNext()
    {
        return !IsInstalling && CurrentPage == InstallPage.Welcome;
    }

    /// <summary>
    /// CanExecute 方法 - 控制 InstallCommand 是否可用
    /// 
    /// 条件：
    /// 1. 不在安装中
    /// 2. 当前在 Directory 页面（选择安装目录页面）
    /// 3. 安装目录不为空
    /// </summary>
    private bool CanInstall()
    {
        return !IsInstalling &&
               CurrentPage == InstallPage.Directory &&
               !string.IsNullOrWhiteSpace(InstallDirectory);
    }

    /// <summary>
    /// 尝试启动已安装的应用程序
    /// 
    /// 启动逻辑：
    /// 1. 拼接应用程序路径（InstallDirectory + MainExe）
    /// 2. 检查文件是否存在
    /// 3. 使用 Process.Start 启动（UseShellExecute = true，让系统处理）
    /// 
    /// 注意：不修改系统 PATH 或注册表，只启动本地文件
    /// </summary>
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

    /// <summary>
    /// 关闭应用程序
    /// 使用 Avalonia 的 IClassicDesktopStyleApplicationLifetime.Shutdown()
    /// </summary>
    private static void Shutdown()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    // ========== 内部类型 ==========
    
    /// <summary>
    /// 启动模式枚举 - 表示安装器应该以哪种模式启动
    /// </summary>
    private enum StartupMode
    {
        /// <summary>全新安装模式</summary>
        Install,
        /// <summary>已安装模式（显示"已安装"页面）</summary>
        Installed,
        /// <summary>卸载模式（显示卸载确认页面）</summary>
        Uninstall,
        /// <summary>卸载错误模式（无法找到卸载信息）</summary>
        UninstallError
    }

    /// <summary>
    /// 启动状态记录 - 封装解析后的启动信息
    /// 
    /// 使用 record 类型，不可变，适合作为返回值
    /// 工厂方法：Install()、Installed()、Uninstall()、UninstallError()
    /// </summary>
    /// <param name="Mode">启动模式</param>
    /// <param name="ManifestPath">Manifest 文件路径（卸载时需要）</param>
    /// <param name="Manifest">Manifest 内容（卸载时需要）</param>
    /// <param name="State">安装状态（已安装模式需要）</param>
    /// <param name="ErrorMessage">错误信息（卸载错误模式需要）</param>
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
