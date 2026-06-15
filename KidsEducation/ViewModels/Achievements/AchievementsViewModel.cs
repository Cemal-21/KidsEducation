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
    private readonly ShareService _shareService;

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
        NavigationService navigationService,
        ShareService shareService)
    {
        _badgeService = badgeService;
        _profileService = profileService;
        _navigationService = navigationService;
        _shareService = shareService;
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
    public Task GoBackAsync() =>
        Shell.Current.GoToAsync("//home");

    [RelayCommand]
    public async Task ShareBadgeAsync(Badge badge)
    {
        if (badge is null || !badge.IsEarned) return;
        var profileName = ActiveProfile?.Name ?? "Kahraman";
        await _shareService.ShareAchievementAsync(
            profileName, badge.NameTr, badge.Emoji, badge.DescriptionTr);
    }

    [RelayCommand]
    public async Task ShareProgressAsync()
    {
        if (ActiveProfile is null) return;
        await _shareService.ShareProgressAsync(
            ActiveProfile.Name, ActiveProfile.TotalStars,
            ActiveProfile.StreakDays, ActiveProfile.LevelTitle);
    }
}
