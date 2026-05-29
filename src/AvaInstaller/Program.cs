
using Avalonia;
using AvaInstaller.Services;
using System;

namespace AvaInstaller;

/// <summary>
/// 应用程序入口点。
/// 
/// 启动流程：
/// 1. 检查是否有 --silent-uninstall 命令行参数：
///    - 有 → 无 UI 静默卸载，返回 0（成功）或 1（失败）
/// 2. 正常启动 → 构建 Avalonia 应用并以经典桌面模式运行
/// 
/// 注意：在 AppMain 调用之前不要使用 Avalonia、第三方 API 或
/// SynchronizationContext 相关代码——此时尚未初始化。
/// </summary>
sealed class Program
{
    /// <summary>
    /// 应用程序主入口点。
    /// </summary>
    /// <param name="args">命令行参数</param>
    /// <returns>静默卸载模式返回退出码，正常模式启动后由 Avalonia 控制</returns>
    [STAThread]
    public static int Main(string[] args)
    {
        // 静默卸载模式：无 UI，仅做卸载操作
        if (args.Any(arg => arg.Equals("--silent-uninstall", StringComparison.OrdinalIgnoreCase)))
        {
            return RunSilentUninstall();
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        return 0;
    }

    /// <summary>
    /// 配置并构建 Avalonia 应用（供 Visual Studio 设计器等使用）。
    /// </summary>
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    /// <summary>
    /// 无 UI 静默卸载。
    /// 直接构造服务层执行卸载，不启动 Avalonia UI。
    /// </summary>
    /// <returns>0 表示成功，1 表示失败</returns>
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

    /// <summary>
    /// 解析安装清单文件路径。
    /// 优先使用本地清单文件，回退到安装状态文件中的路径。
    /// </summary>
    /// <param name="stateService">安装状态服务</param>
    /// <returns>安装清单文件完整路径</returns>
    /// <exception cref="FileNotFoundException">未找到清单文件时抛出</exception>
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
