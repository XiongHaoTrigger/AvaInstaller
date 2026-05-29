namespace AvaInstaller.ViewModels;

/// <summary>
/// Finite state machine for the install flow only.
/// </summary>
public sealed class InstallStateMachine
{
    private static readonly IReadOnlyDictionary<(InstallFlowState State, InstallerEvent Event), InstallFlowState> Transitions =
        new Dictionary<(InstallFlowState State, InstallerEvent Event), InstallFlowState>
        {
            [(InstallFlowState.Idle, InstallerEvent.BeginInstall)] = InstallFlowState.Installing,
            [(InstallFlowState.Failed, InstallerEvent.BeginInstall)] = InstallFlowState.Installing,
            [(InstallFlowState.Installing, InstallerEvent.CompleteInstall)] = InstallFlowState.Completed,
            [(InstallFlowState.Installing, InstallerEvent.FailInstall)] = InstallFlowState.Failed
        };

    public InstallStateMachine()
        : this(InstallFlowState.Idle)
    {
    }

    public InstallStateMachine(InstallFlowState initialState)
    {
        CurrentState = initialState;
    }

    public InstallFlowState CurrentState { get; private set; }

    public bool CanFire(InstallerEvent installerEvent)
    {
        return Transitions.ContainsKey((CurrentState, installerEvent));
    }

    public InstallFlowState Fire(InstallerEvent installerEvent)
    {
        if (!Transitions.TryGetValue((CurrentState, installerEvent), out var nextState))
        {
            throw new InvalidOperationException($"Invalid install flow transition: {CurrentState} -> {installerEvent}");
        }

        CurrentState = nextState;
        return CurrentState;
    }
}
