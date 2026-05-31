namespace AvaInstaller.Services;

/// <summary>
/// 文件日志服务接口。
/// 用于安装或卸载流程记录可诊断信息。
/// </summary>
public interface IFileLogger
{
    /// <summary>
    /// 写入一行日志。
    /// </summary>
    /// <param name="message">日志内容。</param>
    void WriteLine(string message);
}
