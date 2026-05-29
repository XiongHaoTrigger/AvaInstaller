namespace AvaInstaller.Models;

/// <summary>
/// 安装器产品元数据静态常量。
/// 集中维护安装器写入 manifest/state 的产品元数据，
/// 需要更改品牌名或版本号时只需修改此类。
/// </summary>
public static class InstallerMetadata
{
    /// <summary>应用程序名称，用于显示和安装目录命名</summary>
    public const string AppName = "MyAvaloniaApp";

    /// <summary>应用程序唯一标识符（反向域名格式）</summary>
    public const string AppId = "com.example.myavaloniaapp";

    /// <summary>应用程序版本号</summary>
    public const string Version = "1.0.0";

    /// <summary>发布者名称</summary>
    public const string Publisher = "Your Company";

    /// <summary>主程序可执行文件名</summary>
    public const string MainExe = "MyAvaloniaApp.exe";

    /// <summary>卸载程序可执行文件名（安装后复制到安装目录）</summary>
    public const string Uninstaller = "uninstall.exe";

    /// <summary>安装清单文件名（记录安装的文件和目录）</summary>
    public const string ManifestFileName = "install-manifest.json";
}
