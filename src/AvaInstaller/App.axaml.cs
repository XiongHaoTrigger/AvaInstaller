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
            _services = ConfigureServices();
            desktop.MainWindow = new MainWindow
            {
                DataContext = _services.GetRequiredService<MainWindowViewModel>(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IPayloadExtractor, PayloadExtractor>();
        services.AddSingleton<IFolderPickerService, FolderPickerService>();
        services.AddTransient<MainWindowViewModel>();
        return services.BuildServiceProvider();
    }
}
