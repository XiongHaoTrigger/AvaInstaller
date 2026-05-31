using AvaInstaller.Models;

namespace AvaInstaller.Services;

/// <summary>
/// 快捷方式服务接口。
/// </summary>
public interface IShortcutService
{
    /// <summary>
    /// 根据安装清单创建快捷方式。
    /// </summary>
    Task CreateAsync(InstallManifest manifest, CancellationToken cancellationToken);
}
