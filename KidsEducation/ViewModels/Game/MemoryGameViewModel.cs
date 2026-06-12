using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KidsEducation.Enums;
using KidsEducation.Models;
using KidsEducation.Services;
using System.Collections.ObjectModel;

namespace KidsEducation.ViewModels.Game;

[QueryProperty(nameof(CategoryId), "categoryId")]
public partial class MemoryGameViewModel : ObservableObject
{
    private readonly ContentService _contentService;
    private readonly GameService _gameService;
    private readonly ProfileService _profileService;
    private readonly NavigationService _navigationService;
    private readonly AudioService _audioService;

    [ObservableProperty] private string _categoryId = string.Empty;
    [ObservableProperty] private MemorySession? _currentSession;
    [ObservableProperty] private bool _isLoading = true;
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(FlipCardCommand))]
    private bool _isBusy; // ikinci kart açılınca input kilidi

    public ObservableCollection<MemoryCard> Cards { get; } = new();

    public int Moves => CurrentSession?.Moves ?? 0;
    public int MatchedPairs => CurrentSession?.MatchedPairs ?? 0;
    public int TotalPairs => CurrentSession?.TotalPairs ?? 0;
    public double Progress => TotalPairs == 0 ? 0 : (double)MatchedPairs / TotalPairs;

    private MemoryCard? _firstFlipped;

    public MemoryGameViewModel(
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
            var profile = _profileService.GetActiveProfile();
            if (profile is null) return;

            var effectiveCategoryId = string.IsNullOrWhiteSpace(CategoryId) ? "mixed" : CategoryId;
            var baseDifficulty = profile.AgeGroup switch
            {
                AgeGroup.Toddler => 1,
                AgeGroup.Explorer => 2,
                AgeGroup.Adventurer => 3,
                _ => 1
            };

            var progress = profile.CategoryProgresses.TryGetValue(effectiveCategoryId, out var p) ? p : null;
            var currentDifficulty = progress?.CurrentDifficulty > 0 ? progress.CurrentDifficulty : baseDifficulty;

            var items = string.IsNullOrWhiteSpace(CategoryId)
                ? await _contentService.GetMixedGameItemsAsync(profile, 6, currentDifficulty)
                : await _contentService.GetGameItemsAsync(CategoryId, 6, currentDifficulty);

            CurrentSession = _gameService.CreateMemorySession(effectiveCategoryId, items, pairCount: 6, currentDifficulty);

            Cards.Clear();
            foreach (var card in CurrentSession.Cards)
                Cards.Add(card);

            OnPropertyChanged(nameof(Moves));
            OnPropertyChanged(nameof(MatchedPairs));
            OnPropertyChanged(nameof(TotalPairs));
            OnPropertyChanged(nameof(Progress));
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanFlipCard))]
    public async Task FlipCardAsync(MemoryCard card)
    {
        var session = CurrentSession;
        if (session is null)
            return;

        IsBusy = true;  // ── Her durumda hemen kilitle

        card.IsFlipped = true;
        await _audioService.PlayClickAsync();

        if (_firstFlipped is null)
        {
            _firstFlipped = card;
            IsBusy = false;  // ── İlk kart bitti, kilidi aç
            return;
        }

        // İkinci kart — kilit zaten true
        var firstCard = _firstFlipped;
        var isMatch = _gameService.CheckMemoryMatch(session, firstCard.Id, card.Id);
        OnPropertyChanged(nameof(Moves));

        if (isMatch)
        {
            await _audioService.PlayCorrectAsync();
            OnPropertyChanged(nameof(MatchedPairs));
            OnPropertyChanged(nameof(Progress));
            _firstFlipped = null;
            IsBusy = false;

            if (session.IsComplete)
                await FinishGameAsync();
        }
        else
        {
            await _audioService.PlayWrongAsync();
            await Task.Delay(900);

            firstCard.IsFlipped = false;
            card.IsFlipped = false;

            _firstFlipped = null;
            IsBusy = false;
        }
    }

    private bool CanFlipCard(MemoryCard? card) =>
        CurrentSession is not null &&
        !IsBusy &&
        card is not null &&
        !card.IsFlipped &&
        !card.IsMatched;

    private async Task FinishGameAsync()
    {
        if (CurrentSession is null) return;

        if (string.IsNullOrWhiteSpace(CurrentSession.CategoryId))
            CurrentSession.CategoryId = "mixed";

        _gameService.FinishMemorySession(CurrentSession);

        var gameSession = _gameService.ToGameSession(CurrentSession);
        _gameService.FinishSession(gameSession);

        var profile = _profileService.GetActiveProfile();
        if (profile is not null)
            _profileService.UpdateProgress(profile.Id, CurrentSession.CategoryId, gameSession);

        await _audioService.PlayCompleteAsync();
        await _navigationService.GoToResultAsync(gameSession);
    }

    [RelayCommand]
    public Task GoBackAsync() => _navigationService.GoBackAsync();
}
