using CommunityToolkit.Mvvm.ComponentModel;


namespace AvaInstaller.ViewModels;

/// <summary>
/// ViewModel 抽象基类。
/// 继承 CommunityToolkit.Mvvm 的 ObservableObject，
/// 提供属性变更通知和命令绑定等 MVVM 基础设施。
/// 项目中所有 ViewModel 都应继承此类。
/// </summary>
public abstract class ViewModelBase : ObservableObject
{
}
