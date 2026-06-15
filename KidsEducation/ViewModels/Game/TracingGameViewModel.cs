using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KidsEducation.Enums;
using KidsEducation.Models;
using KidsEducation.Services;

namespace KidsEducation.ViewModels.Game;

public partial class TracingGameViewModel : ObservableObject
{
    private readonly ProfileService _profileService;
    private readonly NavigationService _navigationService;
    private readonly AudioService _audioService;

    private ChildProfile? _profile;
    private List<TracingItem> _items = new();

    private const double MinStrokeLength = 200;

    public TracingDrawable TracingDrawable { get; } = new();

    [ObservableProperty] private TracingItem? _currentItem;
    [ObservableProperty] private int _currentIndex;
    [ObservableProperty] private int _totalItems;
    [ObservableProperty] private bool _isLoading = true;
    [ObservableProperty] private bool _canComplete;
    [ObservableProperty] private bool _showSuccess;
    [ObservableProperty] private string _categoryId = "letters";

    public double Progress => _totalItems == 0 ? 0 : (double)_currentIndex / _totalItems;
    public int DisplayNumber => _currentIndex + 1;

    public TracingGameViewModel(
        ProfileService profileService,
        NavigationService navigationService,
        AudioService audioService)
    {
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
            _profile = _profileService.GetActiveProfile();
            if (_profile is null) return;

            CategoryId = catId ?? "letters";
            _items = BuildTracingItems(CategoryId, _profile);
            TotalItems = _items.Count;
            CurrentIndex = 0;
            OnPropertyChanged(nameof(Progress));
            OnPropertyChanged(nameof(DisplayNumber));
            LoadCurrentItem();
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void UpdateStrokeLength(double canvasWidth, double canvasHeight)
    {
        CanComplete = IsTracePlausible(canvasWidth, canvasHeight);
    }

    private bool IsTracePlausible(double canvasWidth, double canvasHeight)
    {
        if (TracingDrawable.TotalStrokeLength < MinStrokeLength || TracingDrawable.PointCount < 15)
            return false;

        var bounds = TracingDrawable.GetStrokeBounds();
        if (bounds is null || canvasWidth <= 0 || canvasHeight <= 0)
            return false;

        var centerX = bounds.Value.X + bounds.Value.Width / 2;
        var centerY = bounds.Value.Y + bounds.Value.Height / 2;
        var isNearGuide = centerX >= canvasWidth * 0.15 &&
                          centerX <= canvasWidth * 0.85 &&
                          centerY >= canvasHeight * 0.10 &&
                          centerY <= canvasHeight * 0.90;

        // Çizimin hem yeterli yüksekliği hem genişliği olmalı (sadece kısa çizgi ya da nokta kabul edilmez)
        var hasEnoughShape = bounds.Value.Height >= canvasHeight * 0.25 &&
                             bounds.Value.Width >= canvasWidth * 0.12;

        return isNearGuide && hasEnoughShape;
    }

    [RelayCommand]
    public void Clear()
    {
        TracingDrawable.Clear();
        CanComplete = false;
    }

    [RelayCommand]
    public async Task CompleteCurrentAsync()
    {
        if (!CanComplete) return;

        ShowSuccess = true;
        await _audioService.PlayCorrectAsync();
        await Task.Delay(900);
        ShowSuccess = false;

        TracingDrawable.Clear();
        CanComplete = false;

        CurrentIndex++;
        OnPropertyChanged(nameof(Progress));
        OnPropertyChanged(nameof(DisplayNumber));

        if (CurrentIndex >= TotalItems)
        {
            await _audioService.PlayCompleteAsync();
            var session = new GameSession
            {
                CategoryId = CategoryId,
                GameType = Enums.GameType.Tracing,
                FinishedAt = DateTime.UtcNow,
                Rounds = Enumerable.Range(0, TotalItems).Select(_ => new GameRound
                {
                    CorrectItem = new LearningItem(),
                    Result = Enums.GameResult.Correct
                }).ToList()
            };
            if (_profile is not null)
                _profileService.UpdateProgress(_profile.Id, CategoryId, session);
            await _navigationService.GoToResultAsync(session);
            return;
        }

        LoadCurrentItem();
    }

    private void LoadCurrentItem()
    {
        if (CurrentIndex < _items.Count)
            CurrentItem = _items[CurrentIndex];
    }

    [RelayCommand]
    public Task GoBackAsync() => _navigationService.GoBackAsync();

    private static List<TracingItem> BuildTracingItems(string category, ChildProfile profile)
    {
        if (category == "numbers")
        {
            var max = profile.AgeGroup switch
            {
                AgeGroup.Toddler => 10,
                AgeGroup.Explorer => 15,
                _ => 20
            };

            string[] names = { "", "Bir", "İki", "Üç", "Dört", "Beş", "Altı", "Yedi", "Sekiz", "Dokuz",
                                "On", "On Bir", "On İki", "On Üç", "On Dört", "On Beş",
                                "On Altı", "On Yedi", "On Sekiz", "On Dokuz", "Yirmi" };

            return Enumerable.Range(1, max).Select(n => new TracingItem
            {
                Id = $"trace_number_{n}",
                DisplayChar = n.ToString(),
                NameTr = names[n],
                Category = "numbers",
                DifficultyLevel = n <= 5 ? 1 : n <= 10 ? 2 : 3
            }).ToList();
        }
        else
        {
            (string ch, string name)[] letters =
            {
                ("A","A Harfi"), ("B","B Harfi"), ("C","C Harfi"), ("Ç","Ç Harfi"),
                ("D","D Harfi"), ("E","E Harfi"), ("F","F Harfi"), ("G","G Harfi"),
                ("Ğ","Ğ Harfi"), ("H","H Harfi"), ("I","I Harfi"), ("İ","İ Harfi"),
                ("J","J Harfi"), ("K","K Harfi"), ("L","L Harfi"), ("M","M Harfi"),
                ("N","N Harfi"), ("O","O Harfi"), ("Ö","Ö Harfi"), ("P","P Harfi"),
                ("R","R Harfi"), ("S","S Harfi"), ("Ş","Ş Harfi"), ("T","T Harfi"),
                ("U","U Harfi"), ("Ü","Ü Harfi"), ("V","V Harfi"), ("Y","Y Harfi"),
                ("Z","Z Harfi")
            };

            return letters.Select((l, i) => new TracingItem
            {
                Id = $"trace_letter_{l.ch.ToLowerInvariant()}",
                DisplayChar = l.ch,
                NameTr = l.name,
                Category = "letters",
                DifficultyLevel = i < 10 ? 1 : i < 20 ? 2 : 3
            }).ToList();
        }
    }
}
