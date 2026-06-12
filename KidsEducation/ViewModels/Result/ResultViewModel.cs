using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KidsEducation.Enums;
using KidsEducation.Models;
using KidsEducation.Services;

namespace KidsEducation.ViewModels.Result;

public partial class ResultViewModel : ObservableObject
{
    private readonly NavigationService _navigationService;
    private readonly AudioService _audioService;   // ── YENİ
    private readonly ProfileService _profileService;

    [ObservableProperty] private GameSession? _session;
    [ObservableProperty] private DailyGoalInfo _dailyGoal = new();

    public int Stars => Session?.Stars ?? 0;
    public int Score => Session?.Score ?? 0;
    public int CorrectCount => Session?.CorrectCount ?? 0;
    public int TotalRounds => Session?.TotalRounds ?? 0;
    public string StarsText => Session?.StarsText ?? string.Empty;
    public string CategoryId => Session?.CategoryId ?? string.Empty;

    public string StarsEmoji => Stars switch
    {
        3 => "🌟🌟🌟",
        2 => "⭐⭐",
        1 => "👏",
        _ => "💪"
    };

    public ResultViewModel(
        NavigationService navigationService,
        AudioService audioService,
        ProfileService profileService)          // ── YENİ
    {
        _navigationService = navigationService;
        _audioService = audioService;  // ── YENİ
        _profileService = profileService;
    }

    public async Task LoadSessionAsync()
    {
        Session = SessionStore.Current;
        OnPropertyChanged(nameof(Stars));
        OnPropertyChanged(nameof(Score));
        OnPropertyChanged(nameof(CorrectCount));
        OnPropertyChanged(nameof(TotalRounds));
        OnPropertyChanged(nameof(StarsText));
        OnPropertyChanged(nameof(StarsEmoji));
        OnPropertyChanged(nameof(CategoryId));

        var profile = _profileService.GetActiveProfile();
        if (profile is not null)
            DailyGoal = _profileService.GetDailyGoal(profile);

        // ── Yıldız kazanıldıysa ses çal ─────────────────────
        if (Stars > 0)
            await _audioService.PlayStarAsync();
        // ────────────────────────────────────────────────────
    }

    // Eski senkron versiyon — geriye dönük uyumluluk için
    public void LoadSession() => _ = LoadSessionAsync();

    [RelayCommand]
    public Task PlayAgainAsync()
    {
        var gameType = Session?.GameType ?? GameType.MatchName;

        return gameType switch
        {
            GameType.MemoryMatch => Shell.Current.GoToAsync(string.IsNullOrWhiteSpace(CategoryId) || CategoryId == "mixed"
                ? "memorygame"
                : $"memorygame?categoryId={Uri.EscapeDataString(CategoryId)}"),
            GameType.ZoomGuess => Shell.Current.GoToAsync(string.IsNullOrWhiteSpace(CategoryId) || CategoryId == "mixed"
                ? "zoomgame"
                : $"zoomgame?categoryId={Uri.EscapeDataString(CategoryId)}"),
            GameType.SoundGuess => Shell.Current.GoToAsync(string.IsNullOrWhiteSpace(CategoryId) || CategoryId == "mixed"
                ? "soundgame"
                : $"soundgame?categoryId={Uri.EscapeDataString(CategoryId)}"),
            GameType.BalloonPop => Shell.Current.GoToAsync(string.IsNullOrWhiteSpace(CategoryId) || CategoryId == "mixed"
                ? "balloongame"
                : $"balloongame?categoryId={Uri.EscapeDataString(CategoryId)}"),
            _ => _navigationService.GoToGameAsync(CategoryId, gameType)
        };
    }

    [RelayCommand]
    public Task GoHomeAsync() =>
        _navigationService.GoToHomeAsync();
}
