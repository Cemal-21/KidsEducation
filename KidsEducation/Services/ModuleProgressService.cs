using KidsEducation.Models;

namespace KidsEducation.Services;

public class ModuleProgressService
{
    private static readonly Dictionary<string, (string Id, string Name, string Emoji)> CategoryModules = new(StringComparer.OrdinalIgnoreCase)
    {
        ["letters"] = ("language", "Okuma ve Dil", "🔤"),
        ["english"] = ("language", "Okuma ve Dil", "🔤"),

        ["numbers"] = ("concepts", "Sayı ve Kavram", "🔢"),
        ["shapes"] = ("concepts", "Sayı ve Kavram", "🔢"),
        ["opposites"] = ("concepts", "Sayı ve Kavram", "🔢"),

        ["animals"] = ("life", "Yaşam Bilgisi", "🌱"),
        ["fruits"] = ("life", "Yaşam Bilgisi", "🌱"),
        ["vegetables"] = ("life", "Yaşam Bilgisi", "🌱"),
        ["body"] = ("life", "Yaşam Bilgisi", "🌱"),
        ["emotions"] = ("life", "Yaşam Bilgisi", "🌱"),

        ["planets"] = ("world", "Dünya ve Doğa", "🪐"),
        ["countries"] = ("world", "Dünya ve Doğa", "🪐"),
        ["cities"] = ("world", "Dünya ve Doğa", "🪐"),
        ["nature"] = ("world", "Dünya ve Doğa", "🪐"),
        ["weather"] = ("world", "Dünya ve Doğa", "🪐"),
        ["seasons"] = ("world", "Dünya ve Doğa", "🪐"),

        ["traffic"] = ("safety", "Güvenlik", "🚦"),

        ["objects"] = ("daily", "Günlük Yaşam", "🎒"),
        ["vehicles"] = ("daily", "Günlük Yaşam", "🎒"),
        ["professions"] = ("daily", "Günlük Yaşam", "🎒"),
    };

    public static (string Id, string Name, string Emoji) GetModuleForCategory(string categoryId) =>
        CategoryModules.TryGetValue(categoryId, out var module)
            ? module
            : ("discovery", "Keşif Modülü", "🧭");

    public List<ModuleProgressInfo> BuildModuleProgress(IEnumerable<Category> categories, ChildProfile profile)
    {
        return categories
            .GroupBy(c => GetModuleForCategory(c.Id))
            .Select(group =>
            {
                var categoryIds = group.Select(c => c.Id).ToList();
                var completed = categoryIds.Count(id =>
                    profile.CategoryProgresses.TryGetValue(id, out var p) && p.BestStars >= 3);
                var played = categoryIds.Count(id =>
                    profile.CategoryProgresses.TryGetValue(id, out var p) && p.PlayCount > 0);

                return new ModuleProgressInfo
                {
                    Id = group.Key.Id,
                    Name = group.Key.Name,
                    Emoji = group.Key.Emoji,
                    TotalCategories = categoryIds.Count,
                    CompletedCategories = completed,
                    PlayedCategories = played
                };
            })
            .OrderByDescending(m => m.IsCompleted)
            .ThenByDescending(m => m.ProgressRatio)
            .ThenBy(m => m.Name)
            .ToList();
    }
}
