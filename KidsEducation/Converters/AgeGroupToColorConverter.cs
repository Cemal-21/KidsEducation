using KidsEducation.Enums;
using System.Globalization;

namespace KidsEducation.Converters;

public class AgeGroupToColorConverter : IValueConverter
{
    public AgeGroup TargetGroup { get; set; }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is AgeGroup selected && selected == TargetGroup)
            return Color.FromArgb("#EEF0FF");
        return Color.FromArgb("#F5F5F5");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class AgeGroupToStrokeConverter : IValueConverter
{
    public AgeGroup TargetGroup { get; set; }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is AgeGroup selected && selected == TargetGroup)
            return Color.FromArgb("#6C63FF");
        return Colors.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}