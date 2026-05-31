using AvaInstaller.Models;

namespace AvaInstaller.Services;

/// <summary>
/// 安装服务接口。
/// 用于抽象完整安装流程，后续可把 MainWindowViewModel 中的安装编排下沉到服务层。
/// </summary>
public interface IInstallerService
{
    /// <summary>
    /// 执行安装。
    /// </summary>
    /// <param name="options">安装选项。</param>
    /// <param name="progress">安装进度回调。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task InstallAsync(
        InstallOptions options,
        IProgress<InstallProgress> progress,
        CancellationToken cancellationToken);
}
