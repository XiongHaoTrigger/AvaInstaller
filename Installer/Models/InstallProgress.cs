namespace Installer.Models;

public sealed record InstallProgress(
    int Percent,
    string CurrentFile,
    long BytesExtracted,
    long TotalBytes);
