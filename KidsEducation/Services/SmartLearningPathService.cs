using KidsEducation.Models;

namespace KidsEducation.Services;

public class SmartLearningPathService
{
    private readonly ContentService _contentService;
    private readonly LearningEventService _learningEventService;

    public SmartLearningPathService(
        ContentService contentService,
        LearningEventService learningEventService)
    {
        _contentService = contentService;
        _learningEventService = learningEventService;
    }

    public async Task<SmartLearningSuggestion> GetSuggestionAsync(ChildProfile profile)
    {
        var categories = await _contentService.GetCategoriesAsync(profile);
        var weeklyAccuracy = await _learningEventService.GetWeeklyAccuracyByCategoryAsync(profile.Id);

        var weakCategory = weeklyAccuracy
            .Where(kv => kv.Value < 0.75)
            .OrderBy(kv => kv.Value)
            .Select(kv => categories.FirstOrDefault(c => c.Id == kv.Key))
            .FirstOrDefault(c => c is not null);

        if (weakCategory is not null)
        {
            return new SmartLearningSuggestion
            {
                Title = "akilli ogrenme yolu",
                Headline = $"{weakCategory.NameTr} tekrar zamani",
                Subtitle = "Son cevaplara gore burada kisa bir pratik iyi gelir.",
                Emoji = weakCategory.Emoji,
                CategoryId = weakCategory.Id,
                Route = "soundgame",
                ActionText = "Dinle ve bul",
                ReasonText = "Haftalik dogruluk oranina gore secildi"
            };
        }

        var nextCategory = categories
            .OrderBy(c => profile.CategoryProgresses.TryGetValue(c.Id, out var p) ? p.PlayCount : 0)
            .ThenBy(c => c.NameTr)
            .FirstOrDefault();

        if (nextCategory is not null)
        {
            return new SmartLearningSuggestion
            {
                Title = "akilli ogrenme yolu",
                Headline = $"{nextCategory.NameTr} ile devam",
                Subtitle = "Bugun kisa ve odakli bir oyunla ilerleme kazan.",
                Emoji = nextCategory.Emoji,
                CategoryId = nextCategory.Id,
                Route = "zoomgame",
                ActionText = "Onerilen oyuna gec",
                ReasonText = "Kaldigin ilerlemeye gore secildi"
            };
        }

        return new SmartLearningSuggestion
        {
            Title = "akilli ogrenme yolu",
            Headline = "Kisa bir hafiza oyunu",
            Subtitle = "Isinma icin karisik kartlarla basla.",
            Emoji = "🧠",
            Route = "memorygame",
            ActionText = "Basla",
            ReasonText = "Yeni profil icin baslangic onerisi"
        };
    }
}
