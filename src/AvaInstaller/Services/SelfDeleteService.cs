namespace AvaInstaller.Services;

public sealed class SelfDeleteService
{
    public void ScheduleSelfDelete(string uninstallerPath, string installLocation, IEnumerable<string> delayedDeletePaths)
    {
        if (!OperatingSystem.IsWindows() || !File.Exists(uninstallerPath))
        {
            return;
        }

        var cleanupPath = Path.Combine(Path.GetTempPath(), $"avainstaller-cleanup-{Guid.NewGuid():N}.cmd");
        var lines = new List<string>
        {
            "@echo off",
            "setlocal",
            "timeout /t 2 /nobreak >nul",
            $"del /f /q \"{uninstallerPath}\" >nul 2>nul"
        };

        foreach (var path in delayedDeletePaths.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            lines.Add($"del /f /q \"{path}\" >nul 2>nul");
        }

        lines.Add($"rd \"{installLocation}\" >nul 2>nul");
        lines.Add("endlocal");
        lines.Add("del /f /q \"%~f0\" >nul 2>nul");

        var script = string.Join(Environment.NewLine, lines);

        File.WriteAllText(cleanupPath, script);
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = cleanupPath,
            CreateNoWindow = true,
            UseShellExecute = false,
            WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
        });
    }
}
