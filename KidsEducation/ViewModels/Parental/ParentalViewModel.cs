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
    private readonly AudioService _audioService;
    private readonly AppPreferencesService _appPreferences;
    private readonly NotificationService _notificationService;
    private readonly ProgressBackupService _backupService;

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

    // AI Koç API key
    [ObservableProperty] private string _anthropicApiKey = string.Empty;
    [ObservableProperty] private string _apiKeySaveStatus = "";

    partial void OnAnthropicApiKeyChanged(string v)
    {
        ApiKeySaveStatus = "";
    }

    [RelayCommand]
    public void SaveApiKey()
    {
        _appPreferences.AnthropicApiKey = AnthropicApiKey.Trim();
        ApiKeySaveStatus = string.IsNullOrWhiteSpace(AnthropicApiKey)
            ? "API key silindi."
            : "✅ Kaydedildi! AI Koç aktif.";
    }

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
        get => _audioService.EffectsEnabled;
        set
        {
            _audioService.SetEffectsEnabled(value);
            Settings.SoundEnabled = value;
            OnPropertyChanged();
        }
    }

    public bool MusicEnabled
    {
        get => _audioService.MusicEnabled;
        set
        {
            _audioService.SetMusicEnabled(value);
            OnPropertyChanged();
        }
    }

    public double MusicVolumeLevel
    {
        get => _audioService.MusicVolume;
        set
        {
            _audioService.SetMusicVolume(value);
            OnPropertyChanged();
        }
    }

    public double EffectsVolumeLevel
    {
        get => _audioService.EffectsVolume;
        set
        {
            _audioService.SetEffectsVolume(value);
            OnPropertyChanged();
        }
    }

    public double SpeechRateLevel
    {
        get => _appPreferences.SpeechRate;
        set
        {
            _appPreferences.SpeechRate = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SpeechRateText));
        }
    }

    public string SpeechRateText => _appPreferences.SpeechRate switch
    {
        <= 0.6  => "Çok Yavaş",
        <= 0.85 => "Yavaş",
        <= 1.15 => "Normal",
        <= 1.35 => "Hızlı",
        _       => "Çok Hızlı"
    };

    public bool WeeklyReportEnabled
    {
        get => Settings.WeeklyReportEnabled;
        set { Settings.WeeklyReportEnabled = value; OnPropertyChanged(); }
    }

    public bool DarkModeEnabled
    {
        get => _appPreferences.ThemePreference == "dark";
        set
        {
            _appPreferences.ThemePreference = value ? "dark" : "light";
            OnPropertyChanged();
        }
    }

    [ObservableProperty] private List<ColorThemeItem> _colorThemes = new();

    public string SelectedColorThemeId => _appPreferences.ColorTheme;

    private void LoadColorThemes()
    {
        ColorThemes = AppPreferencesService.ColorThemes
            .Select(t => new ColorThemeItem
            {
                Id = t.Id,
                Label = t.Label,
                Emoji = t.Emoji,
                PrimaryHex = t.Primary,
                IsSelected = t.Id == _appPreferences.ColorTheme
            })
            .ToList();
    }

    private const string NotificationEnabledKey = "notif_daily_enabled";
    private const string NotificationHourKey = "notif_daily_hour";
    private const string NotificationMinuteKey = "notif_daily_minute";

    public bool NotificationEnabled
    {
        get => Preferences.Get(NotificationEnabledKey, false);
        set
        {
            Preferences.Set(NotificationEnabledKey, value);
            OnPropertyChanged();
            _ = value
                ? _notificationService.ScheduleDailyReminderAsync(new TimeSpan(NotificationHour, NotificationMinute, 0))
                : _notificationService.CancelDailyReminderAsync();
        }
    }

    public int NotificationHour
    {
        get => Preferences.Get(NotificationHourKey, 18);
        set { Preferences.Set(NotificationHourKey, value); OnPropertyChanged(); OnPropertyChanged(nameof(NotificationTimeText)); }
    }

    public int NotificationMinute
    {
        get => Preferences.Get(NotificationMinuteKey, 0);
        set { Preferences.Set(NotificationMinuteKey, value); OnPropertyChanged(); OnPropertyChanged(nameof(NotificationTimeText)); }
    }

    public TimeSpan NotificationTime
    {
        get => new TimeSpan(NotificationHour, NotificationMinute, 0);
        set
        {
            NotificationHour = value.Hours;
            NotificationMinute = value.Minutes;
            if (NotificationEnabled)
                _ = _notificationService.ScheduleDailyReminderAsync(value);
        }
    }

    public string NotificationTimeText => $"{NotificationHour:D2}:{NotificationMinute:D2}";

    public ParentalViewModel(
        ProfileService profileService,
        ContentService contentService,
        NavigationService navigationService,
        LearningEventService learningEventService,
        SkillCatalogService skillCatalog,
        CurriculumActivityService curriculumActivityService,
        AudioService audioService,
        AppPreferencesService appPreferences,
        NotificationService notificationService,
        ProgressBackupService backupService)
    {
        _profileService = profileService;
        _contentService = contentService;
        _navigationService = navigationService;
        _learningEventService = learningEventService;
        _skillCatalog = skillCatalog;
        _curriculumActivityService = curriculumActivityService;
        _audioService = audioService;
        _appPreferences = appPreferences;
        _notificationService = notificationService;
        _backupService = backupService;
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
            OnPropertyChanged(nameof(MusicEnabled));
            OnPropertyChanged(nameof(MusicVolumeLevel));
            OnPropertyChanged(nameof(EffectsVolumeLevel));
            OnPropertyChanged(nameof(WeeklyReportEnabled));
            OnPropertyChanged(nameof(DarkModeEnabled));
            OnPropertyChanged(nameof(SpeechRateLevel));
            OnPropertyChanged(nameof(SpeechRateText));
            LoadColorThemes();
            OnPropertyChanged(nameof(NotificationEnabled));
            OnPropertyChanged(nameof(NotificationTime));
            OnPropertyChanged(nameof(NotificationTimeText));

            var profile = _profileService.GetActiveProfile();
            if (profile is not null)
                Report = _profileService.GetWeeklyReport(profile.Id);

            await LoadCategoryTogglesAsync();
            await LoadWeakCategoriesAsync();
            LoadSkillInsights();
            LoadCurriculumActivities();
            LoadWeeklyInsight();
            AnthropicApiKey = _appPreferences.AnthropicApiKey;
            RefreshPinStatus();
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
    public void SelectColorTheme(ColorThemeItem item)
    {
        _appPreferences.ColorTheme = item.Id;
        foreach (var t in ColorThemes)
            t.IsSelected = t.Id == item.Id;
        OnPropertyChanged(nameof(ColorThemes));
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

    [ObservableProperty] private bool _hasPin;
    [ObservableProperty] private string _pinStatusText = "";

    public void RefreshPinStatus()
    {
        HasPin = _appPreferences.HasPin;
        PinStatusText = HasPin
            ? "PIN aktif — değiştirmek veya kaldırmak için aşağıyı kullanın"
            : "PIN yok — ebeveyn panelini korumak için PIN belirleyin";
    }

    [RelayCommand]
    public Task SetupPinAsync() => Shell.Current.GoToAsync("pinsetup");

    [RelayCommand]
    public async Task RemovePinAsync()
    {
        bool confirm = await Shell.Current.DisplayAlert(
            "PIN Kaldır", "Ebeveyn PIN korumasını kaldırmak istiyor musunuz?", "Kaldır", "İptal");
        if (confirm)
        {
            _appPreferences.ClearPin();
            RefreshPinStatus();
        }
    }

    [RelayCommand]
    public Task GoToProgressReportAsync() => Shell.Current.GoToAsync("progressreport");

    [RelayCommand]
    public Task GoToDashboardAsync() => Shell.Current.GoToAsync("parentaldashboard");

    [RelayCommand]
    public Task GoBackAsync() =>
        Shell.Current.GoToAsync("//home");

    [ObservableProperty] private string _backupStatus = "";

    [RelayCommand]
    public async Task ExportBackupAsync()
    {
        var ok = await _backupService.ExportAsync();
        BackupStatus = ok ? "✅ Yedek dışa aktarıldı." : "❌ Dışa aktarma başarısız.";
    }

    [RelayCommand]
    public async Task ImportBackupAsync()
    {
        var result = await _backupService.ImportAsync();
        BackupStatus = result.SummaryText;
    }
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

public class ColorThemeItem : ObservableObject
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Emoji { get; set; } = string.Empty;
    public string PrimaryHex { get; set; } = "#5148D4";

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public Color PrimaryColor => Color.FromArgb(PrimaryHex);
    public double SelectedOpacity => IsSelected ? 1.0 : 0.0;
    public double BorderWidth => IsSelected ? 3 : 1.5;
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
