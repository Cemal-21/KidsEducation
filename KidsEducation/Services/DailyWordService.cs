using KidsEducation.Models;

namespace KidsEducation.Services;

public class DailyWordService
{
    private readonly ContentService _contentService;
    private readonly AppPreferencesService _prefs;
    private const string DateKey = "daily_word_date";
    private const string ItemIdKey = "daily_word_item_id";
    private const string CategoryKey = "daily_word_category";

    public DailyWordService(ContentService contentService, AppPreferencesService prefs)
    {
        _contentService = contentService;
        _prefs = prefs;
    }

    public async Task<DailyWordInfo?> GetTodayWordAsync(ChildProfile profile)
    {
        var today = DateTime.Today.ToString("yyyy-MM-dd");
        var savedDate = Preferences.Default.Get(DateKey, "");
        var savedItemId = Preferences.Default.Get(ItemIdKey, "");
        var savedCategory = Preferences.Default.Get(CategoryKey, "");

        // Aynı gün ise kayıtlı kelimeyi döndür
        if (savedDate == today && !string.IsNullOrEmpty(savedItemId) && !string.IsNullOrEmpty(savedCategory))
        {
            var items = await _contentService.GetItemsAsync(savedCategory);
            var saved = items.FirstOrDefault(i => i.Id == savedItemId);
            if (saved is not null)
            {
                var cats = await _contentService.GetCategoriesAsync(profile);
                var cat = cats.FirstOrDefault(c => c.Id == savedCategory);
                var cachedInfo = ToInfo(saved, cat);
                SaveForWidget(cachedInfo);
                return cachedInfo;
            }
        }

        // Yeni gün — rastgele kelime seç (tarihe göre deterministik)
        var allItems = await _contentService.GetMixedGameItemsAsync(profile, 50);
        if (allItems.Count == 0) return null;

        var seed = today.GetHashCode();
        var index = Math.Abs(seed) % allItems.Count;
        var item = allItems[index];

        Preferences.Default.Set(DateKey, today);
        Preferences.Default.Set(ItemIdKey, item.Id);
        Preferences.Default.Set(CategoryKey, item.Category);

        var categories = await _contentService.GetCategoriesAsync(profile);
        var category = categories.FirstOrDefault(c => c.Id == item.Category);
        var info = ToInfo(item, category);
        SaveForWidget(info);
        return info;
    }

    private static void SaveForWidget(DailyWordInfo info)
    {
        Preferences.Default.Set("daily_word_name_tr", info.NameTr);
        Preferences.Default.Set("daily_word_name_en", info.NameEn);
        Preferences.Default.Set("daily_word_emoji", info.Emoji);
        Preferences.Default.Set("daily_word_category_name", info.CategoryNameTr);
        Preferences.Default.Set("daily_word_sentence", info.ExampleSentence);

#if ANDROID
        var context = global::Android.App.Application.Context;
        KidsEducation.Platforms.Android.DailyWordWidget.NotifyUpdate(context);
#endif
    }

    private static DailyWordInfo ToInfo(LearningItem item, Category? category) => new()
    {
        NameTr = item.NameTr,
        NameEn = item.NameEn,
        Emoji = category?.Emoji ?? "📖",
        ImagePath = item.ImagePath,
        CategoryNameTr = category?.NameTr ?? "",
        ExampleSentence = BuildSentence(item, category),
        SpeakText = item.NameTr
    };

    private static string BuildSentence(LearningItem item, Category? category)
    {
        if (!string.IsNullOrWhiteSpace(item.DescriptionTr))
            return item.DescriptionTr;

        return category?.Id switch
        {
            "animals"     => $"{item.NameTr} bir hayvandır.",
            "fruits"      => $"{item.NameTr} lezzetli bir meyvedir.",
            "vegetables"  => $"{item.NameTr} sağlıklı bir sebzedir.",
            "colors"      => $"{item.NameTr} güzel bir renktir.",
            "shapes"      => $"{item.NameTr} bir şekildir.",
            "vehicles"    => $"{item.NameTr} bir araçtır.",
            "numbers"     => $"{item.NameTr} bir sayıdır.",
            "letters"     => $"{item.NameTr} alfabedeki bir harftir.",
            "body"        => $"{item.NameTr} vücudumuzun bir parçasıdır.",
            "emotions"    => $"{item.NameTr} bir duygudur.",
            "professions" => $"{item.NameTr} bir meslektir.",
            "seasons"     => $"{item.NameTr} bir mevsimdir.",
            "weather"     => $"Bugün hava {item.NameTr}.",
            "planets"     => $"{item.NameTr} güneş sisteminde bir gezegendir.",
            _             => $"Bu bir {item.NameTr}."
        };
    }
}

public class DailyWordInfo
{
    public string NameTr { get; set; } = "";
    public string NameEn { get; set; } = "";
    public string Emoji { get; set; } = "📖";
    public string ImagePath { get; set; } = "";
    public string CategoryNameTr { get; set; } = "";
    public string ExampleSentence { get; set; } = "";
    public string SpeakText { get; set; } = "";
}
