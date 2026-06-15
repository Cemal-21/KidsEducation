using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KidsEducation.Enums;
using KidsEducation.Models;
using KidsEducation.Services;
using System.Collections.ObjectModel;

namespace KidsEducation.ViewModels.Game;

public partial class SortingCard : ObservableObject
{
    public string ItemId { get; set; } = string.Empty;
    public string NameTr { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty;
    public int CorrectPosition { get; set; } // 0-indexed correct order
    public int? TappedAt { get; set; }      // which tap number placed this card

    [ObservableProperty] private bool _isPlaced;
    [ObservableProperty] private string _borderColor = "#E7EBF5";
    [ObservableProperty] private string _backgroundColor = "#FFFFFF";

    public void SetPlaced(bool correct)
    {
        IsPlaced = true;
        BorderColor = correct ? "#28B87A" : "#FF5C5C";
        BackgroundColor = correct ? "#E6FFF5" : "#FFF0F0";
    }

    public void SetWrong()
    {
        BorderColor = "#FF5C5C";
        BackgroundColor = "#FFF0F0";
    }

    public void Reset()
    {
        BorderColor = "#E7EBF5";
        BackgroundColor = "#FFFFFF";
    }
}

public partial class SortingGameViewModel : ObservableObject
{
    private const int ItemsPerRound = 5;
    private const int TotalRounds = 5;

    private readonly ContentService _contentService;
    private readonly ProfileService _profileService;
    private readonly NavigationService _navigationService;
    private readonly AudioService _audioService;

    [ObservableProperty] private string _categoryId = "numbers";
    [ObservableProperty] private bool _isLoading = true;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private int _currentRound = 1;
    [ObservableProperty] private int _correctCount;
    [ObservableProperty] private string _taskLabel = "Küçükten büyüğe sırala!";
    [ObservableProperty] private bool _showFeedback;
    [ObservableProperty] private string _feedbackEmoji = "⭐";
    [ObservableProperty] private string _feedbackText = "";

    public int TotalRoundsCount => TotalRounds;
    public double Progress => (double)(CurrentRound - 1) / TotalRounds;

    public ObservableCollection<SortingCard> Cards { get; } = new();
    public ObservableCollection<SortingCard> PlacedCards { get; } = new();

    private List<LearningItem> _allItems = new();
    private int _nextTapIndex;
    private readonly List<GameRound> _rounds = new();

    public SortingGameViewModel(
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
        CurrentRound = 1;
        CorrectCount = 0;
        _rounds.Clear();

        try
        {
            _allItems = await _contentService.GetItemsAsync(CategoryId);
            TaskLabel = GetTaskLabel(CategoryId);
            await LoadRoundAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadRoundAsync()
    {
        _nextTapIndex = 0;
        Cards.Clear();
        PlacedCards.Clear();
        IsBusy = false;

        if (_allItems.Count < ItemsPerRound)
            return;

        // Pick 5 consecutive items shifted by round (cycle if needed)
        var startIndex = ((CurrentRound - 1) * ItemsPerRound) % _allItems.Count;
        var selectedItems = new List<LearningItem>();
        for (int i = 0; i < ItemsPerRound; i++)
            selectedItems.Add(_allItems[(startIndex + i) % _allItems.Count]);

        // Shuffle for display — correct position = original index 0..4
        var shuffled = selectedItems
            .Select((item, idx) => new SortingCard
            {
                ItemId = item.Id,
                NameTr = item.NameTr,
                ImagePath = item.ImagePath,
                CorrectPosition = idx
            })
            .OrderBy(_ => Guid.NewGuid())
            .ToList();

        foreach (var card in shuffled)
            Cards.Add(card);

        // Reserve slots
        for (int i = 0; i < ItemsPerRound; i++)
            PlacedCards.Add(new SortingCard { BackgroundColor = "#F0F4FF" });

        await _audioService.PlayClickAsync();
    }

    [RelayCommand]
    public async Task TapCardAsync(SortingCard card)
    {
        if (IsBusy || card.IsPlaced) return;

        bool correct = card.CorrectPosition == _nextTapIndex;

        if (correct)
        {
            card.SetPlaced(true);
            card.TappedAt = _nextTapIndex;

            // Fill the slot
            PlacedCards[_nextTapIndex] = new SortingCard
            {
                ItemId = card.ItemId,
                NameTr = card.NameTr,
                ImagePath = card.ImagePath,
                IsPlaced = true,
                BorderColor = "#28B87A",
                BackgroundColor = "#E6FFF5"
            };
            OnPropertyChanged(nameof(PlacedCards));

            _nextTapIndex++;
            await _audioService.PlayCorrectAsync();

            if (_nextTapIndex == ItemsPerRound)
            {
                CorrectCount++;
                _rounds.Add(new GameRound { CorrectItem = new LearningItem { Id = card.ItemId, NameTr = card.NameTr }, SelectedItemId = card.ItemId, Result = GameResult.Correct });
                await ShowFeedbackAsync("🎉", "Mükemmel sıralama!");
                await AdvanceRoundAsync();
            }
        }
        else
        {
            card.SetWrong();
            await _audioService.PlayWrongAsync();
            await Task.Delay(500);
            card.Reset();
        }
    }

    [RelayCommand]
    public async Task RestartAsync() => await InitializeAsync(CategoryId);

    [RelayCommand]
    public Task GoBackAsync() => _navigationService.GoBackAsync();

    private async Task AdvanceRoundAsync()
    {
        if (CurrentRound >= TotalRounds)
        {
            await FinishAsync();
            return;
        }

        CurrentRound++;
        OnPropertyChanged(nameof(Progress));
        await Task.Delay(600);
        await LoadRoundAsync();
    }

    private async Task ShowFeedbackAsync(string emoji, string text)
    {
        FeedbackEmoji = emoji;
        FeedbackText = text;
        ShowFeedback = true;
        await Task.Delay(1000);
        ShowFeedback = false;
    }

    private async Task FinishAsync()
    {
        var profile = _profileService.GetActiveProfile();
        if (profile is null) return;

        var placeholder = new LearningItem { Id = "sorting_wrong", NameTr = "Yanlış" };
        while (_rounds.Count < TotalRounds)
            _rounds.Add(new GameRound { CorrectItem = placeholder, SelectedItemId = null, Result = GameResult.Wrong });

        var session = new GameSession
        {
            CategoryId = CategoryId,
            GameType = GameType.Sorting,
            Rounds = _rounds,
            FinishedAt = DateTime.UtcNow
        };

        SessionStore.Current = session;
        _profileService.UpdateProgress(profile.Id, CategoryId, session);
        await _navigationService.GoToResultAsync(session);
    }

    private static string GetTaskLabel(string categoryId) => categoryId switch
    {
        "numbers"  => "🔢 Küçükten büyüğe sırala!",
        "letters"  => "🔤 Alfabetik sıraya diz!",
        "seasons"  => "🌸 Mevsim sırasına göre diz!",
        _          => "✅ Doğru sıraya diz!"
    };
}
