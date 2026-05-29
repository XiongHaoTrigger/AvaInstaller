using AvaInstaller.Models;

namespace AvaInstaller.Services;

/// <summary>
/// 卸载日志服务。
/// 记录卸载过程的每一步操作到日志文件，
/// 日志文件路径通过 <see cref="LogPath"/> 属性获取。
/// 
/// 日志格式：
/// [ISO8601时间戳] INFO/ERROR 消息内容
/// </summary>
public sealed class UninstallLogService
{
    private readonly string _logPath;

    /// <summary>
    /// 创建卸载日志服务实例。
    /// 自动在日志目录创建以时间戳命名的日志文件。
    /// 路径：%LocalAppData%\AvaInstaller\Logs\{AppId}\uninstall-yyyyMMdd-HHmmss.log
    /// </summary>
    /// <param name="appId">应用程序标识符（默认使用 InstallerMetadata.AppId）</param>
    public UninstallLogService(string appId = InstallerMetadata.AppId)
    {
        var logDirectory = InstallPathService.GetLogsDirectory(appId);
        Directory.CreateDirectory(logDirectory);
        _logPath = Path.Combine(logDirectory, $"uninstall-{DateTimeOffset.Now:yyyyMMdd-HHmmss}.log");
    }

    /// <summary>日志文件的完整路径</summary>
    public string LogPath => _logPath;

    /// <summary>记录一般信息日志</summary>
    /// <param name="message">日志消息</param>
    public void Info(string message)
    {
        File.AppendAllText(_logPath, $"[{DateTimeOffset.Now:O}] INFO  {message}{Environment.NewLine}");
    }

    /// <summary>记录错误日志，可选附带异常信息</summary>
    /// <param name="message">错误消息</param>
    /// <param name="exception">关联的异常对象（可选）</param>
    public void Error(string message, Exception? exception = null)
    {
        File.AppendAllText(_logPath, $"[{DateTimeOffset.Now:O}] ERROR {message}{Environment.NewLine}");
        if (exception is not null)
        {
            File.AppendAllText(_logPath, exception + Environment.NewLine);
        }
    }
}
