namespace AvaInstaller.Services;

/// <summary>
/// 自删除服务。
/// 卸载完成后，通过生成临时 .cmd 批处理脚本延迟删除卸载器自身及残留文件。
/// 
/// 工作原理：
/// 1. 在系统临时目录生成一个 .cmd 脚本
/// 2. 脚本等待 2 秒（等待卸载器进程退出）
/// 3. 顺序删除卸载器、残留文件、空目录
/// 4. 脚本最后删除自身（%~f0）
/// 
/// 仅在 Windows 平台生效。
/// </summary>
public sealed class SelfDeleteService
{
    /// <summary>
    /// 计划延迟自删除。
    /// 生成并启动一个临时批处理脚本，延迟删除指定文件和目录。
    /// </summary>
    /// <param name="uninstallerPath">卸载器可执行文件路径（必须首先删除）</param>
    /// <param name="installLocation">安装根目录（所有文件删除后尝试清理）</param>
    /// <param name="delayedDeletePaths">需要延迟删除的残留文件路径列表（自动去重）</param>
    public void ScheduleSelfDelete(
        string uninstallerPath, string installLocation, IEnumerable<string> delayedDeletePaths)
    {
        if (!OperatingSystem.IsWindows() || !File.Exists(uninstallerPath))
        {
            return;
        }

        // 生成唯一的临时脚本文件路径
        var cleanupPath = Path.Combine(Path.GetTempPath(), $"avainstaller-cleanup-{Guid.NewGuid():N}.cmd");
        var lines = new List<string>
        {
            "@echo off",                  // 关闭命令回显
            "setlocal",                   // 局部环境变量
            "timeout /t 2 /nobreak >nul", // 等待 2 秒让进程退出
            $"del /f /q \"{uninstallerPath}\" >nul 2>nul" // 删除卸载器
        };

        // 删除残留文件（去重后处理）
        foreach (var path in delayedDeletePaths.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            lines.Add($"del /f /q \"{path}\" >nul 2>nul");
        }

        // 尝试删除安装根目录（非空则跳过）
        lines.Add($"rd \"{installLocation}\" >nul 2>nul");
        lines.Add("endlocal");
        lines.Add("del /f /q \"%~f0\" >nul 2>nul"); // 脚本自删除

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
