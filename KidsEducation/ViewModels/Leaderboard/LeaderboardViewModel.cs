using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KidsEducation.Models;
using KidsEducation.Services;
using System.Collections.ObjectModel;

namespace KidsEducation.ViewModels.Leaderboard;

public partial class LeaderboardEntry : ObservableObject
{
    public int Rank { get; set; }
    public string ProfileId { get; set; } = string.Empty;
    public string AvatarEmoji { get; set; } = "🐰";
    public string Name { get; set; } = string.Empty;
    public int TotalStars { get; set; }
    public int TotalXp { get; set; }
    public int StreakDays { get; set; }
    public int TotalLessons { get; set; }
    public bool IsActiveProfile { get; set; }
    public string AgeGroupName { get; set; } = string.Empty;

    // Görsel hesaplamalar
    public string RankEmoji => Rank switch
    {
        1 => "🥇",
        2 => "🥈",
        3 => "🥉",
        _ => $"{Rank}."
    };

    public bool IsTopThree => Rank <= 3;

    public string CardBorderColor => Rank switch
    {
        1 => "#FFD700",
        2 => "#C0C0C0",
        3 => "#CD7F32",
        _ => "#E7EBF5"
    };

    public string CardBgColor => IsActiveProfile ? "#F0EEFF" : "#FFFFFF";
    public string ActiveBadgeColor => IsActiveProfile ? "#5148D4" : "Transparent";
    public string XpFormatted => TotalXp >= 1000 ? $"{TotalXp / 1000.0:0.#}K XP" : $"{TotalXp} XP";
    public string StarsText => $"⭐ {TotalStars}";
    public string StreakText => StreakDays > 0 ? $"🔥 {StreakDays} gün" : "—";
}

public partial class LeaderboardViewModel : ObservableObject
{
    private readonly ProfileService _profileService;
    private readonly NavigationService _navigationService;

    [ObservableProperty] private bool _isLoading = true;
    [ObservableProperty] private bool _isEmpty;
    [ObservableProperty] private string _totalProfilesText = "";
    [ObservableProperty] private string _sortModeText = "Yıldıza göre";
    [ObservableProperty] private int _sortModeIndex; // 0=Yıldız 1=XP 2=Seri

    public ObservableCollection<LeaderboardEntry> Entries { get; } = new();

    public LeaderboardViewModel(ProfileService profileService, NavigationService navigationService)
    {
        _profileService = profileService;
        _navigationService = navigationService;
    }

    [RelayCommand]
    public Task InitializeAsync() => LoadAsync();

    private Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var profiles = _profileService.GetAllProfiles();
            var active = _profileService.GetActiveProfile();
            IsEmpty = profiles.Count == 0;
            TotalProfilesText = profiles.Count switch
            {
                0 => "Henüz profil yok",
                1 => "1 profil",
                _ => $"{profiles.Count} profil"
            };

            var sorted = SortModeIndex switch
            {
                1 => profiles.OrderByDescending(p => p.TotalXp).ThenByDescending(p => p.TotalStars),
                2 => profiles.OrderByDescending(p => p.StreakDays).ThenByDescending(p => p.TotalStars),
                _ => profiles.OrderByDescending(p => p.TotalStars).ThenByDescending(p => p.TotalXp)
            };

            Entries.Clear();
            int rank = 1;
            foreach (var p in sorted)
            {
                Entries.Add(new LeaderboardEntry
                {
                    Rank = rank++,
                    ProfileId = p.Id,
                    AvatarEmoji = p.AvatarEmoji,
                    Name = p.Name,
                    TotalStars = p.TotalStars,
                    TotalXp = p.TotalXp,
                    StreakDays = p.StreakDays,
                    TotalLessons = p.TotalLessonsCompleted,
                    IsActiveProfile = p.Id == active?.Id,
                    AgeGroupName = p.AgeGroupName
                });
            }
        }
        finally
        {
            IsLoading = false;
        }
        return Task.CompletedTask;
    }

    [RelayCommand]
    public async Task SetSortModeAsync(int mode)
    {
        SortModeIndex = mode;
        SortModeText = mode switch
        {
            1 => "XP'ye göre",
            2 => "Seriye göre",
            _ => "Yıldıza göre"
        };
        await LoadAsync();
    }

    [RelayCommand]
    public Task GoBackAsync() => _navigationService.GoBackAsync();
}
