using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KidsEducation.Enums;
using KidsEducation.Models;
using KidsEducation.Services;

namespace KidsEducation.ViewModels.Game;

public partial class LetterDropGameViewModel : ObservableObject
{
    private readonly ContentService _contentService;
    private readonly ProfileService _profileService;
    private readonly NavigationService _navigationService;
    private readonly AudioService _audioService;

    private List<LearningItem> _items = new();
    private List<GameRound> _rounds = new();
    private ChildProfile? _profile;
    private bool _isAnswering;

    [ObservableProperty] private string _categoryId = "animals";
    [ObservableProperty] private bool _isLoading = true;
    [ObservableProperty] private int _currentIndex;
    [ObservableProperty] private int _totalRounds;
    [ObservableProperty] private LearningItem? _currentItem;
    [ObservableProperty] private string _displayWord = "";        // "K _ D İ"
    [ObservableProperty] private string _missingLetter = "";
    [ObservableProperty] private List<LetterOption> _letterOptions = new();
    [ObservableProperty] private bool _showCorrectFeedback;
    [ObservableProperty] private bool _showWrongFeedback;

    public double Progress => TotalRounds == 0 ? 0 : (double)CurrentIndex / TotalRounds;
    public int DisplayNumber => CurrentIndex + 1;

    public LetterOption? LetterOption0 => LetterOptions.Count > 0 ? LetterOptions[0] : null;
    public LetterOption? LetterOption1 => LetterOptions.Count > 1 ? LetterOptions[1] : null;
    public LetterOption? LetterOption2 => LetterOptions.Count > 2 ? LetterOptions[2] : null;
    public LetterOption? LetterOption3 => LetterOptions.Count > 3 ? LetterOptions[3] : null;

    partial void OnLetterOptionsChanged(List<LetterOption> value)
    {
        OnPropertyChanged(nameof(LetterOption0));
        OnPropertyChanged(nameof(LetterOption1));
        OnPropertyChanged(nameof(LetterOption2));
        OnPropertyChanged(nameof(LetterOption3));
    }

    public LetterDropGameViewModel(
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
                .Where(i => i.NameTr.Length >= 3)
                .OrderBy(_ => Random.Shared.Next())
                .Take(10)
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
        _isAnswering = false;
        ShowCorrectFeedback = false;
        ShowWrongFeedback = false;

        var word = item.NameTr.ToUpper(new System.Globalization.CultureInfo("tr-TR"));
        // Rastgele bir harf seç (ilk veya son harf dışında)
        var blankIdx = Random.Shared.Next(1, word.Length - 1);
        MissingLetter = word[blankIdx].ToString();

        // Kelimeyi görsel: "K _ D İ"
        var chars = word.Select((c, i) => i == blankIdx ? "_" : c.ToString()).ToList();
        DisplayWord = string.Join(" ", chars);

        // 4 harf seçeneği (Türk alfabesi)
        var wrongLetters = new[] { 'A', 'B', 'C', 'Ç', 'D', 'E', 'F', 'G', 'Ğ', 'H', 'I', 'İ', 'J', 'K', 'L', 'M', 'N', 'O', 'Ö', 'P', 'R', 'S', 'Ş', 'T', 'U', 'Ü', 'V', 'Y', 'Z' }
            .Where(c => c.ToString() != MissingLetter)
            .OrderBy(_ => Random.Shared.Next())
            .Take(3)
            .Select(c => new LetterOption { Letter = c.ToString(), IsCorrect = false })
            .ToList();

        wrongLetters.Add(new LetterOption { Letter = MissingLetter, IsCorrect = true });
        LetterOptions = wrongLetters.OrderBy(_ => Random.Shared.Next()).ToList();

        _rounds.Add(new GameRound
        {
            CorrectItem = item,
            Result = GameResult.NotPlayed
        });
    }

    [RelayCommand]
    public async Task SelectLetterAsync(LetterOption? option)
    {
        if (option is null || _isAnswering) return;
        _isAnswering = true;

        bool correct = option.IsCorrect;
        ShowCorrectFeedback = correct;
        ShowWrongFeedback = !correct;

        var round = _rounds[CurrentIndex];
        round.Result = correct ? GameResult.Correct : GameResult.Wrong;

        if (correct) await _audioService.PlayCorrectAsync();
        else await _audioService.PlayWrongAsync();

        await Task.Delay(1000);

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
                GameType = GameType.LetterDrop,
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

public partial class LetterOption : ObservableObject
{
    public string Letter { get; set; } = "";
    public bool IsCorrect { get; set; }
    [ObservableProperty] private bool _isSelected;
}
