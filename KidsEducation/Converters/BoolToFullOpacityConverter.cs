using System.Globalization;

namespace KidsEducation.Converters;

/// <summary>
/// Aktif sekme → 1.0 opacity, pasif sekme → 0.4 opacity
/// </summary>
public class BoolToFullOpacityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? 1.0 : 0.4;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
