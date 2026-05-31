using AvaInstaller.Models;

namespace AvaInstaller.Services;

/// <summary>
/// 简单导航服务。
/// 目前主 ViewModel 仍直接维护 CurrentPage；该服务用于后续把向导导航规则独立出来。
/// </summary>
public sealed class NavigationService : INavigationService
{
    /// <inheritdoc />
    public InstallerStep CurrentStep { get; private set; } = InstallerStep.Welcome;

    /// <inheritdoc />
    public void NavigateTo(InstallerStep step)
    {
        CurrentStep = step;
    }
}
