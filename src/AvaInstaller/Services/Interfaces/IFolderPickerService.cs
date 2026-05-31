namespace AvaInstaller.Services;

/// <summary>
/// 文件夹选择服务接口。
/// 封装系统文件夹选择对话框，允许用户在安装流程中选择安装目录。
/// </summary>
public interface IFolderPickerService
{
    /// <summary>
    /// 打开文件夹选择对话框。
    /// </summary>
    /// <param name="suggestedPath">建议打开的初始目录</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户选择的文件夹路径，取消时返回 null</returns>
    Task<string?> PickFolderAsync(string suggestedPath, CancellationToken cancellationToken);
}
