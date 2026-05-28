namespace AvaInstaller.Models;

// 解压进度快照，由 PayloadExtractor 通过 IProgress 推送给 ViewModel。
public sealed record InstallProgress(
    int Percent,
    string CurrentFile,
    long BytesExtracted,
    long TotalBytes);
