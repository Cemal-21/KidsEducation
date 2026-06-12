using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KidsEducation.Enums;
using KidsEducation.Models;
using KidsEducation.Services;

namespace KidsEducation.ViewModels.Game;

[QueryProperty(nameof(CategoryId), "categoryId")]
public partial class BalloonGameViewModel : ObservableObject
{
    private readonly ContentService _contentService;
    private readonly GameService _gameService;
    private readonly ProfileService _profileService;
    private readonly NavigationService _navigationService;
    private readonly AudioService _audioService;
    private readonly AdaptiveDifficultyEngine _adaptiveDifficultyEngine;
    private CancellationTokenSource? _roundTimerCts;

    private ChildProfile? _profile;

    [ObservableProperty] private string _categoryId = string.Empty;
    [ObservableProperty] private GameSession? _currentSession;
    [ObservableProperty] private GameRound? _currentRound;
    [ObservableProperty] private int _currentRoundIndex;
    [ObservableProperty] private bool _isLoading = true;
    [ObservableProperty] private bool _canAnswer = true;
    [ObservableProperty] private bool _showFeedback;
    [ObservableProperty] private bool _lastAnswerCorrect;
    [ObservableProperty] private int _combo;
    [ObservableProperty] private int _bestCombo;
    [ObservableProperty] private int _timeLeft = RoundSeconds;

    private const int RoundSeconds = 7;

    public ObservableCollection<BalloonOption> Balloons { get; } = new();

    public int TotalRounds => CurrentSession?.TotalRounds ?? 0;
    public int DisplayRoundNumber => TotalRounds == 0 ? 0 : CurrentRoundIndex + 1;
    public double Progress => TotalRounds == 0 ? 0 : (double)CurrentRoundIndex / TotalRounds;
    public double TimeProgress => Math.Clamp((double)TimeLeft / RoundSeconds, 0, 1);
    public string TargetText => CurrentRound?.CorrectItem?.NameTr ?? string.Empty;
    public string ComboText => Combo > 1 ? $"x{Combo} combo" : "combo hazır";
    public string FeedbackText => LastAnswerCorrect ? "Patladı!" : "Bir daha dene!";

