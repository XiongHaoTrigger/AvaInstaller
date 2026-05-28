using AvaInstaller.Models;

namespace AvaInstaller.Services;

public sealed class UninstallLogService
{
    private readonly string _logPath;

    public UninstallLogService(string appId = InstallerMetadata.AppId)
    {
        var logDirectory = InstallPathService.GetLogsDirectory(appId);
        Directory.CreateDirectory(logDirectory);
        _logPath = Path.Combine(logDirectory, $"uninstall-{DateTimeOffset.Now:yyyyMMdd-HHmmss}.log");
    }

    public string LogPath => _logPath;

    public void Info(string message)
    {
        File.AppendAllText(_logPath, $"[{DateTimeOffset.Now:O}] INFO  {message}{Environment.NewLine}");
    }

    public void Error(string message, Exception? exception = null)
    {
        File.AppendAllText(_logPath, $"[{DateTimeOffset.Now:O}] ERROR {message}{Environment.NewLine}");
        if (exception is not null)
        {
            File.AppendAllText(_logPath, exception + Environment.NewLine);
        }
    }
}
