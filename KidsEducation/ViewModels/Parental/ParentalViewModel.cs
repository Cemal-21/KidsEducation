using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KidsEducation.Models;
using KidsEducation.Services;

namespace KidsEducation.ViewModels.Parental;

public partial class ParentalViewModel : ObservableObject
{
    private const double WeakAccuracyThreshold = 0.6;

    private readonly ProfileService _profileService;
    private readonly ContentService _contentService;
    private readonly NavigationService _navigationService;
    private readonly LearningEventService _learningEventService;
    private readonly SkillCatalogService _skillCatalog;
    private readonly CurriculumActivityService _curriculumActivityService;

    [ObservableProperty] private ParentalSettings _settings = new();
    [ObservableProperty] private WeeklyReport _report = new();
    [ObservableProperty] private List<CategoryToggle> _categoryToggles = new();
    [ObservableProperty] private List<WeakCategoryInfo> _weakCategories = new();
    [ObservableProperty] private List<SkillSummaryInfo> _topSkills = new();
    [ObservableProperty] private List<SkillSummaryInfo> _supportSkills = new();
    [ObservableProperty] private List<CurriculumActivity> _curriculumActivities = new();
    [ObservableProperty] private string _weeklyInsightTitle = "Bu haftanin ozeti";
    [ObservableProperty] private string _weeklyInsightText = "Oyun verileri biriktikce burada daha net bir gelisim yorumu gorunecek.";
    [ObservableProperty] private string _nextBestStepText = "Bugun kisa bir dinleme veya hafiza oyunu ile baslamak iyi olur.";
    [ObservableProperty] private bool _hasWeakCategories;
    [ObservableProperty] private bool _hasSkillInsights;
    [ObservableProperty] private bool _hasCurriculumActivities;
    [ObservableProperty] private bool _isLoading = true;

