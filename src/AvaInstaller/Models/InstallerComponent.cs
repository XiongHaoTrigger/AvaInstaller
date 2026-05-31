namespace AvaInstaller.Models;

/// <summary>
/// 可选安装组件模型。
/// 当前安装器默认安装完整 payload，后续需要组件选择页时可直接使用该模型承载组件状态。
/// </summary>
public sealed class InstallerComponent
{
    /// <summary>组件唯一标识。</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>组件显示名称。</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>组件说明。</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>组件占用空间，单位为字节。</summary>
    public long SizeBytes { get; init; }

    /// <summary>是否为必装组件。</summary>
    public bool IsRequired { get; init; }

    /// <summary>是否已被用户选择。</summary>
    public bool IsSelected { get; set; }
}
