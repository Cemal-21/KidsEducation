using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KidsEducation.Models;
using KidsEducation.Services;

namespace KidsEducation.ViewModels.Achievements;

public partial class AchievementsViewModel : ObservableObject
{
    private readonly BadgeService _badgeService;
    private readonly ProfileService _profileService;
    private readonly NavigationService _navigationService;

    [ObservableProperty] private List<Badge> _badges = new();
    [ObservableProperty] private ChildProfile? _activeProfile;
    [ObservableProperty] private bool _isLoading = true;

    // Özet sayaçları
    [ObservableProperty] private int _earnedCount;
    [ObservableProperty] private int _totalCount;
    public string ProgressText => $"{EarnedCount} / {TotalCount}";

    public AchievementsViewModel(
        BadgeService badgeService,
        ProfileService profileService,
        NavigationService navigationService)
    {
        _badgeService = badgeService;
        _profileService = profileService;
        _navigationService = navigationService;
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        IsLoading = true;
        try
        {
            ActiveProfile = _profileService.GetActiveProfile();
            if (ActiveProfile is null) return;

            Badges = _badgeService.EvaluateBadges(ActiveProfile);
            TotalCount = Badges.Count;
            EarnedCount = Badges.Count(b => b.IsEarned);
            OnPropertyChanged(nameof(ProgressText));
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task GoBackAsync() =>
        await _navigationService.GoBackAsync();
}
