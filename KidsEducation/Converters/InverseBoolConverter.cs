using System.Globalization;

namespace KidsEducation.Converters;

/// <summary>
/// bool tersi: true → false, false → true.
/// IsVisible (kapalı kart, IsFlipped tersi) ve IsEnabled (IsBusy tersi) için kullanılır.
/// </summary>
public class InverseBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : value!;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : value!;
}