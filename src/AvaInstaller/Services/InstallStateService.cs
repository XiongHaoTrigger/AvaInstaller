using System.Text.Json;
using AvaInstaller.Models;
using AvaInstaller.Models.Json;

namespace AvaInstaller.Services;

/// <summary>
/// 安装状态读写服务。
/// 负责 install-state.json 的序列化和反序列化，
/// 使用 Native AOT 友好的源生成 JSON 上下文。
/// 状态文件存储在用户级固定位置，不写入注册表。
/// </summary>
public sealed class InstallStateService
{
    /// <summary>
    /// 获取安装状态文件的完整路径。
    /// </summary>
    /// <param name="appId">应用程序标识符（默认使用 InstallerMetadata.AppId）</param>
    public string GetStatePath(string appId = InstallerMetadata.AppId)
    {
        return InstallPathService.GetInstallStatePath(appId);
    }

    /// <summary>
    /// 尝试读取安装状态。
    /// 文件不存在时返回 null，读取失败（JSON 格式错误、权限不足等）时也返回 null。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>安装状态对象，失败时返回 null</returns>
    public async Task<InstallState?> TryReadAsync(CancellationToken cancellationToken)
    {
        var path = GetStatePath();
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            await using var stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync(
                stream,
                InstallerJsonContext.Default.InstallState,
                cancellationToken);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// 异步写入安装状态文件。
    /// 自动创建必要的目录。
    /// </summary>
    /// <param name="state">安装状态对象</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task WriteAsync(InstallState state, CancellationToken cancellationToken)
    {
        var path = GetStatePath(state.AppId);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(
            stream,
            state,
            InstallerJsonContext.Default.InstallState,
            cancellationToken);
    }

    /// <summary>
    /// 删除安装状态文件。
    /// 如果状态文件所在目录为空，一并清理目录。
    /// </summary>
    /// <param name="appId">应用程序标识符（默认使用 InstallerMetadata.AppId）</param>
    public void Delete(string appId = InstallerMetadata.AppId)
    {
        var path = GetStatePath(appId);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        // 如果目录为空则一并删除，防止累积空目录
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory) &&
            Directory.Exists(directory) &&
            !Directory.EnumerateFileSystemEntries(directory).Any())
        {
            Directory.Delete(directory);
        }
    }
}
