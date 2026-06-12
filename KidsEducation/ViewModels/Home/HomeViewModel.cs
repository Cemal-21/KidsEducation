using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KidsEducation.Enums;
using KidsEducation.Models;
using KidsEducation.Services;

namespace KidsEducation.ViewModels.Home;

public partial class HomeViewModel : ObservableObject
{
    private readonly ContentService _contentService;
    private readonly ProfileService _profileService;
    private readonly NavigationService _navigationService;
    private readonly SmartLearningPathService _smartLearningPathService;

    // ── Mevcut ──────────────────────────────────────────────
    [ObservableProperty] private List<Category> _categories = new();
    [ObservableProperty] private ChildProfile? _activeProfile;
    [ObservableProperty] private bool _isLoading = true;

    // ── Yeni ────────────────────────────────────────────────
    [ObservableProperty]
    private string _todayDateText = DateTime.Now
                                                    .ToString("dddd, d MMMM",
                                                        new System.Globalization.CultureInfo("tr-TR"));

    [ObservableProperty] private LastLessonInfo? _lastLesson;
    [ObservableProperty] private DailyGoalInfo _dailyGoal = new();
    [ObservableProperty] private SmartLearningSuggestion? _smartSuggestion;

    // HasLastLesson → LastLesson değişince otomatik güncellenir
    public bool HasLastLesson => LastLesson is not null;
    public bool HasSmartSuggestion => SmartSuggestion is not null;

    partial void OnLastLessonChanged(LastLessonInfo? value) =>
        OnPropertyChanged(nameof(HasLastLesson));

    partial void OnSmartSuggestionChanged(SmartLearningSuggestion? value) =>
        OnPropertyChanged(nameof(HasSmartSuggestion));

    // ────────────────────────────────────────────────────────

    public HomeViewModel(
        ContentService contentService,
        ProfileService profileService,
        NavigationService navigationService,
        SmartLearningPathService smartLearningPathService)
    {
        _contentService = contentService;
        _profileService = profileService;
        _navigationService = navigationService;
        _smartLearningPathService = smartLearningPathService;
    }

    // ── Başlatma ─────────────────────────────────────────────
    [RelayCommand]
    public async Task InitializeAsync()
    {
        IsLoading = true;
        try
        {
            ActiveProfile = _profileService.GetActiveProfile();

            if (ActiveProfile is null)
            {
                await _navigationService.GoToProfileSelectionAsync();
                return;
            }

            Categories = await _contentService.GetCategoriesAsync(ActiveProfile);
            LastLesson = await _contentService.GetLastLessonAsync(ActiveProfile);
            DailyGoal = _profileService.GetDailyGoal(ActiveProfile);
            SmartSuggestion = await _smartLearningPathService.GetSuggestionAsync(ActiveProfile);
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ── Kategori seç ─────────────────────────────────────────
    [RelayCommand]
    public async Task SelectCategoryAsync(Category category)
    {
        await _navigationService.GoToCategoryAsync(category.Id);
    }

    // ── Kaldığı yerden devam et ───────────────────────────────
    [RelayCommand]
    public async Task ContinueLastLessonAsync()
    {
        if (LastLesson is null) return;
        await _navigationService.GoToLessonAsync(LastLesson.LessonId);
    }

    [RelayCommand]
    public Task GoToSongsAsync() =>
        _navigationService.GoToSongsAsync();

    [RelayCommand]
    public Task GoToParentalAsync() =>
        _navigationService.GoToParentalAsync();

    [RelayCommand]
    public async Task GoToSmartSuggestionAsync()
    {
        if (SmartSuggestion is null)
            return;

        await _navigationService.GoToSmartSuggestionAsync(SmartSuggestion);
    }

    [RelayCommand]
    public Task GoToCurriculumActivitiesAsync() =>
        _navigationService.GoToCurriculumActivitiesAsync();

    [RelayCommand]
    public async Task GoToMemoryGameAsync() =>
        await Shell.Current.GoToAsync("memorygame");

    [RelayCommand]
    public async Task GoToZoomGameAsync() =>
        await Shell.Current.GoToAsync("zoomgame");

    [RelayCommand]
    public async Task GoToSoundGameAsync() =>
        await Shell.Current.GoToAsync("soundgame");

    [RelayCommand]
    public async Task GoToBalloonGameAsync() =>
        await Shell.Current.GoToAsync("balloongame");

    [RelayCommand]
    public async Task GoToSequenceGameAsync() =>
        await Shell.Current.GoToAsync("sequencegame");

    [RelayCommand]
    public async Task GoToStoryGameAsync() =>
        await Shell.Current.GoToAsync("storygame");
    // ── Tab bar navigasyon ────────────────────────────────────
    [RelayCommand]
    public async Task GoToProfileSelectionAsync() =>
        await _navigationService.GoToProfileSelectionAsync();

    [RelayCommand]
    public async Task GoToProgressAsync() =>
        await Shell.Current.GoToAsync("//progress");

    [RelayCommand]
    public async Task GoToAchievementsAsync() =>
        await Shell.Current.GoToAsync("//achievements");
}
