using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KidsEducation.Models;
using KidsEducation.Services;

namespace KidsEducation.ViewModels.Profile;

public partial class ProfileViewModel : ObservableObject
{
    private readonly ProfileService _profileService;
    private readonly BadgeService _badgeService;
    private readonly NavigationService _navigationService;

    [ObservableProperty] private ChildProfile? _activeProfile;
    [ObservableProperty] private int _earnedBadgeCount;
    [ObservableProperty] private List<CalendarWeek> _calendarWeeks = new();
    [ObservableProperty] private bool _isLoading = true;
    [ObservableProperty] private string _editableName = string.Empty;
    [ObservableProperty] private string _profileUpdateStatus = string.Empty;
    [ObservableProperty] private int _editableAgeGroupValue = 1;
    [ObservableProperty] private int _editableDailyGoalTarget = 3;
    [ObservableProperty] private string _editableLearningMode = "balanced";
    [ObservableProperty] private int _todayCompletedLessons;

    public IReadOnlyList<ProfileAgeOption> AgeOptions { get; } =
    [
        new((int)KidsEducation.Enums.AgeGroup.Toddler, "Minikler", "3-5 yas", "\U0001F430"),
        new((int)KidsEducation.Enums.AgeGroup.Explorer, "Kesifciler", "5-7 yas", "\U0001F98A"),
        new((int)KidsEducation.Enums.AgeGroup.Adventurer, "Kasifler", "7-9 yas", "\U0001F989")
    ];

    public IReadOnlyList<ProfileGoalOption> DailyGoalOptions { get; } =
    [
        new(2, "Mini", "2 oyun"),
        new(3, "Rutin", "3 oyun"),
        new(5, "Super", "5 oyun")
    ];

    public IReadOnlyList<LearningModeOption> LearningModes { get; } =
    [
        new("gentle", "Sakin", "Rahat tempo"),
        new("balanced", "Dengeli", "Kararli akis"),
        new("focus", "Odak", "Daha hedefli")
    ];

    public string ProfileInsightText => ActiveProfile is null
        ? "Profil secildiginde burada kisa bir ozet gorunecek."
        : $"{ActiveProfile.LevelText} • {ActiveProfile.LevelTitle} • {ActiveProfile.StreakDays} gun seri";

    public string ActivitySummaryText => ActiveProfile is null
        ? "Bugunku ritim hazirlaniyor."
        : ActiveProfile.TotalLessonsCompleted == 0
            ? "Yeni bir baslangic icin her sey hazir."
            : $"{ActiveProfile.TotalLessonsCompleted} ders ve {ActiveProfile.TotalStars} yildiz ile guzel bir birikim olustu.";

    public string DailyGoalProgressText => ActiveProfile is null
        ? "Gunluk hedef secildiginde burada ilerleme gorunecek."
        : $"{Math.Min(TodayCompletedLessons, EditableDailyGoalTarget)}/{EditableDailyGoalTarget} oyun tamamlandi";

    public string LearningModeSummaryText => EditableLearningMode switch
    {
        "focus" => "Bugun daha odakli ve hedefe yonelik bir akis onerilecek.",
        "gentle" => "Bugun daha yumusak ve yormayan bir tempo tercih edilecek.",
        _ => "Bugun dengeli bir oyun ve tekrar ritmi korunacak."
    };

