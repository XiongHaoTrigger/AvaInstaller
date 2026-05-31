namespace AvaInstaller.Models;

/// <summary>
/// 安装流程状态。
/// 该枚举只描述安装任务本身是否空闲、执行中、成功或失败，不直接决定 UI 页面。
/// </summary>
public enum InstallerState
{
    /// <summary>安装流程尚未开始。</summary>
    Idle,

    /// <summary>安装正在执行。</summary>
    Installing,

    /// <summary>安装已成功完成。</summary>
    Completed,

    /// <summary>安装失败，可重新尝试。</summary>
    Failed
}
