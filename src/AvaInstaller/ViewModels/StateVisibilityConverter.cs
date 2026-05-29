using System.Globalization;
using Avalonia.Data.Converters;
using AvaInstaller.Models;

namespace AvaInstaller.ViewModels;

/// <summary>
/// Converts the current page to an Avalonia visibility boolean.
/// </summary>
public sealed class StateVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not InstallPage currentPage || parameter is not string pageName)
        {
            return false;
        }

        var negate = pageName.StartsWith('!');
        var normalizedPageName = negate ? pageName[1..] : pageName;
        var isMatch = Enum.TryParse<InstallPage>(normalizedPageName, ignoreCase: true, out var expectedPage) &&
                      currentPage == expectedPage;

        return negate ? !isMatch : isMatch;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
