using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KidsEducation.Models;
using KidsEducation.Services;
using System.Collections.ObjectModel;

namespace KidsEducation.ViewModels.Report;

public partial class DayBarItem : ObservableObject
{
    public string DayLabel { get; set; } = string.Empty;
    public int Minutes { get; set; }
    public bool IsToday { get; set; }

    // Çubuk yüksekliği: max 60px, oransal
    public double BarHeight { get; set; }
    public string BarColor => IsToday ? "#5148D4" : "#A5B4FC";
    public string DayLabelColor => IsToday ? "#5148D4" : "#94A3B8";
    public string MinutesText => Minutes > 0 ? $"{Minutes}dk" : "";
}

public partial class CategoryHighlight : ObservableObject
{
    public string Emoji { get; set; } = string.Empty;
    public string NameTr { get; set; } = string.Empty;
    public int Stars { get; set; }
    public string StarsText => Stars > 0 ? new string('⭐', Math.Min(Stars, 5)) : "—";
}

public partial class ProgressReportViewModel : ObservableObject
{
    private readonly ProfileService _profileService;
    private readonly ContentService _contentService;
    private readonly NavigationService _navigationService;

    [ObservableProperty] private bool _isLoading = true;
    [ObservableProperty] private string _profileName = "";
    [ObservableProperty] private string _avatarEmoji = "🐰";
    [ObservableProperty] private string _weekRangeText = "";

    // Haftalık özet istatistikler
    [ObservableProperty] private int _weeklyStars;
    [ObservableProperty] private int _weeklyLessons;
    [ObservableProperty] private int _totalStars;
    [ObservableProperty] private int _streakDays;
    [ObservableProperty] private string _streakText = "";
    [ObservableProperty] private string _achievementBadge = "";
    [ObservableProperty] private string _achievementText = "";

    // Günlük aktivite çubuğu
    public ObservableCollection<DayBarItem> DayBars { get; } = new();

    // En iyi kategoriler
    public ObservableCollection<CategoryHighlight> TopCategories { get; } = new();

    [ObservableProperty] private bool _hasTopCategories;

    public ProgressReportViewModel(
        ProfileService profileService,
        ContentService contentService,
        NavigationService navigationService)
    {
        _profileService = profileService;
        _contentService = contentService;
        _navigationService = navigationService;
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        IsLoading = true;
        try
        {
            var profile = _profileService.GetActiveProfile();
            if (profile is null) return;

            ProfileName = profile.Name;
            AvatarEmoji = profile.AvatarEmoji;

            var report = _profileService.GetWeeklyReport(profile.Id);
            WeeklyStars = report.WeeklyStars;
            WeeklyLessons = report.WeeklyLessons;
            TotalStars = report.TotalStars;
            StreakDays = report.StreakDays;

            var today = DateTime.Today;
            var weekStart = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);
            WeekRangeText = $"{weekStart:d MMM} – {weekStart.AddDays(6):d MMM}";

            // Streak rozeti
            (AchievementBadge, AchievementText) = report.StreakDays switch
            {
                >= 30 => ("🏆", "Efsane Seri!"),
                >= 14 => ("🔥", "İnanılmaz!"),
                >= 7  => ("⭐", "Harika Hafta!"),
                >= 3  => ("👏", "Devam Et!"),
                _     => ("🌱", "Başlangıç")
            };
            StreakText = report.StreakDays > 0
                ? $"🔥 {report.StreakDays} günlük seri"
                : "Bugün oyna, seri başlat!";

            // Günlük aktivite çubukları
            string[] dayNames = ["Pzt", "Sal", "Çar", "Per", "Cum", "Cmt", "Paz"];
            int todayIndex = ((int)today.DayOfWeek + 6) % 7; // Pazartesi=0
            int maxMin = report.DailyMinutes.Max();
            if (maxMin == 0) maxMin = 1;

            DayBars.Clear();
            for (int i = 0; i < 7; i++)
            {
                DayBars.Add(new DayBarItem
                {
                    DayLabel = dayNames[i],
                    Minutes = report.DailyMinutes[i],
                    IsToday = i == todayIndex,
                    BarHeight = Math.Max(4, (double)report.DailyMinutes[i] / maxMin * 60)
                });
            }

            // En iyi 3 kategori (bu hafta oynanan)
            TopCategories.Clear();
            var cats = await _contentService.GetCategoriesAsync(profile);
            var topCats = profile.CategoryProgresses
                .Where(kv => kv.Value.LastPlayedAt.Date >= weekStart && kv.Value.BestStars > 0)
                .OrderByDescending(kv => kv.Value.BestStars)
                .Take(3)
                .ToList();

            foreach (var kv in topCats)
            {
                var cat = cats.FirstOrDefault(c => c.Id == kv.Key);
                if (cat is null) continue;
                TopCategories.Add(new CategoryHighlight
                {
                    Emoji = cat.Emoji,
                    NameTr = cat.NameTr,
                    Stars = kv.Value.BestStars
                });
            }
            HasTopCategories = TopCategories.Count > 0;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task ShareReportAsync()
    {
        var weekStarLine = WeeklyStars > 0 ? $"⭐ Bu hafta {WeeklyStars} yıldız" : "Bu hafta henüz yıldız kazanılmadı";
        var streakLine   = StreakDays > 0   ? $"🔥 {StreakDays} günlük seri" : "";
        var topLine      = TopCategories.Count > 0
            ? "🏅 En iyi kategoriler: " + string.Join(", ", TopCategories.Select(c => $"{c.Emoji}{c.NameTr}"))
            : "";

        var lines = new List<string>
        {
            $"📊 {ProfileName}'in haftalık öğrenme raporu ({WeekRangeText})",
            "",
            weekStarLine,
            $"📚 {WeeklyLessons} ders tamamlandı",
        };
        if (!string.IsNullOrEmpty(streakLine)) lines.Add(streakLine);
        if (!string.IsNullOrEmpty(topLine)) lines.Add(topLine);
        lines.Add("");
        lines.Add($"🎓 Toplam {TotalStars} yıldız kazanıldı");
        lines.Add("");
        lines.Add("KidsEducation uygulamasıyla öğrenmek çok eğlenceli! 🚀");

        var text = string.Join("\n", lines);

        await Share.RequestAsync(new ShareTextRequest
        {
            Title = $"{ProfileName}'in Haftalık Raporu",
            Text = text
        });
    }

    [RelayCommand]
    public Task GoBackAsync() => _navigationService.GoBackAsync();
}
