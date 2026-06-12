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
