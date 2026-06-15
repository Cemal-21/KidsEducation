using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KidsEducation.Models;
using KidsEducation.Services;

namespace KidsEducation.ViewModels.Home;

public partial class HomeViewModel : ObservableObject
{
    private readonly ContentService _contentService;
    private readonly ProfileService _profileService;
    private readonly NavigationService _navigationService;
    private readonly SmartLearningPathService _smartLearningPathService;
    private readonly ConnectivityService _connectivityService;
    private readonly DailyChallengeService _dailyChallengeService;
    private readonly AiCoachService _aiCoachService;
    private readonly DailyWordService _dailyWordService;
    private readonly AudioService _audioService;

    [ObservableProperty] private List<Category> _categories = new();
    [ObservableProperty] private ChildProfile? _activeProfile;
    [ObservableProperty] private bool _isLoading = true;
    [ObservableProperty] private string _todayDateText = DateTime.Now
        .ToString("dddd, d MMMM", new System.Globalization.CultureInfo("tr-TR"));
    [ObservableProperty] private LastLessonInfo? _lastLesson;
    [ObservableProperty] private DailyGoalInfo _dailyGoal = new();
    [ObservableProperty] private SmartLearningSuggestion? _smartSuggestion;
    [ObservableProperty] private bool _showStreakBanner;
    [ObservableProperty] private string _streakBannerText = "";
    [ObservableProperty] private bool _isOffline;
    [ObservableProperty] private DailyChallengeInfo? _dailyChallenge;
    public bool HasDailyChallenge => DailyChallenge is not null;

    [ObservableProperty] private CoachTip? _coachTip;
    [ObservableProperty] private DailyWordInfo? _dailyWord;
    public bool HasDailyWord => DailyWord is not null;
    // OnDailyWordChanged defined below with BuildSliderCards()
    public bool HasCoachTip => CoachTip is not null;
    partial void OnCoachTipChanged(CoachTip? v) => OnPropertyChanged(nameof(HasCoachTip));
    partial void OnDailyChallengeChanged(DailyChallengeInfo? value)
    {
        OnPropertyChanged(nameof(HasDailyChallenge));
        MainThread.BeginInvokeOnMainThread(BuildSliderCards);
    }

    public bool HasLastLesson => LastLesson is not null;
    public bool HasSmartSuggestion => SmartSuggestion is not null;

    partial void OnLastLessonChanged(LastLessonInfo? value) =>
        OnPropertyChanged(nameof(HasLastLesson));

    partial void OnSmartSuggestionChanged(SmartLearningSuggestion? value) =>
        OnPropertyChanged(nameof(HasSmartSuggestion));

    public HomeViewModel(
        ContentService contentService,
        ProfileService profileService,
        NavigationService navigationService,
        SmartLearningPathService smartLearningPathService,
        ConnectivityService connectivityService,
        DailyChallengeService dailyChallengeService,
        AiCoachService aiCoachService,
        DailyWordService dailyWordService,
        AudioService audioService)
    {
        _contentService = contentService;
        _profileService = profileService;
        _navigationService = navigationService;
        _smartLearningPathService = smartLearningPathService;
        _connectivityService = connectivityService;
        _dailyChallengeService = dailyChallengeService;
        _aiCoachService = aiCoachService;
        _dailyWordService = dailyWordService;
        _audioService = audioService;
        IsOffline = !connectivityService.IsOnline;
        connectivityService.ConnectivityChanged += (_, online) =>
            MainThread.BeginInvokeOnMainThread(() => IsOffline = !online);
    }

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
            var cats = await _contentService.GetCategoriesAsync(ActiveProfile);
            foreach (var cat in cats)
            {
                if (ActiveProfile.CategoryProgresses.TryGetValue(cat.Id, out var cp))
                    cat.ProgressPercent = cp.BestStars * 33;
            }
            Categories = cats;
            LastLesson = await _contentService.GetLastLessonAsync(ActiveProfile);
            DailyGoal = _profileService.GetDailyGoal(ActiveProfile);
            SmartSuggestion = await _smartLearningPathService.GetSuggestionAsync(ActiveProfile);
            DailyChallenge = _dailyChallengeService.GetTodayChallenge();
            DailyWord = await _dailyWordService.GetTodayWordAsync(ActiveProfile);

            // AI Koç: önbellekten hızlı yükle, arka planda güncelle
            CoachTip = _aiCoachService.GetCachedTip();
            _ = Task.Run(async () =>
            {
                var tip = await _aiCoachService.GetTodaysTipAsync();
                MainThread.BeginInvokeOnMainThread(() => CoachTip = tip);
            });

