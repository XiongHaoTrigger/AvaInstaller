namespace AvaInstaller.ViewModels;

/// <summary>
/// States for the install flow only. UI page selection is represented by InstallPage.
/// </summary>
public enum InstallFlowState
{
    /// <summary>The install flow has not started.</summary>
    Idle,

    /// <summary>Installation is running.</summary>
    Installing,

    /// <summary>Installation completed successfully.</summary>
    Completed,

    /// <summary>Installation failed and can be retried.</summary>
    Failed
}
