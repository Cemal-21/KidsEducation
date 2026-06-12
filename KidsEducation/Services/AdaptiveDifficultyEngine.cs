using KidsEducation.Models;

namespace KidsEducation.Services;

public class AdaptiveDifficultyEngine
{
    private readonly LearningEventService _eventService;

    public AdaptiveDifficultyEngine(LearningEventService eventService)
    {
        _eventService = eventService;
    }

    public async Task<int> GetNextDifficultyAsync(string profileId, string categoryId, int baseDifficulty)
    {
        var recent = await _eventService.GetRecentEventsAsync(profileId, categoryId, take: 3);
        if (recent.Count < 3) return baseDifficulty;

        if (recent.All(e => e.IsCorrect))
            return Math.Min(baseDifficulty + 1, 3);
        if (recent.All(e => !e.IsCorrect))
            return Math.Max(baseDifficulty - 1, 1);

        return baseDifficulty;
    }
}