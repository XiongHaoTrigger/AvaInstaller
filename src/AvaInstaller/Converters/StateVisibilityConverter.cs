using System.Globalization;
using Avalonia.Data.Converters;
using AvaInstaller.Models;

namespace AvaInstaller.Converters;

/// <summary>
/// 步骤可见性转换器。
/// 根据当前安装步骤与 XAML 中传入的步骤名称，返回控件是否可见。
/// </summary>
public sealed class StateVisibilityConverter : IValueConverter
{
    /// <summary>
    /// 将当前步骤转换为布尔可见性。
    /// ConverterParameter 支持 "!StepName" 形式，用于取反判断。
    /// </summary>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not InstallerStep currentPage || parameter is not string pageName)
        {
            return false;
        }

        var negate = pageName.StartsWith('!');
        var normalizedPageName = negate ? pageName[1..] : pageName;
        var isMatch = Enum.TryParse<InstallerStep>(normalizedPageName, ignoreCase: true, out var expectedPage) &&
                      currentPage == expectedPage;

        return negate ? !isMatch : isMatch;
    }

    /// <summary>
    /// 可见性转换不支持反向写入。
    /// </summary>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
