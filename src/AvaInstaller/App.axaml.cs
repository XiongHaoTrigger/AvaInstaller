using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using AvaInstaller.Services;
using AvaInstaller.ViewModels;
using AvaInstaller.Views;
using Microsoft.Extensions.DependencyInjection;

namespace AvaInstaller;

public partial class App : Application
{
    private ServiceProvider? _services;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // 桌面端启动时集中配置依赖注入，主窗口只依赖 ViewModel。
            _services = ConfigureServices();
            var viewModel = _services.GetRequiredService<MainWindowViewModel>();
            desktop.MainWindow = new MainWindow
            {
                DataContext = viewModel,
            };

            viewModel.InitializeStartupMode();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private ServiceProvider ConfigureServices()
    {
        // 服务注册保持简单：安装、卸载、状态文件分别由独立服务负责。
        var services = new ServiceCollection();
        services.AddSingleton<IPayloadExtractor, PayloadExtractor>();
        services.AddSingleton<IFolderPickerService, FolderPickerService>();
        services.AddSingleton<InstallManifestService>();
        services.AddSingleton<InstallStateService>();
        services.AddSingleton<ShortcutService>();
        services.AddSingleton<SelfDeleteService>();
        services.AddSingleton<UninstallService>();
        services.AddSingleton<InstallCompletionService>();
        services.AddTransient<MainWindowViewModel>();
        return services.BuildServiceProvider();
    }
}
