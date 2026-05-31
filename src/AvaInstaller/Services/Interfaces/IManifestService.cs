using AvaInstaller.Models;

namespace AvaInstaller.Services;

/// <summary>
/// 安装清单服务接口。
/// 封装 install-manifest.json 的读写行为。
/// </summary>
public interface IManifestService
{
    /// <summary>
    /// 读取安装清单。
    /// </summary>
    Task<InstallManifest> ReadAsync(string path, CancellationToken cancellationToken);

    /// <summary>
    /// 写入安装清单。
    /// </summary>
    Task WriteAsync(InstallManifest manifest, CancellationToken cancellationToken);
}
