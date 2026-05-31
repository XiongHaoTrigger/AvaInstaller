namespace AvaInstaller.Models;

/// <summary>
/// 一次安装流程的用户选项。
/// 主流程目前直接使用 ViewModel 属性，后续可用该模型集中传递安装参数。
/// </summary>
public sealed class InstallOptions
{
    /// <summary>目标安装目录。</summary>
    public string InstallDirectory { get; set; } = string.Empty;

    /// <summary>安装完成后是否启动应用。</summary>
    public bool LaunchAfterInstall { get; set; } = true;

    /// <summary>被选中的组件标识集合。</summary>
    public IList<string> SelectedComponentIds { get; } = new List<string>();
}
