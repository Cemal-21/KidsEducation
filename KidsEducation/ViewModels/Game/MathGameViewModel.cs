using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KidsEducation.Enums;
using KidsEducation.Models;
using KidsEducation.Services;

namespace KidsEducation.ViewModels.Game;

public partial class MathGameViewModel : ObservableObject
{
    private readonly ProfileService _profileService;
    private readonly NavigationService _navigationService;
    private readonly AudioService _audioService;

    private List<GameRound> _rounds = new();
    private ChildProfile? _profile;
    private bool _isAnswering;
    private int _maxNumber;
    private bool _allowSubtraction;

    [ObservableProperty] private bool _isLoading = true;
    [ObservableProperty] private int _currentIndex;
    [ObservableProperty] private int _totalRounds = 10;
    [ObservableProperty] private int _numA;
    [ObservableProperty] private int _numB;
    [ObservableProperty] private bool _isAddition = true;
    [ObservableProperty] private string _questionText = "";
    [ObservableProperty] private string _visualEmojis = "";
    [ObservableProperty] private List<MathOption> _options = new();
    [ObservableProperty] private bool _showCorrectFeedback;
    [ObservableProperty] private bool _showWrongFeedback;

    public double Progress => TotalRounds == 0 ? 0 : (double)CurrentIndex / TotalRounds;
    public int DisplayNumber => CurrentIndex + 1;
    public int CorrectAnswer => IsAddition ? NumA + NumB : NumA - NumB;

    public MathOption? Option0 => Options.Count > 0 ? Options[0] : null;
    public MathOption? Option1 => Options.Count > 1 ? Options[1] : null;
    public MathOption? Option2 => Options.Count > 2 ? Options[2] : null;
    public MathOption? Option3 => Options.Count > 3 ? Options[3] : null;

    partial void OnOptionsChanged(List<MathOption> value)
    {
        OnPropertyChanged(nameof(Option0));
        OnPropertyChanged(nameof(Option1));
        OnPropertyChanged(nameof(Option2));
        OnPropertyChanged(nameof(Option3));
    }

    public MathGameViewModel(
        ProfileService profileService,
        NavigationService navigationService,
        AudioService audioService)
    {
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
            (_maxNumber, _allowSubtraction) = _profile?.AgeGroup switch
            {
                AgeGroup.Explorer => (10, true),
                AgeGroup.Adventurer => (20, true),
                _ => (5, false) // Toddler
            };

            _rounds = new();
            CurrentIndex = 0;
            TotalRounds = 10;
            OnPropertyChanged(nameof(Progress));
            OnPropertyChanged(nameof(DisplayNumber));
            LoadQuestion();
        }
        finally { IsLoading = false; }
    }

    private void LoadQuestion()
    {
        if (CurrentIndex >= TotalRounds) return;

        _isAnswering = false;
        ShowCorrectFeedback = false;
        ShowWrongFeedback = false;

        // Soru oluştur
        IsAddition = !_allowSubtraction || Random.Shared.Next(2) == 0;

        if (IsAddition)
        {
            NumA = Random.Shared.Next(1, _maxNumber);
            NumB = Random.Shared.Next(1, _maxNumber - NumA + 1);
        }
        else
        {
            NumA = Random.Shared.Next(2, _maxNumber + 1);
            NumB = Random.Shared.Next(1, NumA);
        }

        var op = IsAddition ? "+" : "−";
        QuestionText = $"{NumA}  {op}  {NumB}  =  ?";

        // Emoji görseli (max 10 emoji göster)
        var emoji = "🔵";
        if (IsAddition)
        {
            var showA = Math.Min(NumA, 10);
            var showB = Math.Min(NumB, 10);
            VisualEmojis = string.Concat(Enumerable.Repeat(emoji, showA)) +
                           "  +  " +
                           string.Concat(Enumerable.Repeat("🟡", showB));
        }
        else
        {
            var showA = Math.Min(NumA, 10);
            VisualEmojis = string.Concat(Enumerable.Repeat(emoji, showA));
        }

        // 4 seçenek
        int correct = CorrectAnswer;
        var wrongs = new HashSet<int> { correct };
        var optList = new List<MathOption> { new MathOption { Value = correct, IsCorrect = true } };

        while (optList.Count < 4)
        {
            int candidate = correct + Random.Shared.Next(-3, 4);
            if (candidate < 0) candidate = 0;
            if (!wrongs.Contains(candidate))
            {
                wrongs.Add(candidate);
                optList.Add(new MathOption { Value = candidate, IsCorrect = false });
            }
        }

        Options = optList.OrderBy(_ => Random.Shared.Next()).ToList();

        _rounds.Add(new GameRound
        {
            CorrectItem = new LearningItem { NameTr = $"{NumA}{(IsAddition ? "+" : "-")}{NumB}" },
            Result = GameResult.NotPlayed
        });
    }

    [RelayCommand]
    public async Task SelectOptionAsync(MathOption? option)
    {
        if (option is null || _isAnswering) return;
        _isAnswering = true;

        bool correct = option.IsCorrect;
        ShowCorrectFeedback = correct;
        ShowWrongFeedback = !correct;

        _rounds[CurrentIndex].Result = correct ? GameResult.Correct : GameResult.Wrong;

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
                CategoryId = "math",
                GameType = GameType.MathQuiz,
                FinishedAt = DateTime.UtcNow,
                Rounds = _rounds
            };
            if (_profile is not null)
                _profileService.UpdateProgress(_profile.Id, "math", session);
            await _navigationService.GoToResultAsync(session);
            return;
        }

        LoadQuestion();
    }

    [RelayCommand]
    public Task GoBackAsync() => _navigationService.GoBackAsync();
}

public class MathOption
{
    public int Value { get; set; }
    public bool IsCorrect { get; set; }
}
