using Avalonia;
using AvaInstaller.Services;
using System;

namespace AvaInstaller;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static int Main(string[] args)
    {
        if (args.Any(arg => arg.Equals("--silent-uninstall", StringComparison.OrdinalIgnoreCase)))
        {
            return RunSilentUninstall();
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        return 0;
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    private static int RunSilentUninstall()
    {
        try
        {
            var manifestService = new InstallManifestService();
            var stateService = new InstallStateService();
            var uninstallService = new UninstallService(
                manifestService,
                stateService,
                new ShortcutService(),
                new SelfDeleteService());

            var manifestPath = ResolveManifestPath(stateService);
            var result = uninstallService
                .UninstallAsync(manifestPath, preserveUserData: true, silent: true, progress: null, CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            return result.Succeeded ? 0 : 1;
        }
        catch
        {
            return 1;
        }
    }

    private static string ResolveManifestPath(InstallStateService stateService)
    {
        var localManifest = Path.Combine(AppContext.BaseDirectory, Models.InstallerMetadata.ManifestFileName);
        if (File.Exists(localManifest))
        {
            return localManifest;
        }

        var state = stateService.TryReadAsync(CancellationToken.None).GetAwaiter().GetResult();
        if (state is not null && File.Exists(state.ManifestPath))
        {
            return state.ManifestPath;
        }

        throw new FileNotFoundException("Install manifest was not found.");
    }
}
