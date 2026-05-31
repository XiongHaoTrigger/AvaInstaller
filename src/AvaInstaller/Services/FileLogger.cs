namespace AvaInstaller.Services;

/// <summary>
/// 简单文件日志服务。
/// </summary>
public sealed class FileLogger : IFileLogger
{
    private readonly string _path;

    /// <summary>
    /// 创建文件日志服务。
    /// </summary>
    /// <param name="path">日志文件路径。</param>
    public FileLogger(string path)
    {
        _path = path;
    }

    /// <inheritdoc />
    public void WriteLine(string message)
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.AppendAllText(_path, $"{DateTimeOffset.Now:O} {message}{Environment.NewLine}");
    }
}
