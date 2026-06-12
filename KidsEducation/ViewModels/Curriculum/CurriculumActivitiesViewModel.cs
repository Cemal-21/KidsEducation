using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KidsEducation.Models;
using KidsEducation.Services;

namespace KidsEducation.ViewModels.Curriculum;

public partial class CurriculumActivitiesViewModel : ObservableObject
{
    private readonly ProfileService _profileService;
    private readonly CurriculumActivityService _activityService;
    private readonly NavigationService _navigationService;

    [ObservableProperty] private List<CurriculumActivity> _activities = new();
    [ObservableProperty] private string _headline = "Evde 5 dakikalik etkinlikler";
    [ObservableProperty] private string _subtitle = "MEB okul oncesi alan becerilerini destekleyen kisa aile katilimi onerileri.";
    [ObservableProperty] private bool _isLoading = true;

    public CurriculumActivitiesViewModel(
        ProfileService profileService,
        CurriculumActivityService activityService,
        NavigationService navigationService)
    {
        _profileService = profileService;
        _activityService = activityService;
        _navigationService = navigationService;
    }

    [RelayCommand]
    public Task InitializeAsync()
    {
        IsLoading = true;
        try
        {
            var profile = _profileService.GetActiveProfile();
            if (profile is null)
            {
                Activities = new();
                return Task.CompletedTask;
            }

            var prioritySkillIds = profile.SkillProgresses.Values
                .Where(p => p.TotalAnswers > 0)
                .OrderBy(p => p.AccuracyPercent)
                .ThenByDescending(p => p.PlayCount)
                .Select(p => p.SkillId)
                .Take(4);

            Activities = _activityService
                .GetRecommendedActivities(profile, prioritySkillIds, take: 9)
                .ToList();

            Headline = $"{profile.Name} icin ev etkinlikleri";
        }
        finally
        {
            IsLoading = false;
        }

        return Task.CompletedTask;
    }

    [RelayCommand]
    public Task GoBackAsync() => _navigationService.GoBackAsync();
}