    public BalloonGameViewModel(
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
            StopTimer();
            Combo = 0;
            BestCombo = 0;

            _profile = _profileService.GetActiveProfile();
            if (_profile is null)
                return;

            var effectiveCategoryId = string.IsNullOrWhiteSpace(CategoryId) ? "mixed" : CategoryId;

            var baseDifficulty = _profile.AgeGroup switch
            {
                AgeGroup.Toddler => 1,
                AgeGroup.Explorer => 2,
                AgeGroup.Adventurer => 3,
                _ => 1
            };

            var progress = _profile.CategoryProgresses.TryGetValue(effectiveCategoryId, out var p) ? p : null;
            var currentDifficulty = progress?.CurrentDifficulty > 0 ? progress.CurrentDifficulty : baseDifficulty;

            var nextDifficulty = await _adaptiveDifficultyEngine.GetNextDifficultyAsync(
                _profile.Id, effectiveCategoryId, currentDifficulty);

            var items = string.IsNullOrWhiteSpace(CategoryId)
                ? await _contentService.GetMixedGameItemsAsync(_profile, 8, nextDifficulty)
                : await _contentService.GetGameItemsAsync(CategoryId, 8, nextDifficulty);

            CurrentSession = _gameService.CreateSession(
                effectiveCategoryId,
                GameType.BalloonPop,
                items,
                optionCount: 4,
                difficultyLevel: nextDifficulty);

            CurrentRoundIndex = 0;
            OnPropertyChanged(nameof(TotalRounds));
            LoadCurrentRound();
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task PopBalloonAsync(BalloonOption balloon)
    {
        if (!CanAnswer || CurrentSession is null || balloon.IsPopped || _profile is null)
            return;

        CanAnswer = false;

        var isCorrect = _gameService.SubmitAnswer(
            CurrentSession,
            CurrentRoundIndex,
            balloon.ItemId,
            _profile.Id);

        LastAnswerCorrect = isCorrect;
        ShowFeedback = true;

        if (isCorrect)
        {
            balloon.IsPopped = true;
            Combo++;
            BestCombo = Math.Max(BestCombo, Combo);
            await _audioService.PlayCorrectAsync();
        }
        else
        {
            balloon.IsWrong = true;
            Combo = 0;
            await _audioService.PlayWrongAsync();
        }

        OnPropertyChanged(nameof(ComboText));

        await Task.Delay(isCorrect ? 620 : 760);
        ShowFeedback = false;

        await NextRoundAsync();
    }

    private async Task NextRoundAsync()
    {
        if (CurrentSession is null)
            return;

        StopTimer();
        CurrentRoundIndex++;
        OnPropertyChanged(nameof(DisplayRoundNumber));
        OnPropertyChanged(nameof(Progress));

        if (CurrentRoundIndex >= TotalRounds)
        {
            if (string.IsNullOrWhiteSpace(CurrentSession.CategoryId))
                CurrentSession.CategoryId = "mixed";

            _gameService.FinishSession(CurrentSession);

            var profile = _profileService.GetActiveProfile();
            if (profile is not null)
                _profileService.UpdateProgress(profile.Id, CurrentSession.CategoryId, CurrentSession);

            await _audioService.PlayCompleteAsync();
            await _navigationService.GoToResultAsync(CurrentSession);
            return;
        }

        LoadCurrentRound();
    }

    private void LoadCurrentRound()
    {
        if (CurrentSession is null)
            return;

        CurrentRound = CurrentSession.Rounds[CurrentRoundIndex];
        OnPropertyChanged(nameof(TargetText));

        BuildBalloons();
        CanAnswer = true;
        TimeLeft = RoundSeconds;
        StartTimer();
    }

    private void BuildBalloons()
    {
        Balloons.Clear();

        if (CurrentRound is null)
            return;

        var colors = new[]
        {
            ("#FFF2F6", "#FFD7E2"),
            ("#F0FFFA", "#C8F4E8"),
            ("#F5F1FF", "#DDD6FF"),
            ("#FFF4D8", "#FFE3A3")
        };

        var positions = new[]
        {
            (0, 0),
            (0, 1),
            (1, 0),
            (1, 1)
        };

        for (var i = 0; i < CurrentRound.Options.Count; i++)
        {
            var item = CurrentRound.Options[i];
            var color = colors[i % colors.Length];
            var position = positions[i % positions.Length];

            Balloons.Add(new BalloonOption
            {
                ItemId = item.Id,
                NameTr = item.NameTr,
                ImagePath = item.ImagePath,
                BackgroundColor = color.Item1,
                StrokeColor = color.Item2,
                Row = position.Item1,
                Column = position.Item2
            });
        }
    }

    private void StartTimer()
    {
        StopTimer();
        _roundTimerCts = new CancellationTokenSource();
        _ = RunTimerAsync(_roundTimerCts.Token);
    }

    private async Task RunTimerAsync(CancellationToken token)
    {
        try
        {
            while (TimeLeft > 0 && !token.IsCancellationRequested)
            {
                await Task.Delay(1000, token);
                if (token.IsCancellationRequested)
                    return;

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    TimeLeft--;
                    OnPropertyChanged(nameof(TimeProgress));
                });
            }

            if (!token.IsCancellationRequested && CanAnswer)
                await MainThread.InvokeOnMainThreadAsync(HandleTimeoutAsync);
        }
        catch (TaskCanceledException)
        {
        }
    }

    private async Task HandleTimeoutAsync()
    {
        if (CurrentSession is null || CurrentRound is null || _profile is null)
            return;

        CanAnswer = false;
        Combo = 0;
        LastAnswerCorrect = false;
        ShowFeedback = true;
        _gameService.SubmitAnswer(CurrentSession, CurrentRoundIndex, string.Empty, _profile.Id);
        await _audioService.PlayWrongAsync();
        await Task.Delay(650);
        ShowFeedback = false;
        await NextRoundAsync();
    }

    private void StopTimer()
    {
        _roundTimerCts?.Cancel();
        _roundTimerCts?.Dispose();
        _roundTimerCts = null;
    }

    partial void OnTimeLeftChanged(int value) => OnPropertyChanged(nameof(TimeProgress));

    partial void OnComboChanged(int value) => OnPropertyChanged(nameof(ComboText));

    [RelayCommand]
    public Task GoBackAsync()
    {
        StopTimer();
        return _navigationService.GoBackAsync();
    }

    public void StopGame() => StopTimer();
}
