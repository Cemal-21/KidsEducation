using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KidsEducation.Enums;
using KidsEducation.Models;
using KidsEducation.Services;
using System.Collections.ObjectModel;

namespace KidsEducation.ViewModels.Game;

public enum MemoryV2CardType { Image, Name }

public partial class MemoryV2Card : ObservableObject
{
    public string ItemId { get; set; } = string.Empty;
    public MemoryV2CardType CardType { get; set; }
    public string ImagePath { get; set; } = string.Empty;
    public string NameTr { get; set; } = string.Empty;

    public bool IsImageCard => CardType == MemoryV2CardType.Image;
    public bool IsNameCard  => CardType == MemoryV2CardType.Name;

    [ObservableProperty] private bool _isFlipped;
    [ObservableProperty] private bool _isMatched;
    [ObservableProperty] private string _borderColor = "#E7EBF5";
    [ObservableProperty] private string _backgroundColor = "#FFFFFF";

    partial void OnIsMatchedChanged(bool value)
    {
        if (value)
        {
            BorderColor = "#28B87A";
            BackgroundColor = "#E6FFF5";
        }
    }

    public void SetSelected(bool selected)
    {
        if (IsMatched) return;
        BorderColor = selected ? "#5148D4" : "#E7EBF5";
        BackgroundColor = selected ? "#EEF2FF" : "#FFFFFF";
    }

    public void SetWrong()
    {
        BorderColor = "#FF5C5C";
        BackgroundColor = "#FFF0F0";
    }

    public void Reset()
    {
        IsFlipped = false;
        BorderColor = "#E7EBF5";
        BackgroundColor = "#FFFFFF";
    }
}

public partial class MemoryGameV2ViewModel : ObservableObject
{
    private const int PairCount = 9; // 9 pairs = 18 cards for a 3x6 grid
    private readonly ContentService _contentService;
    private readonly ProfileService _profileService;
    private readonly NavigationService _navigationService;
    private readonly AudioService _audioService;

    [ObservableProperty] private string _categoryId = string.Empty;
    [ObservableProperty] private bool _isLoading = true;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private int _moves;
    [ObservableProperty] private int _matchedPairs;
    [ObservableProperty] private bool _showFeedback;
    [ObservableProperty] private string _feedbackEmoji = "⭐";

    public int TotalPairs => PairCount;
    public double Progress => TotalPairs == 0 ? 0 : (double)MatchedPairs / TotalPairs;
    public string ProgressText => $"{MatchedPairs}/{TotalPairs} eşleşti";

    public ObservableCollection<MemoryV2Card> Cards { get; } = new();

    private MemoryV2Card? _firstSelected;

    public MemoryGameV2ViewModel(
        ContentService contentService,
        ProfileService profileService,
        NavigationService navigationService,
        AudioService audioService)
    {
        _contentService = contentService;
        _profileService = profileService;
        _navigationService = navigationService;
        _audioService = audioService;
    }

    [RelayCommand]
    public async Task InitializeAsync(string? categoryId = null)
    {
        if (!string.IsNullOrWhiteSpace(categoryId))
            CategoryId = categoryId;

        IsLoading = true;
        _firstSelected = null;
        Moves = 0;
        MatchedPairs = 0;
        Cards.Clear();

        try
        {
            var profile = _profileService.GetActiveProfile();
            if (profile is null) return;

            List<LearningItem> items;
            if (string.IsNullOrWhiteSpace(CategoryId) || CategoryId == "mixed")
                items = await _contentService.GetMixedGameItemsAsync(profile, PairCount);
            else
                items = await _contentService.GetGameItemsAsync(CategoryId, PairCount);

            var allCards = new List<MemoryV2Card>();
            foreach (var item in items.Take(PairCount))
            {
                allCards.Add(new MemoryV2Card
                {
                    ItemId = item.Id,
                    CardType = MemoryV2CardType.Image,
                    ImagePath = item.ImagePath,
                    NameTr = item.NameTr
                });
                allCards.Add(new MemoryV2Card
                {
                    ItemId = item.Id,
                    CardType = MemoryV2CardType.Name,
                    ImagePath = item.ImagePath,
                    NameTr = item.NameTr
                });
            }

            foreach (var card in allCards.OrderBy(_ => Guid.NewGuid()))
                Cards.Add(card);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task FlipCardAsync(MemoryV2Card card)
    {
        if (IsBusy || card.IsMatched || card.IsFlipped) return;

        card.IsFlipped = true;
        card.SetSelected(true);
        await _audioService.PlayClickAsync();

        if (_firstSelected is null)
        {
            _firstSelected = card;
            return;
        }

        var second = card;
        var first = _firstSelected;
        _firstSelected = null;
        IsBusy = true;
        Moves++;

        if (first.ItemId == second.ItemId && first.CardType != second.CardType)
        {
            // Match
            await Task.Delay(300);
            first.IsMatched = true;
            second.IsMatched = true;
            MatchedPairs++;
            OnPropertyChanged(nameof(Progress));
            OnPropertyChanged(nameof(ProgressText));
            await _audioService.PlayCorrectAsync();

            if (MatchedPairs == TotalPairs)
                await FinishAsync();
        }
        else
        {
            // No match
            second.SetWrong();
            first.SetWrong();
            await _audioService.PlayWrongAsync();
            await Task.Delay(700);
            first.Reset();
            second.Reset();
        }

        IsBusy = false;
    }

    [RelayCommand]
    public async Task RestartAsync() => await InitializeAsync(CategoryId);

    [RelayCommand]
    public Task GoBackAsync() => _navigationService.GoBackAsync();

    private async Task FinishAsync()
    {
        FeedbackEmoji = "🎉";
        ShowFeedback = true;
        await _audioService.PlayCompleteAsync();
        await Task.Delay(1800);
        ShowFeedback = false;

        var profile = _profileService.GetActiveProfile();
        if (profile is null) return;

        var effectiveCat = string.IsNullOrWhiteSpace(CategoryId) ? "mixed" : CategoryId;
        var stars = Moves <= TotalPairs + 2 ? 3 : Moves <= TotalPairs * 2 ? 2 : 1;

        var correctItem = new LearningItem { Id = "memory_v2", NameTr = "Hafıza" };
        var rounds = Enumerable.Range(0, TotalPairs).Select(_ => new GameRound
        {
            CorrectItem = correctItem,
            SelectedItemId = correctItem.Id,
            Result = GameResult.Correct
        }).ToList();

        for (int i = 0; i < (3 - stars); i++)
            rounds.Add(new GameRound { CorrectItem = correctItem, SelectedItemId = null, Result = GameResult.Wrong });

        var session = new GameSession
        {
            CategoryId = effectiveCat,
            GameType = GameType.MemoryMatch,
            Rounds = rounds,
            FinishedAt = DateTime.UtcNow
        };

        SessionStore.Current = session;
        _profileService.UpdateProgress(profile.Id, effectiveCat, session);
        await _navigationService.GoToResultAsync(session);
    }
}