    public bool CanSaveProfile =>
        ActiveProfile is not null &&
        !string.IsNullOrWhiteSpace(EditableName) &&
        EditableName.Trim().Length >= 2;
    public Color EditToddlerBackground => EditableAgeGroupValue == 1 ? Color.FromArgb("#EDEBFF") : Color.FromArgb("#FFFFFF");
    public Color EditExplorerBackground => EditableAgeGroupValue == 2 ? Color.FromArgb("#E7FFF7") : Color.FromArgb("#FFFFFF");
    public Color EditAdventurerBackground => EditableAgeGroupValue == 3 ? Color.FromArgb("#FFF3E8") : Color.FromArgb("#FFFFFF");
    public Color GoalMiniBackground => EditableDailyGoalTarget == 2 ? Color.FromArgb("#EEF6FF") : Color.FromArgb("#FFFFFF");
    public Color GoalRoutineBackground => EditableDailyGoalTarget == 3 ? Color.FromArgb("#F4F0FF") : Color.FromArgb("#FFFFFF");
    public Color GoalSuperBackground => EditableDailyGoalTarget == 5 ? Color.FromArgb("#FFF4EA") : Color.FromArgb("#FFFFFF");
    public Color GentleModeBackground => EditableLearningMode == "gentle" ? Color.FromArgb("#EEF9F2") : Color.FromArgb("#FFFFFF");
    public Color BalancedModeBackground => EditableLearningMode == "balanced" ? Color.FromArgb("#F4F0FF") : Color.FromArgb("#FFFFFF");
    public Color FocusModeBackground => EditableLearningMode == "focus" ? Color.FromArgb("#FFF4EA") : Color.FromArgb("#FFFFFF");

    public ProfileViewModel(
        ProfileService profileService,
        BadgeService badgeService,
        NavigationService navigationService)
    {
        _profileService = profileService;
        _badgeService = badgeService;
        _navigationService = navigationService;
    }

    [RelayCommand]
    public Task InitializeAsync()
    {
        IsLoading = true;
        try
        {
            ActiveProfile = _profileService.GetActiveProfile();
            if (ActiveProfile is null) return Task.CompletedTask;

            EarnedBadgeCount = ActiveProfile.EarnedBadges.Count;
            CalendarWeeks = BuildCalendar(ActiveProfile);
            EditableName = ActiveProfile.Name;
            EditableAgeGroupValue = (int)ActiveProfile.AgeGroup;
            EditableDailyGoalTarget = Math.Max(1, ActiveProfile.DailyGoalTarget);
            EditableLearningMode = string.IsNullOrWhiteSpace(ActiveProfile.LearningMode) ? "balanced" : ActiveProfile.LearningMode;
            TodayCompletedLessons = _profileService.GetTodayActivityStats(ActiveProfile.Id).LessonsCompleted;
            ProfileUpdateStatus = string.Empty;
            NotifyProfileSummary();
        }
        finally
        {
            IsLoading = false;
        }
        return Task.CompletedTask;
    }

    [RelayCommand]
    public async Task SwitchProfileAsync() =>
        await _navigationService.GoToProfileSelectionAsync();

    [RelayCommand]
    public async Task GoToPreferencesAsync() =>
        await _navigationService.GoToPreferencesAsync();

    [RelayCommand]
    public async Task GoToParentalAsync() =>
        await _navigationService.GoToParentalAsync();

    [RelayCommand]
    public void SelectEditAgeGroup(string value)
    {
        if (int.TryParse(value, out var parsed))
        {
            EditableAgeGroupValue = parsed;
            ProfileUpdateStatus = "Yas grubu secildi. Kaydetmeye hazir.";
            OnPropertyChanged(nameof(CanSaveProfile));
        }
    }

    [RelayCommand]
    public void SelectDailyGoalTarget(string value)
    {
        if (int.TryParse(value, out var parsed))
        {
            EditableDailyGoalTarget = parsed;
            ProfileUpdateStatus = "Gunluk hedef secildi. Kaydetmeye hazir.";
            NotifyProfileSummary();
        }
    }

    [RelayCommand]
    public void SelectLearningMode(string mode)
    {
        if (string.IsNullOrWhiteSpace(mode))
            return;

        EditableLearningMode = mode;
        ProfileUpdateStatus = "Ogrenme modu guncellendi. Kaydetmeye hazir.";
        NotifyProfileSummary();
    }

