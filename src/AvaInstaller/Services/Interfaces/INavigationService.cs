using AvaInstaller.Models;

namespace AvaInstaller.Services;

/// <summary>
/// 页面导航服务接口。
/// 当前项目由主 ViewModel 直接切换步骤，保留该接口便于后续把导航规则独立出来。
/// </summary>
public interface INavigationService
{
    /// <summary>当前步骤。</summary>
    InstallerStep CurrentStep { get; }

    /// <summary>
    /// 导航到指定步骤。
    /// </summary>
    /// <param name="step">目标步骤。</param>
    void NavigateTo(InstallerStep step);
}
