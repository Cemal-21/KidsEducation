using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KidsEducation.Enums;
using KidsEducation.Models;
using KidsEducation.Services;

namespace KidsEducation.ViewModels.Game;

public partial class WordScrambleGameViewModel : ObservableObject
{
    private readonly ContentService _contentService;
    private readonly ProfileService _profileService;
    private readonly NavigationService _navigationService;
    private readonly AudioService _audioService;

    private List<LearningItem> _items = new();
    private List<GameRound> _rounds = new();
    private ChildProfile? _profile;
    private string _targetWord = "";
    private List<ScrambleLetter> _allLetters = new();

    [ObservableProperty] private string _categoryId = "animals";
    [ObservableProperty] private bool _isLoading = true;
    [ObservableProperty] private int _currentIndex;
    [ObservableProperty] private int _totalRounds;
    [ObservableProperty] private LearningItem? _currentItem;
    [ObservableProperty] private List<ScrambleLetter> _availableLetters = new();
    [ObservableProperty] private List<ScrambleLetter> _placedLetters = new();
    [ObservableProperty] private bool _showCorrectFeedback;
    [ObservableProperty] private bool _showWrongFeedback;
    [ObservableProperty] private bool _isChecking;

    public double Progress => TotalRounds == 0 ? 0 : (double)CurrentIndex / TotalRounds;
    public int DisplayNumber => CurrentIndex + 1;
    public int SlotCount => _targetWord.Length;
    public string CurrentAnswer => string.Concat(PlacedLetters.Select(l => l.Letter));
    public bool CanCheck => PlacedLetters.Count == SlotCount && !IsChecking;

    public WordScrambleGameViewModel(
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
    public async Task InitializeAsync(string? catId = null)
    {
        IsLoading = true;
        try
        {
            CategoryId = catId ?? "animals";
            _profile = _profileService.GetActiveProfile();

            var allItems = await _contentService.GetItemsAsync(CategoryId);
            _items = allItems
                .Where(i => i.NameTr.Length >= 3 && i.NameTr.Length <= 8)
                .OrderBy(_ => Random.Shared.Next())
                .Take(8)
                .ToList();

            TotalRounds = _items.Count;
            _rounds = new();
            CurrentIndex = 0;
            OnPropertyChanged(nameof(Progress));
            OnPropertyChanged(nameof(DisplayNumber));
            LoadCurrentRound();
        }
        finally { IsLoading = false; }
    }

    private void LoadCurrentRound()
    {
        if (CurrentIndex >= TotalRounds) return;

        var item = _items[CurrentIndex];
        CurrentItem = item;
        _targetWord = item.NameTr.ToUpperInvariant();
        IsChecking = false;
        ShowCorrectFeedback = false;
        ShowWrongFeedback = false;

        // Harfleri karıştır
        _allLetters = _targetWord
            .Select((c, i) => new ScrambleLetter { Id = i, Letter = c.ToString() })
            .OrderBy(_ => Random.Shared.Next())
            .ToList();

        // Karışık halde hepsi available'da
        AvailableLetters = new List<ScrambleLetter>(_allLetters);
        PlacedLetters = new List<ScrambleLetter>();

        OnPropertyChanged(nameof(SlotCount));
        OnPropertyChanged(nameof(CanCheck));

        _rounds.Add(new GameRound
        {
            CorrectItem = item,
            Result = GameResult.NotPlayed
        });
    }

    [RelayCommand]
    public void PlaceLetter(ScrambleLetter letter)
    {
        if (IsChecking || !AvailableLetters.Contains(letter)) return;

        var newAvail = new List<ScrambleLetter>(AvailableLetters);
        newAvail.Remove(letter);
        AvailableLetters = newAvail;

        var newPlaced = new List<ScrambleLetter>(PlacedLetters) { letter };
        PlacedLetters = newPlaced;

        OnPropertyChanged(nameof(CanCheck));
        OnPropertyChanged(nameof(CurrentAnswer));
    }

    [RelayCommand]
    public void RemoveLetter(ScrambleLetter letter)
    {
        if (IsChecking || !PlacedLetters.Contains(letter)) return;

        var newPlaced = new List<ScrambleLetter>(PlacedLetters);
        newPlaced.Remove(letter);
        PlacedLetters = newPlaced;

        var newAvail = new List<ScrambleLetter>(AvailableLetters) { letter };
        AvailableLetters = newAvail;

        OnPropertyChanged(nameof(CanCheck));
        OnPropertyChanged(nameof(CurrentAnswer));
    }

    [RelayCommand]
    public void ClearAnswer()
    {
        if (IsChecking) return;
        AvailableLetters = new List<ScrambleLetter>(_allLetters);
        PlacedLetters = new List<ScrambleLetter>();
        OnPropertyChanged(nameof(CanCheck));
        OnPropertyChanged(nameof(CurrentAnswer));
    }

    [RelayCommand]
    public async Task CheckAnswerAsync()
    {
        if (!CanCheck) return;
        IsChecking = true;

        bool correct = CurrentAnswer == _targetWord;
        ShowCorrectFeedback = correct;
        ShowWrongFeedback = !correct;

        _rounds[CurrentIndex].Result = correct ? GameResult.Correct : GameResult.Wrong;

        if (correct) await _audioService.PlayCorrectAsync();
        else await _audioService.PlayWrongAsync();

        await Task.Delay(1200);

        ShowCorrectFeedback = false;
        ShowWrongFeedback = false;
        CurrentIndex++;
        OnPropertyChanged(nameof(Progress));
        OnPropertyChanged(nameof(DisplayNumber));

        if (CurrentIndex >= TotalRounds)
        {
            await _audioService.PlayCompleteAsync();
            var session = new GameSession
            {
                CategoryId = CategoryId,
                GameType = GameType.WordScramble,
                FinishedAt = DateTime.UtcNow,
                Rounds = _rounds
            };
            if (_profile is not null)
                _profileService.UpdateProgress(_profile.Id, CategoryId, session);
            await _navigationService.GoToResultAsync(session);
            return;
        }

        LoadCurrentRound();
    }

    [RelayCommand]
    public async Task SpeakClueAsync()
    {
        if (CurrentItem?.SoundClueText is { Length: > 0 } clue)
            await _audioService.SpeakTextAsync(clue);
    }

    [RelayCommand]
    public Task GoBackAsync() => _navigationService.GoBackAsync();
}

public partial class ScrambleLetter : ObservableObject
{
    public int Id { get; set; }
    public string Letter { get; set; } = "";
}
