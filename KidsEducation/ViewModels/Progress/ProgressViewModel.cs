using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KidsEducation.Models;
using KidsEducation.Services;

namespace KidsEducation.ViewModels.Progress;

public partial class ProgressViewModel : ObservableObject
{
    private readonly ProfileService _profileService;
    private readonly ContentService _contentService;
    private readonly ModuleProgressService _moduleProgressService;

    [ObservableProperty] private ChildProfile? _activeProfile;
    [ObservableProperty] private List<CategoryStat> _categoryStats = new();
    [ObservableProperty] private List<ModuleProgressInfo> _moduleProgress = new();
    [ObservableProperty] private List<GameHistoryItem> _gameHistory = new();
    [ObservableProperty] private bool _hasGameHistory;
    [ObservableProperty] private bool _isLoading = true;

    // Haftalık özet
    [ObservableProperty] private int _weeklyLessons;
    [ObservableProperty] private int _weeklyStars;

    // Grafik bar yükseklikleri (0–60 px arası)
    [ObservableProperty] private double _day1Height = 8;
    [ObservableProperty] private double _day2Height = 8;
    [ObservableProperty] private double _day3Height = 8;
    [ObservableProperty] private double _day4Height = 8;
    [ObservableProperty] private double _day5Height = 8;
    [ObservableProperty] private double _day6Height = 8;
    [ObservableProperty] private double _day7Height = 8;

    // Grafik bar renkleri
    [ObservableProperty] private string _day1Color = "#EDE8FF";
    [ObservableProperty] private string _day2Color = "#EDE8FF";
    [ObservableProperty] private string _day3Color = "#EDE8FF";
    [ObservableProperty] private string _day4Color = "#EDE8FF";
    [ObservableProperty] private string _day5Color = "#EDE8FF";
    [ObservableProperty] private string _day6Color = "#EDE8FF";
    [ObservableProperty] private string _day7Color = "#EDE8FF";

    public ProgressViewModel(
        ProfileService profileService,
        ContentService contentService,
        ModuleProgressService moduleProgressService)
    {
        _profileService = profileService;
        _contentService = contentService;
        _moduleProgressService = moduleProgressService;
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        IsLoading = true;
        try
        {
            ActiveProfile = _profileService.GetActiveProfile();
            if (ActiveProfile is null) return;

            await LoadCategoryStatsAsync();
            await LoadModuleProgressAsync();
            LoadWeeklyActivity();
            LoadGameHistory();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadModuleProgressAsync()
    {
        var categories = await _contentService.GetCategoriesAsync(ActiveProfile!);
        ModuleProgress = _moduleProgressService.BuildModuleProgress(categories, ActiveProfile!);
    }

    private async Task LoadCategoryStatsAsync()
    {
        var categories = await _contentService.GetCategoriesAsync(ActiveProfile!);

        CategoryStats = categories.Select(c =>
        {
            var progress = ActiveProfile!.CategoryProgresses
                .TryGetValue(c.Id, out var p) ? p : null;

            return new CategoryStat
            {
                CategoryId = c.Id,
                NameTr = c.NameTr,
                Emoji = c.Emoji,
                ColorHex = c.BackgroundHex + "44", // %27 opacity
                ProgressPercent = progress?.BestStars > 0
                    ? Math.Min(100, progress.PlayCount * 10)
                    : 0
            };
        }).ToList();
    }

    private void LoadWeeklyActivity()
    {
        // Haftalık veriyi CategoryProgresses.LastPlayedAt'e göre hesapla
        var today = DateTime.Today;
        var weekStart = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);

        var dailyLessons = new double[7];

        foreach (var cp in ActiveProfile!.CategoryProgresses.Values)
        {
            if (cp.LastPlayedAt >= weekStart && cp.LastPlayedAt <= today)
            {
                int dayIndex = (int)(cp.LastPlayedAt.Date - weekStart).TotalDays;
                if (dayIndex >= 0 && dayIndex < 7)
                    dailyLessons[dayIndex] += cp.PlayCount;
            }
        }

        // Normalize et: max 60px yükseklik
        double max = dailyLessons.Max();
        if (max == 0) max = 1;

        double[] heights =
        {
            Math.Max(8, dailyLessons[0] / max * 60),
            Math.Max(8, dailyLessons[1] / max * 60),
            Math.Max(8, dailyLessons[2] / max * 60),
            Math.Max(8, dailyLessons[3] / max * 60),
            Math.Max(8, dailyLessons[4] / max * 60),
            Math.Max(8, dailyLessons[5] / max * 60),
            Math.Max(8, dailyLessons[6] / max * 60),
        };

        Day1Height = heights[0]; Day2Height = heights[1];
        Day3Height = heights[2]; Day4Height = heights[3];
        Day5Height = heights[4]; Day6Height = heights[5];
        Day7Height = heights[6];

        // Bugünün günü aktif renk
        int todayIndex = (int)(today - weekStart).TotalDays;
        var colors = new string[7];
        for (int i = 0; i < 7; i++)
            colors[i] = dailyLessons[i] > 0 ? "#6C62F5" : "#EDE8FF";
        if (todayIndex >= 0 && todayIndex < 7)
            colors[todayIndex] = "#4C44C6";

        Day1Color = colors[0]; Day2Color = colors[1];
        Day3Color = colors[2]; Day4Color = colors[3];
        Day5Color = colors[4]; Day6Color = colors[5];
        Day7Color = colors[6];

        WeeklyLessons = (int)dailyLessons.Sum();
        WeeklyStars = ActiveProfile.CategoryProgresses.Values
            .Where(cp => cp.LastPlayedAt >= weekStart)
            .Sum(cp => cp.BestStars);
    }

    private void LoadGameHistory()
    {
        GameHistory = ActiveProfile!.CategoryProgresses.Values
            .Where(cp => cp.PlayCount > 0 && cp.LastPlayedAt > DateTime.MinValue)
            .OrderByDescending(cp => cp.LastPlayedAt)
            .Take(20)
            .Select(cp =>
            {
                var cat = CategoryStats.FirstOrDefault(c => c.CategoryId == cp.CategoryId);
                return new GameHistoryItem
                {
                    CategoryId = cp.CategoryId,
                    CategoryName = cat?.NameTr ?? cp.CategoryId,
                    Emoji = cat?.Emoji ?? "🎮",
                    PlayCount = cp.PlayCount,
                    BestStars = cp.BestStars,
                    LastPlayedAt = cp.LastPlayedAt,
                    DateText = FormatDate(cp.LastPlayedAt)
                };
            })
            .ToList();

        HasGameHistory = GameHistory.Count > 0;
    }

    private static string FormatDate(DateTime dt)
    {
        var local = dt.ToLocalTime();
        var today = DateTime.Today;
        if (local.Date == today) return "Bugün";
        if (local.Date == today.AddDays(-1)) return "Dün";
        return local.ToString("d MMM");
    }
}

public class GameHistoryItem
{
    public string CategoryId { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string Emoji { get; set; } = "🎮";
    public int PlayCount { get; set; }
    public int BestStars { get; set; }
    public DateTime LastPlayedAt { get; set; }
    public string DateText { get; set; } = string.Empty;
    public string StarsText => BestStars > 0 ? new string('⭐', Math.Min(BestStars, 3)) : "—";
    public string PlayCountText => $"{PlayCount} oyun";
}
