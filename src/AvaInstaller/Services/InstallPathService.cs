using AvaInstaller.Models;

namespace AvaInstaller.Services;

/// <summary>
/// 安装路径工具服务（静态类）。
/// 提供安装器各组件所需的标准路径计算，
/// 包括安装状态目录、日志目录，以及自定义路径模式展开（%Desktop%、%StartMenu% 等）。
/// </summary>
public static class InstallPathService
{
    /// <summary>
    /// 获取已安装应用的根目录。
    /// 路径：%LocalAppData%\AvaInstaller\InstalledApps
    /// </summary>
    public static string GetInstalledAppsRoot()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AvaInstaller",
            "InstalledApps");
    }

    /// <summary>
    /// 获取指定应用的安装状态目录。
    /// 路径：%LocalAppData%\AvaInstaller\InstalledApps\{AppId}
    /// </summary>
    /// <param name="appId">应用程序标识符（默认使用 InstallerMetadata.AppId）</param>
    public static string GetInstallStateDirectory(string appId = InstallerMetadata.AppId)
    {
        return Path.Combine(GetInstalledAppsRoot(), appId);
    }

    /// <summary>
    /// 获取安装状态文件完整路径。
    /// 路径：%LocalAppData%\AvaInstaller\InstalledApps\{AppId}\install-state.json
    /// </summary>
    /// <param name="appId">应用程序标识符（默认使用 InstallerMetadata.AppId）</param>
    public static string GetInstallStatePath(string appId = InstallerMetadata.AppId)
    {
        return Path.Combine(GetInstallStateDirectory(appId), "install-state.json");
    }

    /// <summary>
    /// 获取卸载日志目录。
    /// 路径：%LocalAppData%\AvaInstaller\Logs\{AppId}
    /// </summary>
    /// <param name="appId">应用程序标识符（默认使用 InstallerMetadata.AppId）</param>
    public static string GetLogsDirectory(string appId = InstallerMetadata.AppId)
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AvaInstaller",
            "Logs",
            appId);
    }

    /// <summary>
    /// 展开自定义路径占位符和环境变量。
    /// 支持的占位符：
    /// - %Desktop% → 桌面目录
    /// - %StartMenu% → 开始菜单目录（用于快捷方式路径）
    /// - %LocalAppData% → 本地应用数据目录
    /// 同时展开标准 Windows 环境变量（如 %APPDATA%）。
    /// </summary>
    /// <param name="path">包含占位符的路径</param>
    /// <returns>展开后的绝对路径</returns>
    public static string ExpandKnownPath(string path)
    {
        var expanded = path
            .Replace("%Desktop%", Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), StringComparison.OrdinalIgnoreCase)
            .Replace("%StartMenu%", Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), StringComparison.OrdinalIgnoreCase)
            .Replace("%LocalAppData%", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), StringComparison.OrdinalIgnoreCase);

        return Environment.ExpandEnvironmentVariables(expanded);
    }
}
