using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KidsEducation.Enums;
using KidsEducation.Models;
using KidsEducation.Services;

namespace KidsEducation.ViewModels.Game;

public partial class QuizGameViewModel : ObservableObject
{
    private readonly ContentService _contentService;
    private readonly ProfileService _profileService;
    private readonly NavigationService _navigationService;
    private readonly AudioService _audioService;
    private ChildProfile? _profile;

    private List<LearningItem> _allItems = new();
    private List<GameRound> _rounds = new();
    private bool _isAnswering;

    [ObservableProperty] private string _categoryId = "animals";
    [ObservableProperty] private bool _isLoading = true;
    [ObservableProperty] private int _currentIndex;
    [ObservableProperty] private int _totalRounds;
    [ObservableProperty] private LearningItem? _currentItem;
    [ObservableProperty] private List<QuizOption> _options = new();
    [ObservableProperty] private string? _selectedOptionId;
    [ObservableProperty] private bool _showFeedback;
    [ObservableProperty] private bool _lastAnswerCorrect;
    [ObservableProperty] private bool _showCorrectFeedback;
    [ObservableProperty] private bool _showWrongFeedback;

    public double Progress => TotalRounds == 0 ? 0 : (double)CurrentIndex / TotalRounds;
    public int DisplayNumber => CurrentIndex + 1;

    public QuizOption? Option0 => Options.Count > 0 ? Options[0] : null;
    public QuizOption? Option1 => Options.Count > 1 ? Options[1] : null;
    public QuizOption? Option2 => Options.Count > 2 ? Options[2] : null;
    public QuizOption? Option3 => Options.Count > 3 ? Options[3] : null;

    partial void OnOptionsChanged(List<QuizOption> value)
    {
        OnPropertyChanged(nameof(Option0));
        OnPropertyChanged(nameof(Option1));
        OnPropertyChanged(nameof(Option2));
        OnPropertyChanged(nameof(Option3));
    }

    public QuizGameViewModel(
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

            var items = CategoryId == "mixed" && _profile is not null
                ? await _contentService.GetMixedGameItemsAsync(_profile, 10)
                : await _contentService.GetItemsAsync(CategoryId);

            if (items.Count == 0 && CategoryId == "mixed")
                items = await _contentService.GetItemsAsync("animals");

            _allItems = items.OrderBy(_ => Random.Shared.Next()).ToList();

            TotalRounds = Math.Min(10, _allItems.Count);
            _rounds = new List<GameRound>();
            CurrentIndex = 0;

            OnPropertyChanged(nameof(Progress));
            OnPropertyChanged(nameof(DisplayNumber));
            LoadCurrentRound();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void LoadCurrentRound()
    {
        if (CurrentIndex >= TotalRounds) return;

        var correct = _allItems[CurrentIndex];
        CurrentItem = correct;
        SelectedOptionId = null;
        ShowFeedback = false;
        _isAnswering = false;

        var distractors = _allItems
            .Where(i => i.Id != correct.Id)
            .OrderBy(_ => Random.Shared.Next())
            .Take(3)
            .ToList();

        var opts = distractors.Select(d => new QuizOption { Item = d, IsCorrect = false }).ToList();
        opts.Add(new QuizOption { Item = correct, IsCorrect = true });
        opts = opts.OrderBy(_ => Random.Shared.Next()).ToList();

        Options = opts;

        _rounds.Add(new GameRound
        {
            CorrectItem = correct,
            Options = opts.Select(o => o.Item).ToList(),
            Result = GameResult.NotPlayed
        });
    }

    [RelayCommand]
    public async Task SelectOptionAsync(QuizOption? option)
    {
        if (option is null || _isAnswering || ShowFeedback) return;
        _isAnswering = true;

        SelectedOptionId = option.Item.Id;
        option.IsSelected = true;

        bool correct = option.IsCorrect;
        LastAnswerCorrect = correct;
        ShowFeedback = true;
        ShowCorrectFeedback = correct;
        ShowWrongFeedback = !correct;

        var round = _rounds[CurrentIndex];
        round.SelectedItemId = option.Item.Id;
        round.Result = correct ? GameResult.Correct : GameResult.Wrong;

        if (correct)
            await _audioService.PlayCorrectAsync();
        else
            await _audioService.PlayWrongAsync();

        await Task.Delay(1100);

        ShowFeedback = false;
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
                GameType = GameType.MatchName,
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
        var clue = CurrentItem?.SoundClueText;
        if (!string.IsNullOrWhiteSpace(clue))
            await _audioService.SpeakTextAsync(clue);
    }

    [RelayCommand]
    public Task GoBackAsync() => _navigationService.GoBackAsync();
}

public partial class QuizOption : ObservableObject
{
    public LearningItem Item { get; set; } = null!;
    public bool IsCorrect { get; set; }
    [ObservableProperty] private bool _isSelected;
}
