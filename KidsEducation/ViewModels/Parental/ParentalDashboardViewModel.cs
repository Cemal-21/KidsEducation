using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KidsEducation.Models;
using KidsEducation.Services;

namespace KidsEducation.ViewModels.Parental;

public partial class ParentalDashboardViewModel : ObservableObject
{
    private readonly ProfileService _profileService;
    private readonly ContentService _contentService;

    [ObservableProperty] private ChildProfile? _activeProfile;
    [ObservableProperty] private List<WeekDayBar> _weekBars = new();
    [ObservableProperty] private List<CategoryStat> _topCategories = new();
    [ObservableProperty] private List<WeeklyBadge> _recentBadges = new();
    [ObservableProperty] private DailyGoalInfo _dailyGoal = new();
    [ObservableProperty] private string _lastSeenText = "-";
    [ObservableProperty] private string _totalPlayText = "0 oyun";
    [ObservableProperty] private string _strongestCategory = "-";
    [ObservableProperty] private string _needsPracticeCategory = "-";
    [ObservableProperty] private bool _isLoading = true;

    public ParentalDashboardViewModel(ProfileService profileService, ContentService contentService)
    {
        _profileService = profileService;
        _contentService = contentService;
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        IsLoading = true;
        try
        {
            ActiveProfile = _profileService.GetActiveProfile();
            if (ActiveProfile is null) return;

            DailyGoal = _profileService.GetDailyGoal(ActiveProfile);
            TotalPlayText = $"{ActiveProfile.TotalLessonsCompleted} oyun";
            LastSeenText = GetLastSeenText(ActiveProfile.LastPlayedAt);

            WeekBars = BuildWeekBars(ActiveProfile);
            TopCategories = await BuildCategoryStatsAsync(ActiveProfile);
            RecentBadges = BuildRecentBadges(ActiveProfile);

            var strongest = TopCategories.OrderByDescending(c => c.Stars).FirstOrDefault();
            var weakest = TopCategories.Where(c => c.PlayCount > 0).OrderBy(c => c.Stars).FirstOrDefault();

            StrongestCategory = strongest is not null
                ? $"{strongest.Emoji} {strongest.Name}"
                : "-";
            NeedsPracticeCategory = weakest is not null && weakest != strongest
                ? $"{weakest.Emoji} {weakest.Name}"
                : "-";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public Task GoBackAsync() => Shell.Current.GoToAsync("..");

    private static string GetLastSeenText(DateTime lastPlayed)
    {
        var diff = DateTime.UtcNow - lastPlayed;
        return diff.TotalMinutes < 5 ? "Az önce"
            : diff.TotalHours < 1 ? $"{(int)diff.TotalMinutes} dakika önce"
            : diff.TotalDays < 1 ? $"{(int)diff.TotalHours} saat önce"
            : diff.TotalDays < 7 ? $"{(int)diff.TotalDays} gün önce"
            : lastPlayed.ToString("d MMMM", new System.Globalization.CultureInfo("tr-TR"));
    }

    private static List<WeekDayBar> BuildWeekBars(ChildProfile profile)
    {
        var today = DateTime.UtcNow.Date;
        var bars = new List<WeekDayBar>();
        var dayNames = new[] { "Pzt", "Sal", "Çar", "Per", "Cum", "Cmt", "Paz" };

        for (int i = 6; i >= 0; i--)
        {
            var day = today.AddDays(-i);
            var activity = profile.CategoryProgresses.Values
                .Count(cp => cp.LastPlayedAt.Date == day);

            bars.Add(new WeekDayBar
            {
                DayLabel = i == 0 ? "Bugün" : dayNames[(int)day.DayOfWeek == 0 ? 6 : (int)day.DayOfWeek - 1],
                ActivityCount = activity,
                HeightRatio = activity == 0 ? 0.05 : Math.Min(1.0, activity / 8.0),
                IsToday = i == 0
            });
        }
        return bars;
    }

    private async Task<List<CategoryStat>> BuildCategoryStatsAsync(ChildProfile profile)
    {
        var categories = await _contentService.GetCategoriesAsync(profile);
        var stats = new List<CategoryStat>();

        foreach (var cat in categories.Take(6))
        {
            profile.CategoryProgresses.TryGetValue(cat.Id, out var cp);
            stats.Add(new CategoryStat
            {
                Id = cat.Id,
                Name = cat.NameTr,
                Emoji = cat.Emoji,
                Stars = cp?.BestStars ?? 0,
                PlayCount = cp?.PlayCount ?? 0,
                BestScore = cp?.BestScore ?? 0
            });
        }
        return stats;
    }

    private static List<WeeklyBadge> BuildRecentBadges(ChildProfile profile)
    {
        var cutoff = DateTime.UtcNow.AddDays(-7);
        return profile.EarnedBadges
            .Where(kv => kv.Value >= cutoff)
            .OrderByDescending(kv => kv.Value)
            .Take(5)
            .Select(kv => new WeeklyBadge { BadgeId = kv.Key, EarnedAt = kv.Value })
            .ToList();
    }
}

public class WeekDayBar
{
    public string DayLabel { get; set; } = "";
    public int ActivityCount { get; set; }
    public double HeightRatio { get; set; }
    public bool IsToday { get; set; }
    public double BarHeight => Math.Max(6, HeightRatio * 80);
    public string BarColor => IsToday ? "#5148D4" : (ActivityCount > 0 ? "#A78BFA" : "#E5E7EB");
    public string LabelColor => IsToday ? "#5148D4" : "#6B7280";
}

public class CategoryStat
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Emoji { get; set; } = "";
    public int Stars { get; set; }
    public int PlayCount { get; set; }
    public int BestScore { get; set; }
    public string StarsText => Stars == 0 ? "Başlanmadı" : new string('⭐', Stars);
    public string PlayCountText => PlayCount == 0 ? "—" : $"{PlayCount}x";
    public Color ProgressColor => Stars switch
    {
        3 => Color.FromArgb("#22C55E"),
        2 => Color.FromArgb("#F59E0B"),
        1 => Color.FromArgb("#A78BFA"),
        _ => Color.FromArgb("#E5E7EB")
    };
}

public class WeeklyBadge
{
    public string BadgeId { get; set; } = "";
    public DateTime EarnedAt { get; set; }
    public string EarnedAtText => (DateTime.UtcNow - EarnedAt).TotalDays < 1 ? "Bugün"
        : $"{(int)(DateTime.UtcNow - EarnedAt).TotalDays} gün önce";
}
