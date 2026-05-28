using AvaInstaller.Models;

namespace AvaInstaller.Services;

public static class InstallPathService
{
    public static string GetInstalledAppsRoot()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AvaInstaller",
            "InstalledApps");
    }

    public static string GetInstallStateDirectory(string appId = InstallerMetadata.AppId)
    {
        return Path.Combine(GetInstalledAppsRoot(), appId);
    }

    public static string GetInstallStatePath(string appId = InstallerMetadata.AppId)
    {
        return Path.Combine(GetInstallStateDirectory(appId), "install-state.json");
    }

    public static string GetLogsDirectory(string appId = InstallerMetadata.AppId)
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AvaInstaller",
            "Logs",
            appId);
    }

    public static string ExpandKnownPath(string path)
    {
        var expanded = path
            .Replace("%Desktop%", Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), StringComparison.OrdinalIgnoreCase)
            .Replace("%StartMenu%", Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), StringComparison.OrdinalIgnoreCase)
            .Replace("%LocalAppData%", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), StringComparison.OrdinalIgnoreCase);

        return Environment.ExpandEnvironmentVariables(expanded);
    }
}
