using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KidsEducation.Enums;
using KidsEducation.Models;
using KidsEducation.Services;

namespace KidsEducation.ViewModels.Game;

public partial class StoryGameViewModel : ObservableObject
{
    private readonly ContentService _contentService;
    private readonly GameService _gameService;
    private readonly ProfileService _profileService;
    private readonly NavigationService _navigationService;
    private readonly AudioService _audioService;

    private ChildProfile? _profile;

    [ObservableProperty] private GameSession? _currentSession;
    [ObservableProperty] private GameRound? _currentRound;
    [ObservableProperty] private int _currentRoundIndex;
    [ObservableProperty] private bool _isLoading = true;
    [ObservableProperty] private bool _showFeedback;
    [ObservableProperty] private bool _lastAnswerCorrect;
    [ObservableProperty] private bool _canAnswer = true;

    public int TotalRounds => CurrentSession?.TotalRounds ?? 0;
    public double Progress => TotalRounds == 0 ? 0 : (double)CurrentRoundIndex / TotalRounds;
    public int DisplayRoundNumber => TotalRounds == 0 ? 0 : CurrentRoundIndex + 1;

    public string StoryText
    {
        get
        {
            var item = CurrentRound?.CorrectItem;
            if (item is null) return string.Empty;

            var clue = !string.IsNullOrWhiteSpace(item.SoundClueText)
                ? item.SoundClueText
                : !string.IsNullOrWhiteSpace(item.DescriptionTr)
                    ? item.DescriptionTr
                    : item.FunFact;

            return $"Kucuk bir hikaye: {clue} Sence hangisi?";
        }
    }

    public StoryGameViewModel(
        ContentService contentService,
        GameService gameService,
        ProfileService profileService,
        NavigationService navigationService,
        AudioService audioService)
    {
        _contentService = contentService;
        _gameService = gameService;
        _profileService = profileService;
        _navigationService = navigationService;
        _audioService = audioService;
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        IsLoading = true;
        try
        {
            _profile = _profileService.GetActiveProfile();
            if (_profile is null) return;

            var items = await _contentService.GetMixedGameItemsAsync(_profile, 6, 2);
            CurrentSession = _gameService.CreateSession(
                "mixed",
                GameType.StoryQuiz,
                items,
                optionCount: 4,
                difficultyLevel: 2);

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
        if (!CanAnswer || CurrentSession is null || _profile is null)
            return;

        CanAnswer = false;

        var isCorrect = _gameService.SubmitAnswer(
            CurrentSession,
            CurrentRoundIndex,
            itemId,
            _profile.Id);

        LastAnswerCorrect = isCorrect;
        ShowFeedback = true;

        if (isCorrect)
            await _audioService.PlayCorrectAsync();
        else
            await _audioService.PlayWrongAsync();

        await Task.Delay(isCorrect ? 950 : 1150);
        ShowFeedback = false;

        await NextRoundAsync();
    }

    private async Task NextRoundAsync()
    {
        if (CurrentSession is null) return;

        CurrentRoundIndex++;
        OnPropertyChanged(nameof(Progress));
        OnPropertyChanged(nameof(DisplayRoundNumber));

        if (CurrentRoundIndex >= TotalRounds)
        {
            _gameService.FinishSession(CurrentSession);
            if (_profile is not null)
                _profileService.UpdateProgress(_profile.Id, CurrentSession.CategoryId, CurrentSession);

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
        OnPropertyChanged(nameof(StoryText));
    }

    [RelayCommand]
    public Task GoBackAsync() => _navigationService.GoBackAsync();
}
