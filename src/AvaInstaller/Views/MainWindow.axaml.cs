using Avalonia.Controls;

namespace AvaInstaller.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        // 代码隐藏层仅负责加载 XAML，业务交互由 MainWindowViewModel 承担。
        InitializeComponent();
    }
}
