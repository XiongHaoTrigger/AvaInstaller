namespace AvaInstaller.Models;

// 集中维护安装器写入 manifest/state 的产品元数据，后续改品牌名或版本只改这里。
public static class InstallerMetadata
{
    public const string AppName = "MyAvaloniaApp";
    public const string AppId = "com.example.myavaloniaapp";
    public const string Version = "1.0.0";
    public const string Publisher = "Your Company";
    public const string MainExe = "MyAvaloniaApp.exe";
    public const string Uninstaller = "uninstall.exe";
    public const string ManifestFileName = "install-manifest.json";
}
