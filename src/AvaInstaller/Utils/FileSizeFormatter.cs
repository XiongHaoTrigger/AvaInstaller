namespace AvaInstaller.Utils;

/// <summary>
/// 文件大小格式化工具。
/// </summary>
public static class FileSizeFormatter
{
    private static readonly string[] Units = ["B", "KB", "MB", "GB", "TB"];

    /// <summary>
    /// 将字节数格式化为适合界面展示的文本。
    /// </summary>
    /// <param name="bytes">字节数。</param>
    /// <returns>格式化后的大小文本。</returns>
    public static string Format(long bytes)
    {
        var value = (double)Math.Max(0, bytes);
        var unitIndex = 0;

        while (value >= 1024 && unitIndex < Units.Length - 1)
        {
            value /= 1024;
            unitIndex++;
        }

        return $"{value:0.#} {Units[unitIndex]}";
    }
}
