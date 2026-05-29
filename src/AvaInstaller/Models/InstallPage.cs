namespace AvaInstaller.Models;

/// <summary>
/// UI pages shown by the installer window.
/// </summary>
public enum InstallPage
{
    /// <summary>Welcome page.</summary>
    Welcome,

    /// <summary>Install directory selection page.</summary>
    Directory,

    /// <summary>Install progress page.</summary>
    InstallProgress,

    /// <summary>Install completion page.</summary>
    InstallComplete,

    /// <summary>Maintenance page for an already installed app.</summary>
    Installed,

    /// <summary>Uninstall confirmation page.</summary>
    UninstallConfirm,

    /// <summary>Uninstall progress page.</summary>
    UninstallProgress,

    /// <summary>Uninstall completion page.</summary>
    UninstallComplete
}
