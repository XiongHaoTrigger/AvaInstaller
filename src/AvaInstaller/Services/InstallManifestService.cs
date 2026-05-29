using System.Text.Json;
using AvaInstaller.Models;
using AvaInstaller.Models.Json;

namespace AvaInstaller.Services;

/// <summary>
/// 安装清单读写服务。
/// 负责 install-manifest.json 的序列化和反序列化，
/// 使用 Native AOT 友好的源生成 JSON 上下文。
/// </summary>
public sealed class InstallManifestService
{
    /// <summary>
    /// 获取指定安装目录下的清单文件完整路径。
    /// </summary>
    /// <param name="installLocation">安装目录</param>
    /// <returns>install-manifest.json 的完整路径</returns>
    public string GetManifestPath(string installLocation)
    {
        return Path.Combine(installLocation, InstallerMetadata.ManifestFileName);
    }

    /// <summary>
    /// 异步写入安装清单文件。
    /// </summary>
    /// <param name="manifest">安装清单对象</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task WriteAsync(InstallManifest manifest, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(manifest.InstallLocation);
        var path = GetManifestPath(manifest.InstallLocation);
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(
            stream,
            manifest,
            InstallerJsonContext.Default.InstallManifest,
            cancellationToken);
    }

    /// <summary>
    /// 异步读取安装清单文件。
    /// </summary>
    /// <param name="manifestPath">清单文件的完整路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>安装清单对象</returns>
    /// <exception cref="InvalidDataException">清单文件内容为空时抛出</exception>
    public async Task<InstallManifest> ReadAsync(string manifestPath, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(manifestPath);
        var manifest = await JsonSerializer.DeserializeAsync(
            stream,
            InstallerJsonContext.Default.InstallManifest,
            cancellationToken);

        return manifest ?? throw new InvalidDataException($"Manifest is empty: {manifestPath}");
    }
}
