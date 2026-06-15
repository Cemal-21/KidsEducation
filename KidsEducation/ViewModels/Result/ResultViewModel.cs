using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KidsEducation.Enums;
using KidsEducation.Models;
using KidsEducation.Services;

namespace KidsEducation.ViewModels.Result;

public partial class ResultViewModel : ObservableObject
{
    private readonly NavigationService _navigationService;
    private readonly AudioService _audioService;
    private readonly ProfileService _profileService;
    private readonly DailyChallengeService _dailyChallengeService;
    private readonly ContentService _contentService;
    private readonly ModuleProgressService _moduleProgressService;
    private readonly ShareService _shareService;

    [ObservableProperty] private GameSession? _session;
    [ObservableProperty] private DailyGoalInfo _dailyGoal = new();
    [ObservableProperty] private bool _showCertificate;
    [ObservableProperty] private bool _showModuleCertificate;
    [ObservableProperty] private string _certificateCategoryName = "";
    [ObservableProperty] private string _certificateProfileName = "";
    [ObservableProperty] private string _moduleCertificateText = "";
    [ObservableProperty] private string _levelText = "";
    [ObservableProperty] private string _levelTitle = "";
    [ObservableProperty] private string _levelProgressText = "";
    [ObservableProperty] private double _levelProgress;

