/// <summary>
/// 安装进度快照记录。
/// PayloadExtractor 通过 IProgress&lt;InstallProgress&gt; 推送给 ViewModel 更新 UI。
/// </summary>
/// <param name="Percent">当前进度百分比 (0-100)</param>
/// <param name="CurrentFile">当前正在解压的文件名</param>
/// <param name="BytesExtracted">已解压的字节数</param>
/// <param name="TotalBytes">需要解压的总字节数</param>
namespace AvaInstaller.Models;

public sealed record InstallProgress(
    int Percent,
    string CurrentFile,
    long BytesExtracted,
    long TotalBytes);
