using AvaInstaller.Models;

namespace AvaInstaller.Services;

/// <summary>
/// 安装流程服务。
/// 将 payload 解压和安装完成收尾组合为一个更高层的安装入口。
/// </summary>
public sealed class InstallerService : IInstallerService
{
    private readonly IPayloadExtractor _payloadExtractor;
    private readonly InstallCompletionService _completionService;

    /// <summary>
    /// 创建安装流程服务。
    /// </summary>
    public InstallerService(IPayloadExtractor payloadExtractor, InstallCompletionService completionService)
    {
        _payloadExtractor = payloadExtractor;
        _completionService = completionService;
    }

    /// <inheritdoc />
    public async Task InstallAsync(
        InstallOptions options,
        IProgress<InstallProgress> progress,
        CancellationToken cancellationToken)
    {
        var extractionResult = await _payloadExtractor.ExtractAsync(
            options.InstallDirectory,
            progress,
            cancellationToken);
        await _completionService.CompleteAsync(options.InstallDirectory, extractionResult, cancellationToken);
    }
}
