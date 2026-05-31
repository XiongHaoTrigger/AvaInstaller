using System.Collections.ObjectModel;
using AvaInstaller.Models;

namespace AvaInstaller.ViewModels.Pages;

/// <summary>
/// 组件选择页面 ViewModel。
/// 当前 payload 作为整体安装，后续拆分组件时可直接绑定 Components 集合。
/// </summary>
public sealed class ComponentsPageViewModel : ViewModelBase
{
    /// <summary>
    /// 初始化默认组件集合。
    /// </summary>
    public ComponentsPageViewModel()
    {
        Components =
        [
            new InstallerComponent
            {
                Id = "core",
                Name = "Core Application",
                Description = "核心程序和运行所需文件。",
                IsRequired = true,
                IsSelected = true
            }
        ];
    }

    /// <summary>可选安装组件集合。</summary>
    public ObservableCollection<InstallerComponent> Components { get; }
}
