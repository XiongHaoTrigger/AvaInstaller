using AvaInstaller.Models;

namespace AvaInstaller.Services;

/// <summary>
/// 标准命名的清单服务适配器。
/// 内部复用现有 InstallManifestService，方便目录结构和服务命名保持统一。
/// </summary>
public sealed class ManifestService : IManifestService
{
    private readonly InstallManifestService _inner;

    /// <summary>
    /// 创建清单服务适配器。
    /// </summary>
    public ManifestService(InstallManifestService inner)
    {
        _inner = inner;
    }

    /// <inheritdoc />
    public Task<InstallManifest> ReadAsync(string path, CancellationToken cancellationToken)
    {
        return _inner.ReadAsync(path, cancellationToken);
    }

    /// <inheritdoc />
    public Task WriteAsync(InstallManifest manifest, CancellationToken cancellationToken)
    {
        return _inner.WriteAsync(manifest, cancellationToken);
    }
}
