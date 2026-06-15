using System.Globalization;

namespace KidsEducation.Converters;

public class EmojiIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value?.ToString() switch
        {
            "🐰" => "animal_rabbit.png",
            "🦊" => "animal_fox.png",
            "🦉" => "animal_owl.png",
            "🐻" => "animal_bear.png",
            "🐸" => "animal_frog.png",
            "🦁" => "animal_lion.png",
            "🦋" => "animal_butterfly.png",
            "🐳" => "animal_dolphin.png",
            "🐾" => "category_animals.png",
            "🍎" => "category_fruits.png",
            "🎨" => "category_colors.png",
            "🔢" => "category_numbers.png",
            "📚" => "ui_learning_3d.png",
            "🎮" => "ui_games_3d.png",
            "🎯" => "ui_goal_3d.png",
            "📖" => "ui_tales_3d.png",
            "⭐" => "ui_star_3d.png",
            "🔥" => "ui_streak_3d.png",
            "🏆" => "ui_leaderboard_3d.png",
            "🎤" => "ui_mic_3d.png",
            _ => "ui_learning_3d.png"
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
