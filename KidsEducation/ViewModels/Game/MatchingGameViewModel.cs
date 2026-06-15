using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KidsEducation.Enums;
using KidsEducation.Models;
using KidsEducation.Services;

namespace KidsEducation.ViewModels.Game;

public partial class MatchingGameViewModel : ObservableObject
{
    private readonly ContentService _contentService;
    private readonly AudioService _audioService;
    private readonly NavigationService _navigationService;
    private readonly ProfileService _profileService;

    [ObservableProperty] private List<MatchCard> _imageCards = new();
    [ObservableProperty] private List<MatchCard> _nameCards = new();
    [ObservableProperty] private int _matchedCount;
    [ObservableProperty] private int _totalPairs;
    [ObservableProperty] private int _mistakes;
    [ObservableProperty] private bool _isComplete;
    [ObservableProperty] private string _feedbackEmoji = "";
    [ObservableProperty] private bool _showFeedback;

    public double ProgressPercent => TotalPairs == 0 ? 0 : (double)MatchedCount / TotalPairs;

    private MatchCard? _selectedImage;
    private MatchCard? _selectedName;
    private List<LearningItem> _allItems = new();
    private string _categoryId = string.Empty;

    public string CategoryId
    {
        get => _categoryId;
        set { _categoryId = value; }
    }

    public MatchingGameViewModel(
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

        var profile = _profileService.GetActiveProfile();
        _allItems = await _contentService.GetItemsAsync(_categoryId);

        var selected = _allItems.OrderBy(_ => Guid.NewGuid()).Take(5).ToList();
        TotalPairs = selected.Count;
        MatchedCount = 0;
        Mistakes = 0;
        IsComplete = false;
        _selectedImage = null;
        _selectedName = null;
        OnPropertyChanged(nameof(ProgressPercent));

        var images = selected.Select(item => new MatchCard
        {
            ItemId = item.Id,
            ImagePath = item.ImagePath,
            NameTr = item.NameTr,
            CardType = MatchCardType.Image
        }).OrderBy(_ => Guid.NewGuid()).ToList();

        var names = selected.Select(item => new MatchCard
        {
            ItemId = item.Id,
            ImagePath = item.ImagePath,
            NameTr = item.NameTr,
            CardType = MatchCardType.Name
        }).OrderBy(_ => Guid.NewGuid()).ToList();

        ImageCards = images;
        NameCards = names;
    }

    [RelayCommand]
    public async Task SelectImageCardAsync(MatchCard card)
    {
        if (card.IsMatched || card.IsSelected) return;

        // Önceki seçimi kaldır
        if (_selectedImage is not null)
            _selectedImage.IsSelected = false;

        _selectedImage = card;
        card.IsSelected = true;
        await _audioService.PlayClickAsync();

        await TryMatchAsync();
    }

    [RelayCommand]
    public async Task SelectNameCardAsync(MatchCard card)
    {
        if (card.IsMatched || card.IsSelected) return;

        if (_selectedName is not null)
            _selectedName.IsSelected = false;

        _selectedName = card;
        card.IsSelected = true;
        await _audioService.PlayClickAsync();

        await TryMatchAsync();
    }

    private async Task TryMatchAsync()
    {
        if (_selectedImage is null || _selectedName is null) return;

        var img = _selectedImage;
        var name = _selectedName;
        _selectedImage = null;
        _selectedName = null;

        if (img.ItemId == name.ItemId)
        {
            // Doğru eşleşme
            img.IsMatched = true;
            name.IsMatched = true;
            img.IsSelected = false;
            name.IsSelected = false;
            MatchedCount++;
            OnPropertyChanged(nameof(ProgressPercent));

            await _audioService.PlayCorrectAsync();
            await ShowFeedbackAsync("⭐");

            if (MatchedCount >= TotalPairs)
                await CompleteAsync();
        }
        else
        {
            // Yanlış eşleşme
            Mistakes++;
            img.IsWrong = true;
            name.IsWrong = true;
            await _audioService.PlayWrongAsync();
            await ShowFeedbackAsync("❌");

            await Task.Delay(700);
            img.IsWrong = false;
            name.IsWrong = false;
            img.IsSelected = false;
            name.IsSelected = false;
        }
    }

    private async Task ShowFeedbackAsync(string emoji)
    {
        FeedbackEmoji = emoji;
        ShowFeedback = true;
        await Task.Delay(600);
        ShowFeedback = false;
    }

    private async Task CompleteAsync()
    {
        IsComplete = true;
        await _audioService.PlayCompleteAsync();

        var rounds = Enumerable.Range(0, TotalPairs)
            .Select(i => new GameRound
            {
                CorrectItem = new LearningItem { Id = $"match_{i}" },
                Result = i < TotalPairs - Mistakes ? GameResult.Correct : GameResult.Wrong
            }).ToList();

        var session = new GameSession
        {
            CategoryId = _categoryId,
            GameType = GameType.Matching,
            FinishedAt = DateTime.UtcNow,
            Rounds = rounds,
            DifficultyLevel = 1
        };

        var profile = _profileService.GetActiveProfile();
        if (profile is not null)
            _profileService.UpdateProgress(profile.Id, _categoryId, session);

        await _navigationService.GoToResultAsync(session);
    }

    [RelayCommand]
    public async Task GoBackAsync() => await _navigationService.GoBackAsync();
}

public partial class MatchCard : ObservableObject
{
    public string ItemId { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty;
    public string NameTr { get; set; } = string.Empty;
    public MatchCardType CardType { get; set; }

    [ObservableProperty] private bool _isSelected;
    [ObservableProperty] private bool _isMatched;
    [ObservableProperty] private bool _isWrong;

    public Brush BorderColor
    {
        get
        {
            if (IsMatched) return new SolidColorBrush(Color.FromArgb("#22C55E"));
            if (IsWrong) return new SolidColorBrush(Color.FromArgb("#EF4444"));
            if (IsSelected) return new SolidColorBrush(Color.FromArgb("#6C62F5"));
            return new SolidColorBrush(Color.FromArgb("#E8E8F0"));
        }
    }

    public Color BackgroundColor
    {
        get
        {
            if (IsMatched) return Color.FromArgb("#F0FFF4");
            if (IsWrong) return Color.FromArgb("#FFF0F0");
            if (IsSelected) return Color.FromArgb("#EDE8FF");
            return Color.FromArgb("#FFFFFF");
        }
    }

    protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName is nameof(IsSelected) or nameof(IsMatched) or nameof(IsWrong))
        {
            OnPropertyChanged(nameof(BorderColor));
            OnPropertyChanged(nameof(BackgroundColor));
        }
    }
}

public enum MatchCardType { Image, Name }
