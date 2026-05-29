using AvaInstaller.Models;

namespace AvaInstaller.Services;

/// <summary>
/// Payload 解压服务接口。
/// 负责从安装包（payload.zip）中解压文件到目标安装目录。
/// </summary>
public interface IPayloadExtractor
{
    /// <summary>
    /// 异步解压安装包到目标目录。
    /// </summary>
    /// <param name="targetDirectory">目标安装目录</param>
    /// <param name="progress">进度报告回调，用于更新 UI</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>解压结果，包含提取的文件和目录列表</returns>
    /// <exception cref="InvalidOperationException">目标目录为空时抛出</exception>
    /// <exception cref="FileNotFoundException">未找到 payload.zip 时抛出</exception>
    /// <exception cref="IOException">磁盘空间不足时抛出</exception>
    /// <exception cref="InvalidDataException">payload 中包含不安全路径时抛出</exception>
    Task<PayloadExtractionResult> ExtractAsync(
        string targetDirectory,
        IProgress<InstallProgress> progress,
        CancellationToken cancellationToken);
}
