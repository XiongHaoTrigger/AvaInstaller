namespace AvaInstaller.Utils;

/// <summary>
/// 安全文件操作工具。
/// 用于封装带目录创建的复制、写入等常见操作。
/// </summary>
public static class SafeFileOperations
{
    /// <summary>
    /// 确保文件所在目录存在。
    /// </summary>
    /// <param name="filePath">目标文件路径。</param>
    public static void EnsureParentDirectory(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}
