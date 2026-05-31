using Avalonia.Controls;

namespace AvaInstaller.Views.Pages;

/// <summary>
/// 安装路径页面视图。
/// 仅负责加载 XAML，交互逻辑由 PathPageViewModel 提供。
/// </summary>
public partial class PathPage : UserControl
{
    public PathPage()
    {
        InitializeComponent();
    }
}
