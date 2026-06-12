using KidsEducation.Enums;

namespace KidsEducation.Models;

public static class ModuleConfig
{
    public static readonly Dictionary<string, AgeGroup[]> CategoryAgeGroups = new()
    {
        { "animals",    new[] { AgeGroup.Toddler, AgeGroup.Explorer, AgeGroup.Adventurer } },
        { "fruits",     new[] { AgeGroup.Toddler, AgeGroup.Explorer, AgeGroup.Adventurer } },
        { "vegetables", new[] { AgeGroup.Toddler, AgeGroup.Explorer, AgeGroup.Adventurer } },
        { "colors",     new[] { AgeGroup.Toddler, AgeGroup.Explorer, AgeGroup.Adventurer } },
        { "shapes",     new[] { AgeGroup.Toddler, AgeGroup.Explorer, AgeGroup.Adventurer } },
        { "vehicles",   new[] { AgeGroup.Explorer, AgeGroup.Adventurer } },
        { "numbers",    new[] { AgeGroup.Explorer, AgeGroup.Adventurer } },
        { "body",       new[] { AgeGroup.Explorer, AgeGroup.Adventurer } },
        { "letters",    new[] { AgeGroup.Adventurer } },
        { "english",    new[] { AgeGroup.Adventurer } },
        { "seasons",    new[] { AgeGroup.Adventurer } },
    };

    public static readonly Dictionary<GameType, AgeGroup[]> GameTypeAgeGroups = new()
    {
        { GameType.MatchName,   new[] { AgeGroup.Toddler, AgeGroup.Explorer, AgeGroup.Adventurer } },
        { GameType.ShadowGuess, new[] { AgeGroup.Explorer, AgeGroup.Adventurer } },
        { GameType.ZoomGuess,   new[] { AgeGroup.Adventurer } },
        { GameType.SoundGuess,  new[] { AgeGroup.Toddler, AgeGroup.Explorer, AgeGroup.Adventurer } },
        { GameType.BalloonPop,  new[] { AgeGroup.Toddler, AgeGroup.Explorer, AgeGroup.Adventurer } },
    };

    public static bool IsCategoryAvailable(string categoryId, AgeGroup ageGroup)
    {
        if (!CategoryAgeGroups.TryGetValue(categoryId, out var groups))
            return false;
        return groups.Contains(ageGroup);
    }

    public static bool IsGameTypeAvailable(GameType gameType, AgeGroup ageGroup)
    {
        if (!GameTypeAgeGroups.TryGetValue(gameType, out var groups))
            return false;
        return groups.Contains(ageGroup);
    }

    public static List<GameType> GetAvailableGameTypes(AgeGroup ageGroup) =>
        GameTypeAgeGroups
            .Where(kv => kv.Value.Contains(ageGroup))
            .Select(kv => kv.Key)
            .ToList();
}
