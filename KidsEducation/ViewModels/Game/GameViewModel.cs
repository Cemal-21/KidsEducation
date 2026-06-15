using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KidsEducation.Enums;
using KidsEducation.Models;
using KidsEducation.Services;

namespace KidsEducation.ViewModels.Game;

[QueryProperty(nameof(CategoryId), "categoryId")]
[QueryProperty(nameof(GameTypeName), "gameType")]
[QueryProperty(nameof(TimedModeParam), "timed")]
public partial class GameViewModel : ObservableObject
{
    private readonly ContentService _contentService;
    private readonly GameService _gameService;
    private readonly ProfileService _profileService;
    private readonly NavigationService _navigationService;
    private readonly AudioService _audioService;
    private readonly AdaptiveDifficultyEngine _adaptiveDifficultyEngine;

    private ChildProfile? _profile;
    private CancellationTokenSource? _timerCts;
    private const int TimerSeconds = 15;

    [ObservableProperty] private string _categoryId = string.Empty;
    [ObservableProperty] private string _gameTypeName = string.Empty;
    [ObservableProperty] private string _timedModeParam = string.Empty;
    [ObservableProperty] private GameSession? _currentSession;
    [ObservableProperty] private GameRound? _currentRound;
    [ObservableProperty] private int _currentRoundIndex;
    [ObservableProperty] private bool _isLoading = true;
    [ObservableProperty] private bool _showFeedback;
    [ObservableProperty] private bool _lastAnswerCorrect;
    [ObservableProperty] private bool _canAnswer = true;
    [ObservableProperty] private string? _selectedOptionId;

    // ── Zaman yarışı modu ─────────────────────────────────────
    [ObservableProperty] private bool _timerEnabled;
    [ObservableProperty] private int _timeLeft = TimerSeconds;
    [ObservableProperty] private double _timerProgress = 1.0;
    [ObservableProperty] private string _timerColor = "#28B87A";
    [ObservableProperty] private int _speedBonus;

    public int TotalRounds => CurrentSession?.TotalRounds ?? 0;
    public double Progress => TotalRounds == 0 ? 0 : (double)CurrentRoundIndex / TotalRounds;

    public GameViewModel(
        ContentService contentService,
        GameService gameService,
        ProfileService profileService,
        NavigationService navigationService,
        AudioService audioService,
        AdaptiveDifficultyEngine adaptiveDifficultyEngine)
    {
        _contentService = contentService;
        _gameService = gameService;
        _profileService = profileService;
        _navigationService = navigationService;
        _audioService = audioService;
        _adaptiveDifficultyEngine = adaptiveDifficultyEngine;
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        IsLoading = true;
        try
        {
            _profile = _profileService.GetActiveProfile();
            if (_profile is null) return;

            var gameType = Enum.TryParse<GameType>(GameTypeName, out var gt)
                ? gt : GameType.MatchName;

            var baseDifficulty = _profile.AgeGroup switch
            {
                AgeGroup.Toddler => 1,
                AgeGroup.Explorer => 2,
                AgeGroup.Adventurer => 3,
                _ => 1
            };

            var progress = _profile.CategoryProgresses.TryGetValue(CategoryId, out var p) ? p : null;
            var currentDifficulty = progress?.CurrentDifficulty > 0 ? progress.CurrentDifficulty : baseDifficulty;

            var nextDifficulty = await _adaptiveDifficultyEngine.GetNextDifficultyAsync(
                _profile.Id, CategoryId, currentDifficulty);

            var items = await _contentService.GetGameItemsAsync(CategoryId, 8, nextDifficulty);

            CurrentSession = _gameService.CreateSession(
                CategoryId,
                gameType,
                items,
                _profile.OptionCount,
                difficultyLevel: nextDifficulty);

            TimerEnabled = TimedModeParam == "true" || _profile.TimerEnabled;
            SpeedBonus = 0;
            CurrentRoundIndex = 0;
            LoadCurrentRound();
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task SelectOptionAsync(string itemId)
    {
        if (!CanAnswer || CurrentSession is null || _profile is null) return;

        StopTimer();
        CanAnswer = false;
        SelectedOptionId = itemId;

        var isCorrect = _gameService.SubmitAnswer(
            CurrentSession, CurrentRoundIndex, itemId, _profile.Id);

        LastAnswerCorrect = isCorrect;
        ShowFeedback = true;

        if (isCorrect && TimerEnabled)
            SpeedBonus += Math.Max(0, TimeLeft * 10);

        // ── Ses efekti ───────────────────────────────────────
        if (isCorrect)
        {
            HapticService.Success();
            await _audioService.PlayCorrectAsync();
        }
        else
        {
            HapticService.Error();
            await _audioService.PlayWrongAsync();
        }
        // ────────────────────────────────────────────────────

        await Task.Delay(1200);

        ShowFeedback = false;
        SelectedOptionId = null;

        await NextRoundAsync();
    }

    private async Task NextRoundAsync()
    {
        if (CurrentSession is null) return;

        CurrentRoundIndex++;
        OnPropertyChanged(nameof(Progress));

        if (CurrentRoundIndex >= TotalRounds)
        {
            _gameService.FinishSession(CurrentSession);

            var profile = _profileService.GetActiveProfile();
            if (profile is not null)
                _profileService.UpdateProgress(profile.Id, CategoryId, CurrentSession);

            // ── Tamamlama sesi ───────────────────────────────
            await _audioService.PlayCompleteAsync();
            // ────────────────────────────────────────────────

            await _navigationService.GoToResultAsync(CurrentSession);
            return;
        }

        LoadCurrentRound();
        CanAnswer = true;
    }

    private void LoadCurrentRound()
    {
        if (CurrentSession is null) return;
        CurrentRound = CurrentSession.Rounds[CurrentRoundIndex];
        if (TimerEnabled)
            StartTimer();
    }

    // ── Timer mantığı ─────────────────────────────────────────

    private void StartTimer()
    {
        StopTimer();
        TimeLeft = TimerSeconds;
        TimerProgress = 1.0;
        TimerColor = "#28B87A";
        _timerCts = new CancellationTokenSource();
        _ = RunTimerAsync(_timerCts.Token);
    }

    private void StopTimer()
    {
        _timerCts?.Cancel();
        _timerCts = null;
    }

    private async Task RunTimerAsync(CancellationToken ct)
    {
        while (TimeLeft > 0 && !ct.IsCancellationRequested)
        {
            await Task.Delay(1000, ct).ContinueWith(_ => { });
            if (ct.IsCancellationRequested) return;

            TimeLeft--;
            TimerProgress = (double)TimeLeft / TimerSeconds;
            TimerColor = TimeLeft switch
            {
                <= 5  => "#FF5C5C",
                <= 10 => "#FFC857",
                _     => "#28B87A"
            };
        }

        if (!ct.IsCancellationRequested && CanAnswer)
            await SelectOptionAsync("__timeout__");
    }

    [RelayCommand]
    public Task GoBackAsync()
    {
        StopTimer();
        return _navigationService.GoBackAsync();
    }
}
