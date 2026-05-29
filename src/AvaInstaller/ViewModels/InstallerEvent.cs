namespace AvaInstaller.ViewModels;

/// <summary>
/// Events accepted by the install flow state machine.
/// </summary>
public enum InstallerEvent
{
    /// <summary>Installation started.</summary>
    BeginInstall,

    /// <summary>Installation completed successfully.</summary>
    CompleteInstall,

    /// <summary>Installation failed.</summary>
    FailInstall
}
