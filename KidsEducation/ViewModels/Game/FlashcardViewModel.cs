using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KidsEducation.Models;
using KidsEducation.Services;

namespace KidsEducation.ViewModels.Game;

public partial class FlashcardViewModel : ObservableObject
{
    private readonly ContentService _contentService;
    private readonly AudioService _audioService;
    private readonly NavigationService _navigationService;

    [ObservableProperty] private List<LearningItem> _items = new();
    [ObservableProperty] private LearningItem? _currentItem;
    [ObservableProperty] private int _currentIndex;
    [ObservableProperty] private bool _isFlipped;
    [ObservableProperty] private bool _isLoading = true;
    [ObservableProperty] private string _categoryName = "";

    private string _categoryId = string.Empty;

    public string CategoryId
    {
        get => _categoryId;
        set => _categoryId = value;
    }

    public int TotalCount => Items.Count;
    public bool HasPrevious => CurrentIndex > 0;
    public bool HasNext => CurrentIndex < Items.Count - 1;
    public bool IsLast => CurrentIndex == Items.Count - 1;

    public double ProgressPercent => TotalCount == 0 ? 0 : (double)(CurrentIndex + 1) / TotalCount;
    public string ProgressText => $"{CurrentIndex + 1} / {TotalCount}";

    public FlashcardViewModel(
        ContentService contentService,
        AudioService audioService,
        NavigationService navigationService)
    {
        _contentService = contentService;
        _audioService = audioService;
        _navigationService = navigationService;
    }

    [RelayCommand]
    public async Task InitializeAsync(string? categoryId)
    {
        if (!string.IsNullOrWhiteSpace(categoryId))
            _categoryId = categoryId;

        IsLoading = true;
        var allItems = await _contentService.GetItemsAsync(_categoryId);
        Items = allItems.OrderBy(_ => Guid.NewGuid()).ToList();
        CurrentIndex = 0;
        IsFlipped = false;
        CurrentItem = Items.FirstOrDefault();
        IsLoading = false;

        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(HasPrevious));
        OnPropertyChanged(nameof(HasNext));
        OnPropertyChanged(nameof(IsLast));
        OnPropertyChanged(nameof(ProgressPercent));
        OnPropertyChanged(nameof(ProgressText));

        if (CurrentItem is not null)
            await _audioService.PlayItemSoundAsync(CurrentItem.Id);
    }

    [RelayCommand]
    public async Task FlipCardAsync()
    {
        IsFlipped = !IsFlipped;
        await _audioService.PlayClickAsync();

        if (IsFlipped && CurrentItem is not null)
            await _audioService.PlayItemSoundAsync(CurrentItem.Id);
    }

    [RelayCommand]
    public async Task NextAsync()
    {
        if (!HasNext) return;
        CurrentIndex++;
        IsFlipped = false;
        CurrentItem = Items[CurrentIndex];
        NotifyNavigation();
        await _audioService.PlayClickAsync();
        await Task.Delay(300);
        await _audioService.PlayItemSoundAsync(CurrentItem.Id);
    }

    [RelayCommand]
    public async Task PreviousAsync()
    {
        if (!HasPrevious) return;
        CurrentIndex--;
        IsFlipped = false;
        CurrentItem = Items[CurrentIndex];
        NotifyNavigation();
        await _audioService.PlayClickAsync();
    }

    [RelayCommand]
    public async Task PlaySoundAsync()
    {
        if (CurrentItem is null) return;
        await _audioService.PlayItemSoundAsync(CurrentItem.Id);
    }

    [RelayCommand]
    public async Task ShuffleAsync()
    {
        Items = Items.OrderBy(_ => Guid.NewGuid()).ToList();
        CurrentIndex = 0;
        IsFlipped = false;
        CurrentItem = Items.FirstOrDefault();
        NotifyNavigation();
        OnPropertyChanged(nameof(TotalCount));
        await _audioService.PlayClickAsync();
        if (CurrentItem is not null)
            await _audioService.PlayItemSoundAsync(CurrentItem.Id);
    }

    private void NotifyNavigation()
    {
        OnPropertyChanged(nameof(HasPrevious));
        OnPropertyChanged(nameof(HasNext));
        OnPropertyChanged(nameof(IsLast));
        OnPropertyChanged(nameof(ProgressPercent));
        OnPropertyChanged(nameof(ProgressText));
    }

    [RelayCommand]
    public async Task GoBackAsync() => await _navigationService.GoBackAsync();
}
