using System.Diagnostics;
using AvaInstaller.Models;

namespace AvaInstaller.Services;

public sealed class ShortcutService
{
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

        var mainExePath = Path.Combine(installLocation, InstallerMetadata.MainExe);
        if (File.Exists(mainExePath))
        {
            var appShortcut = Path.Combine(startMenuDirectory, $"{InstallerMetadata.AppName}.lnk");
            CreateShortcut(appShortcut, mainExePath, string.Empty, installLocation);
            shortcuts.Add(appShortcut);
        }

        var uninstallerPath = Path.Combine(installLocation, InstallerMetadata.Uninstaller);
        var uninstallShortcut = Path.Combine(startMenuDirectory, $"卸载 {InstallerMetadata.AppName}.lnk");
        CreateShortcut(uninstallShortcut, uninstallerPath, "--uninstall", installLocation);
        shortcuts.Add(uninstallShortcut);

        return shortcuts;
    }

    public void DeleteShortcut(string shortcutPath)
    {
        var expandedPath = InstallPathService.ExpandKnownPath(shortcutPath);
        if (File.Exists(expandedPath))
        {
            File.Delete(expandedPath);
        }

        var directory = Path.GetDirectoryName(expandedPath);
        if (!string.IsNullOrWhiteSpace(directory) &&
            Directory.Exists(directory) &&
            !Directory.EnumerateFileSystemEntries(directory).Any())
        {
            Directory.Delete(directory);
        }
    }

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
            throw new InvalidOperationException($"Shortcut creation failed with exit code {process.ExitCode}: {shortcutPath}");
        }
    }

    private static string EscapePowerShellSingleQuoted(string value)
    {
        return value.Replace("'", "''", StringComparison.Ordinal);
    }
}
