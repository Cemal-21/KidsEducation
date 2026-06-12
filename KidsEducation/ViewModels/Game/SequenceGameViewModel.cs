using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KidsEducation.Enums;
using KidsEducation.Models;
using KidsEducation.Services;

namespace KidsEducation.ViewModels.Game;

public partial class SequenceGameViewModel : ObservableObject
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
    [ObservableProperty] private string _sequenceText = string.Empty;

    public int TotalRounds => CurrentSession?.TotalRounds ?? 0;
    public double Progress => TotalRounds == 0 ? 0 : (double)CurrentRoundIndex / TotalRounds;
    public int DisplayRoundNumber => TotalRounds == 0 ? 0 : CurrentRoundIndex + 1;
    public string PromptText => CurrentRoundIndex == 0
        ? "Saymaya baslamak icin ilk sayiyi sec"
        : "Siradaki sayiyi sec";

    public SequenceGameViewModel(
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

            var count = _profile.AgeGroup switch
            {
                AgeGroup.Toddler => 4,
                AgeGroup.Explorer => 5,
                AgeGroup.Adventurer => 6,
                _ => 4
            };

            var items = (await _contentService.GetItemsAsync("numbers"))
                .OrderBy(GetNumberOrder)
                .Take(count)
                .ToList();

            CurrentSession = _gameService.CreateSession(
                "numbers",
                GameType.SequenceOrder,
                items,
                optionCount: Math.Min(4, items.Count),
                difficultyLevel: _profile.AgeGroup == AgeGroup.Adventurer ? 3 : 2);

            CurrentRoundIndex = 0;
            SequenceText = string.Empty;
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
        {
            SequenceText = string.IsNullOrWhiteSpace(SequenceText)
                ? CurrentRound?.CorrectItem.NameTr ?? string.Empty
                : $"{SequenceText}  •  {CurrentRound?.CorrectItem.NameTr}";
            await _audioService.PlayCorrectAsync();
        }
        else
        {
            await _audioService.PlayWrongAsync();
        }

        await Task.Delay(isCorrect ? 850 : 950);
        ShowFeedback = false;

        if (!isCorrect)
        {
            CanAnswer = true;
            return;
        }

        await NextRoundAsync();
    }

    private async Task NextRoundAsync()
    {
        if (CurrentSession is null) return;

        CurrentRoundIndex++;
        OnPropertyChanged(nameof(Progress));
        OnPropertyChanged(nameof(DisplayRoundNumber));
        OnPropertyChanged(nameof(PromptText));

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
    }

    [RelayCommand]
    public Task GoBackAsync() => _navigationService.GoBackAsync();

    private static int GetNumberOrder(LearningItem item)
    {
        var lastPart = item.Id.Split('_').LastOrDefault();
        return int.TryParse(lastPart, out var value) ? value : 999;
    }
}
