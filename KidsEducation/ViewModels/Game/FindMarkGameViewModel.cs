using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KidsEducation.Enums;
using KidsEducation.Models;
using KidsEducation.Services;

namespace KidsEducation.ViewModels.Game;

public partial class FindMarkGameViewModel : ObservableObject
{
    private readonly ContentService _contentService;
    private readonly AudioService _audioService;
    private readonly NavigationService _navigationService;
    private readonly ProfileService _profileService;

    [ObservableProperty] private List<FindMarkOption> _options = new();
    [ObservableProperty] private string _questionText = "";
    [ObservableProperty] private int _currentRound;
    [ObservableProperty] private int _totalRounds = 6;
    [ObservableProperty] private bool _isAnswered;
    [ObservableProperty] private bool _showFeedback;
    [ObservableProperty] private string _feedbackEmoji = "";
    [ObservableProperty] private bool _isLoading = true;

    public double ProgressPercent => TotalRounds == 0 ? 0 : (double)CurrentRound / TotalRounds;

    private List<LearningItem> _allItems = new();
    private string _categoryId = string.Empty;
    private LearningItem? _correctItem;
    private int _mistakes;
    private int _correctCount;

    public int CorrectCount
    {
        get => _correctCount;
        private set { _correctCount = value; OnPropertyChanged(); }
    }

    public string CategoryId
    {
        get => _categoryId;
        set => _categoryId = value;
    }

    public FindMarkGameViewModel(
        ContentService contentService,
        AudioService audioService,
        NavigationService navigationService,
        ProfileService profileService)
    {
        _contentService = contentService;
        _audioService = audioService;
        _navigationService = navigationService;
        _profileService = profileService;
    }

    [RelayCommand]
    public async Task InitializeAsync(string? categoryId)
    {
        if (!string.IsNullOrWhiteSpace(categoryId))
            _categoryId = categoryId;

        IsLoading = true;
        _allItems = await _contentService.GetItemsAsync(_categoryId);
        CurrentRound = 0;
        CorrectCount = 0;
        _mistakes = 0;

        await NextRoundAsync();
        IsLoading = false;
    }

    private async Task NextRoundAsync()
    {
        if (CurrentRound >= TotalRounds)
        {
            await CompleteAsync();
            return;
        }

        IsAnswered = false;

        // Doğru cevabı seç
        _correctItem = _allItems.OrderBy(_ => Guid.NewGuid()).First();

        // 6 seçenek: 1 doğru + 5 yanlış (tekrar yok)
        var wrong = _allItems
            .Where(x => x.Id != _correctItem.Id)
            .OrderBy(_ => Guid.NewGuid())
            .Take(5)
            .ToList();

        var all = wrong.Append(_correctItem).OrderBy(_ => Guid.NewGuid()).ToList();

        Options = all.Select(item => new FindMarkOption
        {
            ItemId = item.Id,
            ImagePath = item.ImagePath,
            NameTr = item.NameTr,
            IsCorrect = item.Id == _correctItem.Id
        }).ToList();

        QuestionText = $"Hangisi {_correctItem.NameTr}?";
        CurrentRound++;
        OnPropertyChanged(nameof(ProgressPercent));

        // Sesli soru
        await _audioService.PlayItemSoundAsync(_correctItem.Id);
    }

    [RelayCommand]
    public async Task SelectOptionAsync(FindMarkOption option)
    {
        if (IsAnswered) return;
        IsAnswered = true;

        option.IsSelected = true;

        if (option.IsCorrect)
        {
            option.State = FindMarkState.Correct;
            CorrectCount++;
            await _audioService.PlayCorrectAsync();
            await ShowFeedbackAsync("⭐");
        }
        else
        {
            option.State = FindMarkState.Wrong;
            _mistakes++;
            // Doğru olanı göster
            var correct = Options.First(o => o.IsCorrect);
            correct.State = FindMarkState.Correct;
            await _audioService.PlayWrongAsync();
            await ShowFeedbackAsync("❌");
        }

        await Task.Delay(1000);
        await NextRoundAsync();
    }

    private async Task ShowFeedbackAsync(string emoji)
    {
        FeedbackEmoji = emoji;
        ShowFeedback = true;
        await Task.Delay(700);
        ShowFeedback = false;
    }

    private async Task CompleteAsync()
    {
        await _audioService.PlayCompleteAsync();

        var stars = _mistakes == 0 ? 3 : _mistakes <= 2 ? 2 : 1;
        var score = Math.Max(0, CorrectCount * 50 - _mistakes * 15);

        var profile = _profileService.GetActiveProfile();
        if (profile is not null)
        {
            var rounds = Enumerable.Range(0, TotalRounds)
                .Select(i => new GameRound
                {
                    CorrectItem = new LearningItem { Id = $"findmark_{i}" },
                    Result = i < CorrectCount ? GameResult.Correct : GameResult.Wrong
                }).ToList();

            _profileService.UpdateProgress(profile.Id, _categoryId, new GameSession
            {
                GameType = GameType.FindAndMark,
                FinishedAt = DateTime.UtcNow,
                Rounds = rounds,
                DifficultyLevel = 1
            });
        }

        await Shell.Current.GoToAsync($"result?stars={stars}&score={score}&categoryId={Uri.EscapeDataString(_categoryId)}");
    }

    [RelayCommand]
    public async Task GoBackAsync() => await _navigationService.GoBackAsync();
}

public partial class FindMarkOption : ObservableObject
{
    public string ItemId { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty;
    public string NameTr { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }

    [ObservableProperty] private bool _isSelected;
    [ObservableProperty] private FindMarkState _state = FindMarkState.Default;

    public Color BorderColor => State switch
    {
        FindMarkState.Correct => Color.FromArgb("#22C55E"),
        FindMarkState.Wrong => Color.FromArgb("#EF4444"),
        _ => Color.FromArgb("#E8E8F0")
    };

    public Color BackgroundColor => State switch
    {
        FindMarkState.Correct => Color.FromArgb("#F0FFF4"),
        FindMarkState.Wrong => Color.FromArgb("#FFF0F0"),
        _ => Color.FromArgb("#FFFFFF")
    };

    protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName == nameof(State))
        {
            OnPropertyChanged(nameof(BorderColor));
            OnPropertyChanged(nameof(BackgroundColor));
        }
    }
}

public enum FindMarkState { Default, Correct, Wrong }
