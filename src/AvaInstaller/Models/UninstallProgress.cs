/// <summary>
/// 卸载进度快照记录。
/// UninstallService 通过 IProgress&lt;UninstallProgress&gt; 推送给 ViewModel 更新 UI。
/// </summary>
/// <param name="Percent">当前进度百分比 (0-100)</param>
/// <param name="Status">当前状态描述文本</param>
namespace AvaInstaller.Models;

public sealed record UninstallProgress(int Percent, string Status);
