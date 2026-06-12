using System.Globalization;

namespace KidsEducation.Converters;

/// <summary>
/// bool → Opacity: kazanıldıysa 1.0, kilitliyse 0.35
/// </summary>
public class BoolToOpacityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? 1.0 : 0.35;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// bool (IsEarned) → footer arka plan rengi
/// kazanıldı = açık yeşil, kilitli = açık gri
/// </summary>
public class BadgeFooterColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true
            ? Color.FromArgb("#E8FFF3")   // açık yeşil
            : Color.FromArgb("#F6F6F8");  // açık gri

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// EarnedCount (int) → progress bar WidthRequest
/// Max genişlik ~220px, TotalCount 11 rozet
/// </summary>
public class BadgeProgressConverter : IValueConverter
{
    private const double MaxWidth = 220;
    private const int TotalBadges = 11;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int earned)
            return Math.Min(MaxWidth, MaxWidth * earned / TotalBadges);
        return 0.0;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
