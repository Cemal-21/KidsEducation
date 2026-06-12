using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KidsEducation.Enums;
using KidsEducation.Models;
using KidsEducation.Services;

namespace KidsEducation.ViewModels.Game;

[QueryProperty(nameof(CategoryId), "categoryId")]
public partial class ZoomGameViewModel : ObservableObject
{
    private readonly ContentService _contentService;
    private readonly GameService _gameService;
    private readonly ProfileService _profileService;
    private readonly NavigationService _navigationService;
    private readonly AudioService _audioService;
    private readonly AdaptiveDifficultyEngine _adaptiveDifficultyEngine;

    private ChildProfile? _profile;

    [ObservableProperty] private string _categoryId = string.Empty;
    [ObservableProperty] private GameSession? _currentSession;
    [ObservableProperty] private GameRound? _currentRound;
    [ObservableProperty] private int _currentRoundIndex;
    [ObservableProperty] private bool _isLoading = true;
    [ObservableProperty] private bool _showFeedback;
    [ObservableProperty] private bool _lastAnswerCorrect;
    [ObservableProperty] private bool _canAnswer = true;
    [ObservableProperty] private string? _selectedOptionId;
    [ObservableProperty] private int _revealStep;

    public int TotalRounds => CurrentSession?.TotalRounds ?? 0;
    public double Progress => TotalRounds == 0 ? 0 : (double)CurrentRoundIndex / TotalRounds;
    public int DisplayRoundNumber => TotalRounds == 0 ? 0 : CurrentRoundIndex + 1;
    public int MaxRevealStep => 2;
    public bool CanRevealMore => RevealStep < MaxRevealStep && CanAnswer;
    public double ZoomScale => RevealStep switch
    {
        0 => 4.8,
        1 => 3.1,
        _ => 1.75
    };

    public double ZoomTranslationX => GetBaseOffset().X * RevealOffsetMultiplier;
    public double ZoomTranslationY => GetBaseOffset().Y * RevealOffsetMultiplier;
    public string RevealLevelText => $"Yakınlık {RevealStep + 1}/3";
    public string RevealButtonText => RevealStep switch
    {
        0 => "Biraz uzaklaş",
        1 => "Daha fazla aç",
        _ => "En açık görünüm"
    };

    public string ZoomHintText => RevealStep switch
    {
        0 => "Çok yakından bakıyorsun. Şekil, renk ve kenarları yakala.",
        1 => CurrentRound?.CorrectItem.FunFact ?? "Bir ipucu daha geldi.",
        _ => CurrentRound?.CorrectItem.DescriptionTr ?? CurrentRound?.CorrectItem.FunFact ?? "Artık neredeyse tamamı görünüyor."
    };

    private double RevealOffsetMultiplier => RevealStep switch
    {
        0 => 1,
        1 => 0.55,
        _ => 0.18
    };

    public ZoomGameViewModel(
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
                GameType.ZoomGuess,
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
    public async Task SelectOptionAsync(string itemId)
    {
        if (!CanAnswer || CurrentSession is null || _profile is null) return;

        CanAnswer = false;
        SelectedOptionId = itemId;

        var isCorrect = _gameService.SubmitAnswer(
            CurrentSession, CurrentRoundIndex, itemId, _profile.Id);

        LastAnswerCorrect = isCorrect;
        ShowFeedback = true;

        if (isCorrect)
        {
            await _audioService.PlayCorrectAsync();
        }
        else
        {
            await _audioService.PlayWrongAsync();
        }

        await Task.Delay(isCorrect ? 1100 : 850);

        ShowFeedback = false;
        SelectedOptionId = null;

        if (!isCorrect && RevealStep < MaxRevealStep)
        {
            RevealStep++;
            CanAnswer = true;
            NotifyZoomStateChanged();
            return;
        }

        await NextRoundAsync();
    }

    [RelayCommand(CanExecute = nameof(CanRevealMore))]
    public async Task RevealMoreAsync()
    {
        if (!CanRevealMore)
            return;

        RevealStep++;
        NotifyZoomStateChanged();
        await _audioService.PlayClickAsync();
    }

    private async Task NextRoundAsync()
    {
        if (CurrentSession is null) return;

        CurrentRoundIndex++;
        OnPropertyChanged(nameof(Progress));
        OnPropertyChanged(nameof(DisplayRoundNumber));

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
        CanAnswer = true;
    }

    private void LoadCurrentRound()
    {
        if (CurrentSession is null) return;
        CurrentRound = CurrentSession.Rounds[CurrentRoundIndex];
        RevealStep = 0;
        NotifyZoomStateChanged();
    }

    [RelayCommand]
    public Task GoBackAsync() => _navigationService.GoBackAsync();

    partial void OnRevealStepChanged(int value) => NotifyZoomStateChanged();

    partial void OnCanAnswerChanged(bool value)
    {
        OnPropertyChanged(nameof(CanRevealMore));
        RevealMoreCommand.NotifyCanExecuteChanged();
    }

    private void NotifyZoomStateChanged()
    {
        OnPropertyChanged(nameof(ZoomScale));
        OnPropertyChanged(nameof(ZoomTranslationX));
        OnPropertyChanged(nameof(ZoomTranslationY));
        OnPropertyChanged(nameof(RevealLevelText));
        OnPropertyChanged(nameof(RevealButtonText));
        OnPropertyChanged(nameof(ZoomHintText));
        OnPropertyChanged(nameof(CanRevealMore));
        RevealMoreCommand.NotifyCanExecuteChanged();
    }

    private (double X, double Y) GetBaseOffset()
    {
        var offsets = new (double X, double Y)[]
        {
            (34, -24),
            (-32, 20),
            (24, 30),
            (-36, -18),
            (30, 8),
            (-24, -32),
            (36, 24),
            (-18, -14)
        };

        return offsets[CurrentRoundIndex % offsets.Length];
    }
}