    [RelayCommand]
    public void SaveProfileDetails()
    {
        if (ActiveProfile is null)
            return;

        var trimmedName = EditableName.Trim();
        if (trimmedName.Length < 2)
        {
            ProfileUpdateStatus = "Profil adi en az 2 harf olmali.";
            return;
        }

        var duplicateExists = _profileService.GetAllProfiles()
            .Any(profile => profile.Id != ActiveProfile.Id &&
                            string.Equals(profile.Name.Trim(), trimmedName, StringComparison.OrdinalIgnoreCase));
        if (duplicateExists)
        {
            ProfileUpdateStatus = "Bu isim baska bir profilde kullaniliyor.";
            return;
        }

        ActiveProfile.Name = trimmedName;
        ActiveProfile.AgeGroup = (KidsEducation.Enums.AgeGroup)EditableAgeGroupValue;
        ActiveProfile.DailyGoalTarget = EditableDailyGoalTarget;
        ActiveProfile.LearningMode = EditableLearningMode;
        _profileService.SaveProfile(ActiveProfile);
        ProfileUpdateStatus = "Profil bilgileri guncellendi.";
        NotifyProfileSummary();
    }

    partial void OnEditableNameChanged(string value)
    {
        ProfileUpdateStatus = string.Empty;
        OnPropertyChanged(nameof(CanSaveProfile));
    }

    partial void OnEditableAgeGroupValueChanged(int value)
    {
        OnPropertyChanged(nameof(CanSaveProfile));
        OnPropertyChanged(nameof(EditToddlerBackground));
        OnPropertyChanged(nameof(EditExplorerBackground));
        OnPropertyChanged(nameof(EditAdventurerBackground));
    }

    partial void OnEditableDailyGoalTargetChanged(int value)
    {
        OnPropertyChanged(nameof(GoalMiniBackground));
        OnPropertyChanged(nameof(GoalRoutineBackground));
        OnPropertyChanged(nameof(GoalSuperBackground));
        OnPropertyChanged(nameof(DailyGoalProgressText));
    }

    partial void OnEditableLearningModeChanged(string value)
    {
        OnPropertyChanged(nameof(GentleModeBackground));
        OnPropertyChanged(nameof(BalancedModeBackground));
        OnPropertyChanged(nameof(FocusModeBackground));
        OnPropertyChanged(nameof(LearningModeSummaryText));
    }

    partial void OnTodayCompletedLessonsChanged(int value)
    {
        OnPropertyChanged(nameof(DailyGoalProgressText));
    }

    /// <summary>
    /// Son 28 günü 4 haftalık satıra böler.
    /// Her gün için oynandı/oynanmadı rengi hesaplar.
    /// </summary>
    private static List<CalendarWeek> BuildCalendar(ChildProfile profile)
    {
        var today = DateTime.Today;
        var start = today.AddDays(-27);

        // Oynanan günleri bir HashSet'e al
        var playedDays = profile.CategoryProgresses.Values
            .Select(cp => cp.LastPlayedAt.Date)
            .ToHashSet();

        var weeks = new List<CalendarWeek>();
        var current = start;

        for (int w = 0; w < 4; w++)
        {
            var week = new CalendarWeek();
            for (int d = 0; d < 7; d++)
            {
                bool played = playedDays.Contains(current);
                bool isToday = current == today;
                bool future = current > today;

                string color = future ? "#F0F0F0"
                             : isToday ? "#4C44C6"
                             : played ? "#6C62F5"
                                       : "#EDE8FF";

                week.Colors[d] = color;
                current = current.AddDays(1);
            }
            weeks.Add(week);
        }
        return weeks;
    }

    private void NotifyProfileSummary()
    {
        OnPropertyChanged(nameof(ProfileInsightText));
        OnPropertyChanged(nameof(ActivitySummaryText));
        OnPropertyChanged(nameof(DailyGoalProgressText));
        OnPropertyChanged(nameof(LearningModeSummaryText));
        OnPropertyChanged(nameof(CanSaveProfile));
    }
}

public class CalendarWeek
{
    public string[] Colors { get; set; } = new string[7];

    public string Day0Color => Colors[0];
    public string Day1Color => Colors[1];
    public string Day2Color => Colors[2];
    public string Day3Color => Colors[3];
    public string Day4Color => Colors[4];
    public string Day5Color => Colors[5];
    public string Day6Color => Colors[6];
}

public record ProfileAgeOption(int Value, string Title, string Range, string Emoji);
public record ProfileGoalOption(int Value, string Title, string Subtitle);
public record LearningModeOption(string Value, string Title, string Subtitle);
