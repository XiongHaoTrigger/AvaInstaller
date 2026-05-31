namespace AvaInstaller.Utils;

/// <summary>
/// 参数校验工具。
/// 用于服务层入口快速表达前置条件，减少重复的空值判断代码。
/// </summary>
public static class Guard
{
    /// <summary>
    /// 校验字符串不能为空。
    /// </summary>
    /// <param name="value">待校验字符串。</param>
    /// <param name="parameterName">参数名称。</param>
    /// <returns>原始字符串。</returns>
    /// <exception cref="ArgumentException">字符串为空时抛出。</exception>
    public static string NotNullOrWhiteSpace(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("参数不能为空。", parameterName);
        }

        return value;
    }
}
