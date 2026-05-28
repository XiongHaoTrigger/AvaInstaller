namespace AvaInstaller.Models;

public sealed record InstallProgress(
    int Percent,
    string CurrentFile,
    long BytesExtracted,
    long TotalBytes);