            // Streak banner
            var streak = ActiveProfile.StreakDays;
            if (streak >= 2)
            {
                StreakBannerText = streak switch
                {
                    >= 30 => $"🔥 {streak} günlük seri! Efsane!",
                    >= 14 => $"🔥 {streak} gün üst üste! İnanılmaz!",
                    >= 7  => $"🔥 {streak} gün üst üste! Harika!",
                    _     => $"🔥 {streak} gün üst üste devam ediyorsun!"
                };
                ShowStreakBanner = true;
            }
            else
            {
                ShowStreakBanner = false;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[HomeViewModel] Ana sayfa yüklenemedi: {ex}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ── Ana navigasyon ────────────────────────────────────────
    [RelayCommand]
    public Task GoToVocabularyAsync() => Shell.Current.GoToAsync("vocabulary");

    [RelayCommand]
    public Task GoToLeaderboardAsync() => Shell.Current.GoToAsync("leaderboard");

    [RelayCommand]
    public Task GoToProgressReportAsync() => Shell.Current.GoToAsync("progressreport");

    [RelayCommand]
    public Task GoToCoachTipAsync()
    {
        if (CoachTip?.ActionRoute is { Length: > 0 } route)
            return Shell.Current.GoToAsync(route);
        return Task.CompletedTask;
    }

    [RelayCommand]
    public Task GoToDailyChallengeAsync()
    {
        if (DailyChallenge is null) return Task.CompletedTask;
        return Shell.Current.GoToAsync(DailyChallenge.GameRoute);
    }

    [RelayCommand]
    public Task GoToGamesAsync() => _navigationService.GoToGamesAsync();

    [RelayCommand]
    public Task GoToSongsAsync() => _navigationService.GoToSongsAsync();

    [RelayCommand]
    public Task GoToParentalAsync() => _navigationService.GoToParentalAsync();

    [RelayCommand]
    public Task GoToCurriculumActivitiesAsync() => _navigationService.GoToCurriculumActivitiesAsync();

    [RelayCommand]
    public Task GoToAdventureMapAsync() => _navigationService.GoToAdventureMapAsync();

    [RelayCommand]
    public Task GoToSmartSuggestionAsync()
    {
        if (SmartSuggestion is null) return Task.CompletedTask;
        return _navigationService.GoToSmartSuggestionAsync(SmartSuggestion);
    }

    [RelayCommand]
    public Task ContinueLastLessonAsync()
    {
        if (LastLesson is null) return Task.CompletedTask;
        return _navigationService.GoToLessonAsync(LastLesson.LessonId);
    }

    [RelayCommand]
    public Task SelectCategoryAsync(Category category) =>
        _navigationService.GoToCategoryAsync(category.Id);

    [RelayCommand]
    public Task GoToLearningModulesAsync() =>
        Shell.Current.GoToAsync("learningmodules");

    [RelayCommand]
    public async Task SpeakDailyWordAsync()
    {
        if (DailyWord is not null)
            await _audioService.SpeakTextAsync(DailyWord.SpeakText);
    }

    // ── Tab bar navigasyon ────────────────────────────────────
    [RelayCommand]
    public Task GoToProfileSelectionAsync() => _navigationService.GoToProfileSelectionAsync();

    [RelayCommand]
    public Task GoToProgressAsync() => Shell.Current.GoToAsync("//progress");

    [RelayCommand]
    public Task GoToAchievementsAsync() => Shell.Current.GoToAsync("//achievements");

    [RelayCommand]
    public Task GoToDailyGoalAsync() => Shell.Current.GoToAsync("dailygoal");

    [RelayCommand]
    public Task GoToTalesAsync() => Shell.Current.GoToAsync("tales");

    [RelayCommand]
    public async Task SliderTapAsync(HomeSliderCard card)
    {
        if (string.IsNullOrEmpty(card.ActionRoute)) return;
        await Shell.Current.GoToAsync(card.ActionRoute);
    }

    // ── Slider kartları ──────────────────────────────────────
    [ObservableProperty] private List<HomeSliderCard> _sliderCards = new();

    public void BuildSliderCards()
    {
        var cards = new List<HomeSliderCard>();

        if (DailyWord is not null)
            cards.Add(new HomeSliderCard
            {
                Type = SliderCardType.DailyWord,
                Emoji = DailyWord.Emoji,
                IconSource = string.IsNullOrWhiteSpace(DailyWord.ImagePath)
                    ? "ui_learning_3d.png"
                    : DailyWord.ImagePath,
                Title = "Günün Kelimesi",
                Subtitle = DailyWord.NameTr,
                Detail = DailyWord.NameEn,
                GradientFrom = "#5148D4", GradientTo = "#3A86FF",
                ActionRoute = ""
            });

        if (DailyChallenge is not null && !DailyChallenge.IsCompleted)
            cards.Add(new HomeSliderCard
            {
                Type = SliderCardType.Challenge,
                IconSource = "ui_goal_3d.png",
                Emoji = "🎯",
                Title = "Günlük Meydan Okuma",
                Subtitle = $"{DailyChallenge.CategoryNameTr} – {DailyChallenge.GameTypeNameTr}",
                Detail = "Hemen başla!",
                GradientFrom = "#FF8C42", GradientTo = "#FF6B6B",
                ActionRoute = $"game?categoryId={DailyChallenge.CategoryId}"
            });

        cards.Add(new HomeSliderCard
        {
            Type = SliderCardType.Tales,
            IconSource = "ui_tales_3d.png",
            Emoji = "📖",
            Title = "Masallar",
            Subtitle = "Klasik çocuk masalları",
            Detail = "Dinle ve oku →",
            GradientFrom = "#FF6B6B", GradientTo = "#FF8C42",
            ActionRoute = "tales"
        });

        if (ActiveProfile is not null)
            cards.Add(new HomeSliderCard
            {
                Type = SliderCardType.Progress,
                IconSource = "ui_star_3d.png",
                Emoji = "⭐",
                Title = "İlerleme",
                Subtitle = $"{ActiveProfile.TotalStars} yıldız kazandın",
                Detail = ActiveProfile.LevelTitle,
                GradientFrom = "#F59E0B", GradientTo = "#D97706",
                ActionRoute = "//progress"
            });

        SliderCards = cards;
    }

    partial void OnDailyWordChanged(DailyWordInfo? v)
    {
        OnPropertyChanged(nameof(HasDailyWord));
        MainThread.BeginInvokeOnMainThread(BuildSliderCards);
    }
}