    public int Stars => Session?.Stars ?? 0;
    public int Score => Session?.Score ?? 0;
    public int CorrectCount => Session?.CorrectCount ?? 0;
    public int WrongCount => Session?.WrongCount ?? 0;
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
        ProfileService profileService,
        DailyChallengeService dailyChallengeService,
        ContentService contentService,
        ModuleProgressService moduleProgressService,
        ShareService shareService)
    {
        _navigationService = navigationService;
        _audioService = audioService;
        _profileService = profileService;
        _dailyChallengeService = dailyChallengeService;
        _contentService = contentService;
        _moduleProgressService = moduleProgressService;
        _shareService = shareService;
    }

    public async Task LoadSessionAsync()
    {
        Session = SessionStore.Current;
        OnPropertyChanged(nameof(Stars));
        OnPropertyChanged(nameof(Score));
        OnPropertyChanged(nameof(CorrectCount));
        OnPropertyChanged(nameof(WrongCount));
        OnPropertyChanged(nameof(TotalRounds));
        OnPropertyChanged(nameof(StarsText));
        OnPropertyChanged(nameof(StarsEmoji));
        OnPropertyChanged(nameof(CategoryId));

        var profile = _profileService.GetActiveProfile();
        CompleteDailyChallengeIfNeeded();
        if (profile is not null)
        {
            DailyGoal = _profileService.GetDailyGoal(profile);
            LevelText = profile.LevelText;
            LevelTitle = profile.LevelTitle;
            LevelProgressText = profile.LevelProgressText;
            LevelProgress = profile.LevelProgress;
            await LoadModuleCertificateAsync(profile);

            // Widget verilerini güncelle
            AppPreferencesService.SaveStreakForWidget(profile.StreakDays);
            AppPreferencesService.SaveDailyGoalForWidget(DailyGoal.CompletedCount, DailyGoal.TotalCount);
        }

        // ── Yıldız kazanıldıysa ses çal ─────────────────────
        if (Stars > 0)
        {
            HapticService.Heavy();
            await _audioService.PlayStarAsync();
        }

        // ── 3 yıldız = sertifika göster ──────────────────────
        if (Stars == 3 && profile is not null)
        {
            CertificateProfileName = profile.Name;
            CertificateCategoryName = await GetCategoryNameAsync(CategoryId);
            ShowCertificate = true;
        }
    }

    private async Task LoadModuleCertificateAsync(ChildProfile profile)
    {
        var categories = await _contentService.GetCategoriesAsync(profile);
        var currentModule = ModuleProgressService.GetModuleForCategory(CategoryId);
        var module = _moduleProgressService
            .BuildModuleProgress(categories, profile)
            .FirstOrDefault(m => m.Id == currentModule.Id);

        ShowModuleCertificate = module?.IsCompleted == true;
        ModuleCertificateText = module is null
            ? string.Empty
            : module.IsCompleted
                ? $"{module.Name} modülü tamamlandı"
                : $"{module.Name}: {module.ProgressText} durak tamam";
    }

    private async Task<string> GetCategoryNameAsync(string categoryId)
    {
        // CategoryId genellikle "animals", "colors" gibi — Türkçe karşılıklarını eşleştir
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["animals"] = "Hayvanlar", ["colors"] = "Renkler", ["fruits"] = "Meyveler",
            ["vegetables"] = "Sebzeler", ["vehicles"] = "Taşıtlar", ["shapes"] = "Şekiller",
            ["numbers"] = "Sayılar", ["letters"] = "Harfler", ["body"] = "Vücudum",
            ["seasons"] = "Mevsimler", ["emotions"] = "Duygular", ["professions"] = "Meslekler",
            ["countries"] = "Ülkeler", ["planets"] = "Gezegenler", ["cities"] = "İller",
            ["nature"] = "Doğa", ["weather"] = "Hava Durumu"
        };
        return map.TryGetValue(categoryId, out var name) ? name : categoryId;
    }

    private void CompleteDailyChallengeIfNeeded()
    {
        if (Session is null)
            return;

        var challenge = _dailyChallengeService.GetTodayChallenge();
        if (challenge.IsCompleted)
            return;

        if (string.Equals(challenge.CategoryId, Session.CategoryId, StringComparison.OrdinalIgnoreCase) &&
            challenge.GameType == Session.GameType)
        {
            _dailyChallengeService.MarkCompleted(challenge.DateKey);
        }
    }

    // Eski senkron versiyon — geriye dönük uyumluluk için
    public void LoadSession() => _ = LoadSessionAsync();

    [RelayCommand]
    public Task PlayAgainAsync()
    {
        var gameType = Session?.GameType ?? GameType.MatchName;
        var categoryQuery = string.IsNullOrWhiteSpace(CategoryId) || CategoryId == "mixed"
            ? string.Empty
            : $"?categoryId={Uri.EscapeDataString(CategoryId)}";

        return gameType switch
        {
            GameType.MemoryMatch => Shell.Current.GoToAsync($"memorygamev2{categoryQuery}"),
            GameType.ZoomGuess => Shell.Current.GoToAsync($"zoomgame{categoryQuery}"),
            GameType.SoundGuess => Shell.Current.GoToAsync($"soundgame{categoryQuery}"),
            GameType.BalloonPop => Shell.Current.GoToAsync($"balloongame{categoryQuery}"),
            GameType.SequenceOrder => Shell.Current.GoToAsync($"sequencegame{categoryQuery}"),
            GameType.StoryQuiz => Shell.Current.GoToAsync($"storygame{categoryQuery}"),
            GameType.Tracing => Shell.Current.GoToAsync($"tracinggame{categoryQuery}"),
            GameType.PuzzleSwap => Shell.Current.GoToAsync($"puzzlegame{categoryQuery}"),
            GameType.LetterDrop => Shell.Current.GoToAsync($"letterdrop{categoryQuery}"),
            GameType.MathQuiz => Shell.Current.GoToAsync("mathgame"),
            GameType.WordScramble => Shell.Current.GoToAsync($"wordscramble{categoryQuery}"),
            GameType.Matching => Shell.Current.GoToAsync($"matchinggame{categoryQuery}"),
            GameType.FindAndMark => Shell.Current.GoToAsync($"findmarkgame{categoryQuery}"),
            GameType.Coloring => Shell.Current.GoToAsync("coloringgame"),
            GameType.Sorting => Shell.Current.GoToAsync($"sortinggame{categoryQuery}"),
            _ => _navigationService.GoToGameAsync(CategoryId, gameType)
        };
    }

    [RelayCommand]
    public Task GoHomeAsync() =>
        _navigationService.GoToHomeAsync();

    [RelayCommand]
    public async Task ShareResultAsync()
    {
        var profile = _profileService.GetActiveProfile();
        var profileName = profile?.Name ?? "Kahraman";
        var categoryName = await GetCategoryNameAsync(CategoryId);
        await _shareService.ShareGameResultAsync(
            profileName, categoryName, Stars, Score, CorrectCount, TotalRounds);
    }
}
