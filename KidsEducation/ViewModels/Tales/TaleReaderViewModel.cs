using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KidsEducation.Models;
using KidsEducation.Services;

namespace KidsEducation.ViewModels.Tales;

[QueryProperty(nameof(TaleId), "taleId")]
public partial class TaleReaderViewModel : ObservableObject
{
    private readonly TaleService _taleService;
    private readonly AudioService _audioService;

    [ObservableProperty] private string _taleId = "";
    [ObservableProperty] private Tale? _tale;
    [ObservableProperty] private TalePage? _currentPage;
    [ObservableProperty] private int _currentPageIndex;
    [ObservableProperty] private bool _isPlaying;
    [ObservableProperty] private bool _isLoading = true;
    [ObservableProperty] private bool _isFinished;

    public int TotalPages => Tale?.Pages.Count ?? 0;
    public bool CanGoBack => CurrentPageIndex > 0;
    public bool CanGoNext => CurrentPageIndex < TotalPages - 1;
    public string PageText => $"{CurrentPageIndex + 1} / {TotalPages}";
    public double PageProgress => TotalPages > 1 ? (double)CurrentPageIndex / (TotalPages - 1) : 0;

    public TaleReaderViewModel(TaleService taleService, AudioService audioService)
    {
        _taleService = taleService;
        _audioService = audioService;
    }

    partial void OnTaleIdChanged(string value) => _ = LoadAsync();

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (string.IsNullOrEmpty(TaleId)) return;
        IsLoading = true;
        Tale = await _taleService.GetTaleByIdAsync(TaleId);
        CurrentPageIndex = 0;
        CurrentPage = Tale?.Pages.FirstOrDefault();
        IsFinished = false;
        IsLoading = false;
        RefreshNavigation();
    }

    [RelayCommand]
    public void NextPage()
    {
        if (Tale is null) return;
        if (CurrentPageIndex < Tale.Pages.Count - 1)
        {
            CurrentPageIndex++;
            CurrentPage = Tale.Pages[CurrentPageIndex];
            RefreshNavigation();
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
        }
        else
        {
            IsFinished = true;
        }
    }

    [RelayCommand]
    public void PrevPage()
    {
        if (CurrentPageIndex <= 0) return;
        CurrentPageIndex--;
        CurrentPage = Tale?.Pages[CurrentPageIndex];
        RefreshNavigation();
        HapticFeedback.Default.Perform(HapticFeedbackType.Click);
    }

    [RelayCommand]
    public async Task PlayAudioAsync()
    {
        if (CurrentPage is null || string.IsNullOrEmpty(CurrentPage.AudioFile)) return;
        if (IsPlaying) return;

        IsPlaying = true;
        try
        {
            await _audioService.PlayFileAsync(CurrentPage.AudioFile);
        }
        catch { }
        finally
        {
            IsPlaying = false;
        }
    }

    [RelayCommand]
    public Task GoBackAsync() => Shell.Current.GoToAsync("..");

    [RelayCommand]
    public void Restart()
    {
        CurrentPageIndex = 0;
        CurrentPage = Tale?.Pages.FirstOrDefault();
        IsFinished = false;
        RefreshNavigation();
    }

    private void RefreshNavigation()
    {
        OnPropertyChanged(nameof(CanGoBack));
        OnPropertyChanged(nameof(CanGoNext));
        OnPropertyChanged(nameof(PageText));
        OnPropertyChanged(nameof(PageProgress));
    }
}
