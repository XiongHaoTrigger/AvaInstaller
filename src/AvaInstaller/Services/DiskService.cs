using AvaInstaller.Models;

namespace AvaInstaller.Services;

/// <summary>
/// 磁盘信息服务。
/// </summary>
public sealed class DiskService : IDiskService
{
    /// <inheritdoc />
    public IReadOnlyList<DriveDisk> GetAvailableDisks()
    {
        return DriveInfo.GetDrives()
            .Where(drive => drive.IsReady && drive.DriveType == DriveType.Fixed)
            .Select(drive => new DriveDisk(
                drive.Name,
                drive.RootDirectory.FullName,
                drive.TotalSize,
                drive.AvailableFreeSpace))
            .ToArray();
    }
}