    // Slider için int binding
    public int DailyLimitMinutes
    {
        get => Settings.DailyTimeLimitMinutes;
        set
        {
            Settings.DailyTimeLimitMinutes = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DailyLimitText));
        }
    }

    public string DailyLimitText => DailyLimitMinutes == 0
        ? "Sınırsız"
        : $"{DailyLimitMinutes} dakika";

    public bool SoundEnabled
    {
        get => Settings.SoundEnabled;
        set { Settings.SoundEnabled = value; OnPropertyChanged(); }
    }

    public bool WeeklyReportEnabled
    {
        get => Settings.WeeklyReportEnabled;
        set { Settings.WeeklyReportEnabled = value; OnPropertyChanged(); }
    }

    public ParentalViewModel(
        ProfileService profileService,
        ContentService contentService,
        NavigationService navigationService,
        LearningEventService learningEventService,
        SkillCatalogService skillCatalog,
        CurriculumActivityService curriculumActivityService)
    {
        _profileService = profileService;
        _contentService = contentService;
        _navigationService = navigationService;
        _learningEventService = learningEventService;
        _skillCatalog = skillCatalog;
        _curriculumActivityService = curriculumActivityService;
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        IsLoading = true;
        try
        {
            Settings = _profileService.GetParentalSettings();
            OnPropertyChanged(nameof(DailyLimitMinutes));
            OnPropertyChanged(nameof(DailyLimitText));
            OnPropertyChanged(nameof(SoundEnabled));
            OnPropertyChanged(nameof(WeeklyReportEnabled));

            var profile = _profileService.GetActiveProfile();
            if (profile is not null)
                Report = _profileService.GetWeeklyReport(profile.Id);

            await LoadCategoryTogglesAsync();
            await LoadWeakCategoriesAsync();
            LoadSkillInsights();
            LoadCurriculumActivities();
            LoadWeeklyInsight();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadCategoryTogglesAsync()
    {
        var profile = _profileService.GetActiveProfile();
        if (profile is null) return;

        var categories = await _contentService.GetCategoriesAsync(profile);

        CategoryToggles = categories.Select(c => new CategoryToggle
        {
            CategoryId = c.Id,
            NameTr = c.NameTr,
            Emoji = c.Emoji,
            IsVisible = !Settings.HiddenCategoryIds.Contains(c.Id)
        }).ToList();
    }

    private async Task LoadWeakCategoriesAsync()
    {
        var profile = _profileService.GetActiveProfile();
        if (profile is null) return;

        var accuracyByCategory = await _learningEventService.GetWeeklyAccuracyByCategoryAsync(profile.Id);
        if (accuracyByCategory.Count == 0)
        {
            WeakCategories = new();
            HasWeakCategories = false;
            return;
        }

        var categories = await _contentService.GetCategoriesAsync(profile);

        WeakCategories = accuracyByCategory
            .Where(kv => kv.Value < WeakAccuracyThreshold)
            .Select(kv =>
            {
                var category = categories.FirstOrDefault(c => c.Id == kv.Key);
                return new WeakCategoryInfo
                {
                    CategoryId = kv.Key,
                    NameTr = category?.NameTr ?? kv.Key,
                    Emoji = category?.Emoji ?? "📚",
                    AccuracyPercent = (int)Math.Round(kv.Value * 100)
                };
            })
            .OrderBy(w => w.AccuracyPercent)
            .ToList();

        HasWeakCategories = WeakCategories.Count > 0;
    }

    private void LoadSkillInsights()
    {
        var profile = _profileService.GetActiveProfile();
        if (profile is null || profile.SkillProgresses.Count == 0)
        {
            TopSkills = new();
            SupportSkills = new();
            HasSkillInsights = false;
            return;
        }

        var skillSummaries = profile.SkillProgresses.Values
            .Where(p => p.TotalAnswers > 0)
            .Select(progress =>
            {
                var skill = _skillCatalog.GetSkill(progress.SkillId);
                if (skill is null)
                    return null;

                return new SkillSummaryInfo
                {
                    SkillId = skill.Id,
                    Title = skill.Title,
                    Area = skill.Area,
                    Emoji = skill.Emoji,
                    Description = skill.Description,
                    Suggestion = skill.Suggestion,
                    PlayCount = progress.PlayCount,
                    AccuracyPercent = progress.AccuracyPercent,
                    LastPracticedAt = progress.LastPracticedAt
                };
            })
            .Where(s => s is not null)
            .Cast<SkillSummaryInfo>()
            .ToList();

        TopSkills = skillSummaries
            .OrderByDescending(s => s.PlayCount)
            .ThenByDescending(s => s.AccuracyPercent)
            .Take(3)
            .ToList();

        SupportSkills = skillSummaries
            .Where(s => s.AccuracyPercent < 70)
            .OrderBy(s => s.AccuracyPercent)
            .ThenByDescending(s => s.PlayCount)
            .Take(3)
            .ToList();

        if (SupportSkills.Count == 0 && skillSummaries.Count > 0)
        {
            SupportSkills = skillSummaries
                .OrderBy(s => s.AccuracyPercent)
                .Take(Math.Min(2, skillSummaries.Count))
                .ToList();
        }

        HasSkillInsights = TopSkills.Count > 0 || SupportSkills.Count > 0;
    }

    private void LoadCurriculumActivities()
    {
        var profile = _profileService.GetActiveProfile();
        if (profile is null)
        {
            CurriculumActivities = new();
            HasCurriculumActivities = false;
            return;
        }

        var prioritySkillIds = SupportSkills.Count > 0
            ? SupportSkills.Select(s => s.SkillId)
            : TopSkills.Select(s => s.SkillId);

        CurriculumActivities = _curriculumActivityService
            .GetRecommendedActivities(profile, prioritySkillIds, take: 3)
            .ToList();

        HasCurriculumActivities = CurriculumActivities.Count > 0;
    }

    private void LoadWeeklyInsight()
    {
        var support = SupportSkills.FirstOrDefault();
        var top = TopSkills.FirstOrDefault();
        var weak = WeakCategories.FirstOrDefault();
        var activity = CurriculumActivities.FirstOrDefault();

        if (support is not null)
        {
            WeeklyInsightTitle = $"{support.Title} desteklenebilir";
            WeeklyInsightText = $"{support.CurriculumText}. Dogruluk orani {support.AccuracyText}; kisa ve tekrarli etkinlikler bu alani guclendirir.";
        }
        else if (top is not null)
        {
            WeeklyInsightTitle = $"{top.Title} guclu gidiyor";
            WeeklyInsightText = $"{top.CurriculumText}. Bu alanda iyi bir oyun ritmi var; benzer etkinliklerle kalicilik artar.";
        }
        else
        {
            WeeklyInsightTitle = "Baslangic verisi hazirlaniyor";
            WeeklyInsightText = "Cocuk birkac oyun tamamladiktan sonra guclu alanlar, destek alanlari ve haftalik oneriler burada netlesir.";
        }

        if (weak is not null)
            NextBestStepText = $"Siradaki mini hedef: {weak.NameTr} konusunda 5 dakikalik pratik.";
        else if (activity is not null)
            NextBestStepText = $"Ev etkinligi onerisi: {activity.Title} ({activity.DurationText}).";
        else
            NextBestStepText = "Siradaki mini hedef: bir oyun, bir sarki ve bir kisa tekrar.";
    }

    [RelayCommand]
    public void ToggleCategory(CategoryToggle toggle)
    {
        toggle.IsVisible = !toggle.IsVisible;

        if (toggle.IsVisible)
            Settings.HiddenCategoryIds.Remove(toggle.CategoryId);
        else if (!Settings.HiddenCategoryIds.Contains(toggle.CategoryId))
            Settings.HiddenCategoryIds.Add(toggle.CategoryId);

        // Listeyi yenile
        OnPropertyChanged(nameof(CategoryToggles));
    }

    [RelayCommand]
    public void SaveSettings()
    {
        _profileService.SaveParentalSettings(Settings);
    }

    [RelayCommand]
    public async Task GoBackAsync() =>
        await _navigationService.GoBackAsync();
}

public class CategoryToggle : ObservableObject
{
    public string CategoryId { get; set; } = string.Empty;
    public string NameTr { get; set; } = string.Empty;
    public string Emoji { get; set; } = string.Empty;

    private bool _isVisible = true;
    public bool IsVisible
    {
        get => _isVisible;
        set => SetProperty(ref _isVisible, value);
    }

    public string VisibilityIcon => IsVisible ? "👁️" : "🚫";
}

public class WeakCategoryInfo
{
    public string CategoryId { get; set; } = string.Empty;
    public string NameTr { get; set; } = string.Empty;
    public string Emoji { get; set; } = string.Empty;
    public int AccuracyPercent { get; set; }

    public string AccuracyText => $"%{AccuracyPercent} doğru";
}

public class SkillSummaryInfo
{
    public string SkillId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public string Emoji { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Suggestion { get; set; } = string.Empty;
    public int PlayCount { get; set; }
    public int AccuracyPercent { get; set; }
    public DateTime LastPracticedAt { get; set; }

    public string AccuracyText => $"%{AccuracyPercent} doğru";
    public string PlayCountText => $"{PlayCount} etkinlik";
    public string CurriculumText => $"MEB uyumlu · {Area}";
    public string SummaryText => $"{CurriculumText} · {PlayCountText}";
}
