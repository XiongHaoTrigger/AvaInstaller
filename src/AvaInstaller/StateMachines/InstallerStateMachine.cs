using AvaInstaller.Models;

namespace AvaInstaller.StateMachines;

/// <summary>
/// 安装流程有限状态机。
/// 只负责校验安装任务状态转换，页面导航仍由主 ViewModel 根据业务结果决定。
/// </summary>
public sealed class InstallerStateMachine
{
    private static readonly IReadOnlyDictionary<(InstallerState State, InstallerEvent Event), InstallerState> Transitions =
        new Dictionary<(InstallerState State, InstallerEvent Event), InstallerState>
        {
            [(InstallerState.Idle, InstallerEvent.BeginInstall)] = InstallerState.Installing,
            [(InstallerState.Failed, InstallerEvent.BeginInstall)] = InstallerState.Installing,
            [(InstallerState.Installing, InstallerEvent.CompleteInstall)] = InstallerState.Completed,
            [(InstallerState.Installing, InstallerEvent.FailInstall)] = InstallerState.Failed
        };

    /// <summary>
    /// 使用空闲状态初始化状态机。
    /// </summary>
    public InstallerStateMachine()
        : this(InstallerState.Idle)
    {
    }

    /// <summary>
    /// 使用指定初始状态初始化状态机，便于测试或恢复流程。
    /// </summary>
    /// <param name="initialState">初始安装状态。</param>
    public InstallerStateMachine(InstallerState initialState)
    {
        CurrentState = initialState;
    }

    /// <summary>
    /// 当前安装状态。
    /// </summary>
    public InstallerState CurrentState { get; private set; }

    /// <summary>
    /// 判断当前状态是否可以接收指定事件。
    /// </summary>
    /// <param name="installerEvent">待触发事件。</param>
    /// <returns>可以转换时返回 true。</returns>
    public bool CanFire(InstallerEvent installerEvent)
    {
        return Transitions.ContainsKey((CurrentState, installerEvent));
    }

    /// <summary>
    /// 触发状态转换。
    /// </summary>
    /// <param name="installerEvent">安装事件。</param>
    /// <returns>转换后的新状态。</returns>
    /// <exception cref="InvalidOperationException">当前状态不允许该事件时抛出。</exception>
    public InstallerState Fire(InstallerEvent installerEvent)
    {
        if (!Transitions.TryGetValue((CurrentState, installerEvent), out var nextState))
        {
            throw new InvalidOperationException($"Invalid install flow transition: {CurrentState} -> {installerEvent}");
        }

        CurrentState = nextState;
        return CurrentState;
    }
}
