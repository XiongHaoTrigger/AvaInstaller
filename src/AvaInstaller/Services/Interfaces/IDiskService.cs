using AvaInstaller.Models;

namespace AvaInstaller.Services;

/// <summary>
/// 磁盘信息服务接口。
/// 用于安装路径页查询分区空间和可用容量。
/// </summary>
public interface IDiskService
{
    /// <summary>
    /// 获取当前机器可用于安装的磁盘列表。
    /// </summary>
    /// <returns>磁盘信息集合。</returns>
    IReadOnlyList<DriveDisk> GetAvailableDisks();
}
