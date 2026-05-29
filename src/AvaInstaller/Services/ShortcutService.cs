using System.Diagnostics;
using AvaInstaller.Models;

namespace AvaInstaller.Services;

/// <summary>
/// 快捷方式服务。
/// 负责在开始菜单创建/删除应用程序和卸载程序的快捷方式（.lnk 文件）。
/// 
/// 实现方式：通过 PowerShell 调用 WScript.Shell COM 对象创建 .lnk 文件，
/// 因为 .NET 不提供原生的快捷方式创建 API。
/// 仅在 Windows 平台生效。
/// </summary>
public sealed class ShortcutService
{
    /// <summary>
    /// 在开始菜单中创建快捷方式。
    /// 创建以下快捷方式：
    /// 1. 主程序快捷方式 → "%StartMenu%/Programs/{AppName}/{AppName}.lnk"
    /// 2. 卸载程序快捷方式 → "%StartMenu%/Programs/{AppName}/卸载 {AppName}.lnk"
    /// </summary>
    /// <param name="installLocation">安装目录</param>
    /// <returns>创建的快捷方式完整路径列表</returns>
    public IReadOnlyList<string> CreateShortcuts(string installLocation)
    {
        if (!OperatingSystem.IsWindows())
        {
            return [];
        }

        var shortcuts = new List<string>();
        var startMenuDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
            "Programs",
            InstallerMetadata.AppName);
        Directory.CreateDirectory(startMenuDirectory);

        // 创建主程序快捷方式
        var mainExePath = Path.Combine(installLocation, InstallerMetadata.MainExe);
        if (File.Exists(mainExePath))
        {
            var appShortcut = Path.Combine(startMenuDirectory, $"{InstallerMetadata.AppName}.lnk");
            CreateShortcut(appShortcut, mainExePath, string.Empty, installLocation);
            shortcuts.Add(appShortcut);
        }

        // 创建卸载程序快捷方式
        var uninstallerPath = Path.Combine(installLocation, InstallerMetadata.Uninstaller);
        var uninstallShortcut = Path.Combine(startMenuDirectory, $"卸载 {InstallerMetadata.AppName}.lnk");
        CreateShortcut(uninstallShortcut, uninstallerPath, "--uninstall", installLocation);
        shortcuts.Add(uninstallShortcut);

        return shortcuts;
    }

    /// <summary>
    /// 删除指定快捷方式文件。
    /// 如果快捷方式所在目录为空，一并清理目录。
    /// </summary>
    /// <param name="shortcutPath">快捷方式路径（支持 %StartMenu% 等占位符）</param>
    public void DeleteShortcut(string shortcutPath)
    {
        var expandedPath = InstallPathService.ExpandKnownPath(shortcutPath);
        if (File.Exists(expandedPath))
        {
            File.Delete(expandedPath);
        }

        // 如果父目录为空则一并删除
        var directory = Path.GetDirectoryName(expandedPath);
        if (!string.IsNullOrWhiteSpace(directory) &&
            Directory.Exists(directory) &&
            !Directory.EnumerateFileSystemEntries(directory).Any())
        {
            Directory.Delete(directory);
        }
    }

    /// <summary>
    /// 通过 PowerShell 调用 WScript.Shell COM 对象创建快捷方式。
    /// </summary>
    /// <param name="shortcutPath">快捷方式文件完整路径（.lnk）</param>
    /// <param name="targetPath">目标程序完整路径</param>
    /// <param name="arguments">启动参数</param>
    /// <param name="workingDirectory">工作目录</param>
    /// <exception cref="InvalidOperationException">
    /// 无法启动 PowerShell 或快捷方式创建失败时抛出
    /// </exception>
    private static void CreateShortcut(string shortcutPath, string targetPath, string arguments, string workingDirectory)
    {
        var command = string.Join("; ", new[]
        {
            "$w = New-Object -ComObject WScript.Shell",
            $"$s = $w.CreateShortcut('{EscapePowerShellSingleQuoted(shortcutPath)}')",
            $"$s.TargetPath = '{EscapePowerShellSingleQuoted(targetPath)}'",
            $"$s.Arguments = '{EscapePowerShellSingleQuoted(arguments)}'",
            $"$s.WorkingDirectory = '{EscapePowerShellSingleQuoted(workingDirectory)}'",
            "$s.Save()"
        });

        var process = Process.Start(new ProcessStartInfo
        {
            FileName = "powershell.exe",
            ArgumentList =
            {
                "-NoProfile",
                "-ExecutionPolicy",
                "Bypass",
                "-Command",
                command
            },
            CreateNoWindow = true,
            UseShellExecute = false,
            WindowStyle = ProcessWindowStyle.Hidden
        });

        if (process is null)
        {
            throw new InvalidOperationException("Unable to start powershell.exe for shortcut creation.");
        }

        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Shortcut creation failed with exit code {process.ExitCode}: {shortcutPath}");
        }
    }

    /// <summary>
    /// 转义 PowerShell 单引号字符串中的单引号字符。
    /// PowerShell 中单引号字符串内用两个单引号表示一个字面单引号。
    /// </summary>
    /// <param name="value">原始字符串</param>
    /// <returns>转义后的字符串</returns>
    private static string EscapePowerShellSingleQuoted(string value)
    {
        return value.Replace("'", "''", StringComparison.Ordinal);
    }
}
