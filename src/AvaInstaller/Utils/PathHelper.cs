namespace AvaInstaller.Utils;

/// <summary>
/// 路径处理工具。
/// 集中处理环境变量展开和完整路径规范化。
/// </summary>
public static class PathHelper
{
    /// <summary>
    /// 展开环境变量并转换为完整路径。
    /// </summary>
    /// <param name="path">原始路径。</param>
    /// <returns>规范化后的完整路径。</returns>
    public static string NormalizeFullPath(string path)
    {
        return Path.GetFullPath(Environment.ExpandEnvironmentVariables(path.Trim()));
    }
}
