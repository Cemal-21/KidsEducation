using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KidsEducation.Models;
using KidsEducation.Services;

namespace KidsEducation.ViewModels.Home;

public partial class DailyGoalViewModel : ObservableObject
{
    private readonly ProfileService _profileService;
    private readonly NavigationService _navigationService;

    [ObservableProperty] private ChildProfile? _profile;
    [ObservableProperty] private DailyGoalInfo _dailyGoal = new();
    [ObservableProperty] private bool _isLoading = true;
    [ObservableProperty] private string _streakMessage = "";

    public DailyGoalViewModel(ProfileService profileService, NavigationService navigationService)
    {
        _profileService = profileService;
        _navigationService = navigationService;
    }

    [RelayCommand]
    public void Initialize()
    {
        IsLoading = true;
        try
        {
            Profile = _profileService.GetActiveProfile();
            if (Profile is null) return;

            DailyGoal = _profileService.GetDailyGoal(Profile);

            StreakMessage = Profile.StreakDays switch
            {
                0 or 1 => "Bugün harika bir başlangıç!",
                <= 3 => $"{Profile.StreakDays} gün üst üste öğreniyorsun!",
                <= 7 => $"{Profile.StreakDays} günlük seri, muhteşem!",
                <= 14 => $"{Profile.StreakDays} gün, durmak yok!",
                _ => $"{Profile.StreakDays} günlük efsane seri!"
            };
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task GoToQuestGameAsync(DailyQuestInfo quest)
    {
        if (string.IsNullOrWhiteSpace(quest.ActionRoute)) return;
        await Shell.Current.GoToAsync(quest.ActionRoute);
    }

    [RelayCommand]
    public Task GoBackAsync() => _navigationService.GoBackAsync();
}
