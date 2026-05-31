using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using AvaInstaller.Services;
using AvaInstaller.ViewModels;
using AvaInstaller.Views;
using Microsoft.Extensions.DependencyInjection;


namespace AvaInstaller;

/// <summary>
/// Avalonia Application 类。
/// 
/// 负责：
/// 1. 框架初始化（加载 XAML）
/// 2. 配置依赖注入容器
/// 3. 创建主窗口并绑定 ViewModel
/// 4. 调用 ViewModel 启动模式解析
/// 
/// DI 策略：
/// - Services 注册为 Singleton（整个应用生命周期内共享）
/// - ViewModel 注册为 Transient（每次获取新实例）
/// </summary>
public partial class App : Application
{
    /// <summary>依赖注入服务提供器</summary>
    private ServiceProvider? _services;

    /// <summary>
    /// 框架初始化 - 加载 XAML 资源。
    /// </summary>
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// 框架初始化完成后回调。
    /// 桌面端启动时集中配置依赖注入，主窗口只依赖 ViewModel。
    /// </summary>
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _services = ConfigureServices();
            var viewModel = _services.GetRequiredService<MainWindowViewModel>();
            desktop.MainWindow = new MainWindow
            {
                DataContext = viewModel,
            };

            // 窗口创建后异步解析启动模式，避免状态文件 I/O 阻塞首帧显示。
            _ = viewModel.InitializeStartupMode();
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// 配置依赖注入容器。
    /// 服务注册保持简单：安装、卸载、状态文件分别由独立服务负责。
    /// </summary>
    /// <returns>构建好的 ServiceProvider</returns>
    private ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IPayloadExtractor, PayloadExtractor>();
        services.AddSingleton<IFolderPickerService, FolderPickerService>();
        services.AddSingleton<IDiskService, DiskService>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<InstallManifestService>();
        services.AddSingleton<IManifestService, ManifestService>();
        services.AddSingleton<InstallStateService>();
        services.AddSingleton<ShortcutService>();
        services.AddSingleton<IShortcutService>(provider => provider.GetRequiredService<ShortcutService>());
        services.AddSingleton<SelfDeleteService>();
        services.AddSingleton<UninstallService>();
        services.AddSingleton<InstallCompletionService>();
        services.AddSingleton<IInstallerService, InstallerService>();
        services.AddTransient<MainWindowViewModel>();
        return services.BuildServiceProvider();
    }
}
