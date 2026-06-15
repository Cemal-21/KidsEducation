using KidsEducation.Enums;
using KidsEducation.Models;

namespace KidsEducation.Services;

public class NavigationService
{
    private readonly AppPreferencesService _prefs;

    public NavigationService(AppPreferencesService prefs)
    {
        _prefs = prefs;
    }

    public Task GoToHomeAsync() =>
        Shell.Current.GoToAsync("//home");

    public Task GoToProfileSelectionAsync() =>
        Shell.Current.GoToAsync("//profileselection");

    public Task GoToCategoryAsync(string categoryId) =>
        Shell.Current.GoToAsync($"category?categoryId={Uri.EscapeDataString(categoryId)}");

    public Task GoToLearningItemAsync(string categoryId, string itemId) =>
        Shell.Current.GoToAsync(
            $"itemdetail?categoryId={Uri.EscapeDataString(categoryId)}&itemId={Uri.EscapeDataString(itemId)}");

    public Task GoToGameAsync(string categoryId, GameType gameType) =>
        Shell.Current.GoToAsync(
            $"game?categoryId={Uri.EscapeDataString(categoryId)}&gameType={gameType}");

    public Task GoToResultAsync(GameSession session)
    {
        SessionStore.Current = session;
        return Shell.Current.GoToAsync("result");
    }

    public Task GoToGamesAsync() =>
        Shell.Current.GoToAsync("games");

    public Task GoToSongsAsync() =>
        Shell.Current.GoToAsync("songs");

    public Task GoToCurriculumActivitiesAsync() =>
        Shell.Current.GoToAsync("curriculumactivities");

    public Task GoToAdventureMapAsync() =>
        Shell.Current.GoToAsync("adventuremap");

    public Task GoToSmartSuggestionAsync(SmartLearningSuggestion suggestion)
    {
        if (string.IsNullOrWhiteSpace(suggestion.Route))
            return GoToHomeAsync();

        var route = suggestion.Route;
        if (!string.IsNullOrWhiteSpace(suggestion.CategoryId) &&
            (route == "zoomgame" || route == "soundgame" || route == "game"))
        {
            route += $"?categoryId={Uri.EscapeDataString(suggestion.CategoryId)}";
        }

        return Shell.Current.GoToAsync(route);
    }

    public Task GoToSongDetailAsync(string songId) =>
        Shell.Current.GoToAsync($"songdetail?songId={Uri.EscapeDataString(songId)}");

    public Task GoToLessonAsync(string lessonId) =>
        Shell.Current.GoToAsync($"lesson?lessonId={Uri.EscapeDataString(lessonId)}");

    public Task GoToProgressAsync() =>
        Shell.Current.GoToAsync("//progress");

    public Task GoToAchievementsAsync() =>
        Shell.Current.GoToAsync("//achievements");

    public Task GoToPreferencesAsync() =>
        Shell.Current.GoToAsync("preferences");

    public Task GoToParentalAsync() =>
        _prefs.HasPin
            ? Shell.Current.GoToAsync("pinentry")
            : Shell.Current.GoToAsync("//parental");

    public Task GoBackAsync() =>
        Shell.Current.GoToAsync("..");

    public Task GoToTracingGameAsync(string? categoryId = null)
    {
        var route = string.IsNullOrWhiteSpace(categoryId)
            ? "tracinggame"
            : $"tracinggame?categoryId={Uri.EscapeDataString(categoryId)}";
        return Shell.Current.GoToAsync(route);
    }

    public Task GoToPuzzleGameAsync(string? categoryId = null)
    {
        var route = string.IsNullOrWhiteSpace(categoryId)
            ? "puzzlegame"
            : $"puzzlegame?categoryId={Uri.EscapeDataString(categoryId)}";
        return Shell.Current.GoToAsync(route);
    }

    public Task GoBackOneAsync() =>
        Shell.Current.GoToAsync("..");
}

// Sayfalar arası büyük obje taşımak için
public static class SessionStore
{
    public static GameSession? Current { get; set; }
}
