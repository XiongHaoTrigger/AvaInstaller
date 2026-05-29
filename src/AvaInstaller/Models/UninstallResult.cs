/// <summary>
/// 卸载结果记录。
/// UninstallService.UninstallAsync 的返回值，
/// 包含成功/失败状态、错误信息和日志路径。
/// </summary>
/// <param name="Succeeded">卸载是否成功</param>
/// <param name="ErrorMessage">失败时的错误信息，成功时为 null</param>
/// <param name="LogPath">卸载日志文件的完整路径</param>
namespace AvaInstaller.Models;

public sealed record UninstallResult(bool Succeeded, string? ErrorMessage, string? LogPath);
