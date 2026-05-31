namespace AvaInstaller.Models;

/// <summary>
/// 磁盘分区信息模型。
/// 用于安装路径页面展示可用空间和目标盘符状态。
/// </summary>
public sealed record DriveDisk(
    string Name,
    string RootPath,
    long TotalBytes,
    long AvailableBytes);
