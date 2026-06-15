using System.Globalization;

namespace KidsEducation.Converters;

public class BadgeIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value?.ToString() switch
        {
            "lesson_5" => "badge_lesson_5.png",
            "lesson_10" => "badge_lesson_10.png",
            "lesson_25" => "badge_lesson_25.png",
            "lesson_50" => "badge_lesson_50.png",
            "streak_3" => "badge_streak_3.png",
            "streak_7" => "badge_streak_7.png",
            "streak_30" => "badge_streak_30.png",
            "stars_50" => "badge_stars_50.png",
            "stars_200" => "badge_stars_200.png",
            "xp_500" => "badge_xp_500.png",
            "xp_2000" => "badge_xp_2000.png",
            _ => "ui_star_3d.png"
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
