using KidsEducation.Services;
using KidsEducation.ViewModels.Tales;
using KidsEducation.Views.Tales;
using KidsEducation.ViewModels.Adventure;
using KidsEducation.ViewModels.Achievements;
using KidsEducation.ViewModels.Curriculum;
using KidsEducation.ViewModels.Game;
using KidsEducation.ViewModels.Home;
using KidsEducation.ViewModels.Learning;
using KidsEducation.ViewModels.Profile;
using KidsEducation.ViewModels.Progress;
using KidsEducation.ViewModels.Result;
using KidsEducation.ViewModels.Settings;
using KidsEducation.ViewModels.Songs;
using KidsEducation.Views.Adventure;
using KidsEducation.Views.Achievements;
using KidsEducation.Views.Curriculum;
using KidsEducation.ViewModels.Game;
using KidsEducation.Views.Game;
using KidsEducation.Views.Home;
using KidsEducation.Views.Learning;
using KidsEducation.Views.Profile;
using KidsEducation.Views.Progress;
using KidsEducation.Views.Result;
using KidsEducation.Views.Games;
using KidsEducation.Views.Onboarding;
using KidsEducation.Views.Parental;
using KidsEducation.Views.Settings;
using KidsEducation.Views.Songs;
using KidsEducation.ViewModels.Games;
using KidsEducation.ViewModels.Game;
using KidsEducation.ViewModels.Vocabulary;
using KidsEducation.ViewModels.Story;
using KidsEducation.Views.Story;
using KidsEducation.Views.Vocabulary;
using KidsEducation.ViewModels.Leaderboard;
using KidsEducation.ViewModels.Pronunciation;
using KidsEducation.Views.Pronunciation;
using KidsEducation.ViewModels.Report;
using KidsEducation.Views.Report;
using KidsEducation.Views.Leaderboard;
using KidsEducation.ViewModels.Parental;
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Media;
using Plugin.Maui.Audio;
using Plugin.LocalNotification;
using Microsoft.Extensions.Logging;

namespace KidsEducation;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseLocalNotification()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Servisler
        builder.Services.AddSingleton<SkillCatalogService>();
        builder.Services.AddSingleton<CurriculumActivityService>();
        builder.Services.AddSingleton<SmartLearningPathService>();
        builder.Services.AddSingleton<ProfileService>();
        builder.Services.AddSingleton<ContentService>();
        builder.Services.AddSingleton<GameService>();
        builder.Services.AddSingleton<NavigationService>();
        builder.Services.AddTransient<KidsEducation.ViewModels.Parental.PinEntryViewModel>();
        builder.Services.AddTransient<KidsEducation.ViewModels.Parental.PinSetupViewModel>();
        builder.Services.AddTransient<KidsEducation.Views.Parental.PinEntryPage>();
        builder.Services.AddTransient<KidsEducation.Views.Parental.PinSetupPage>();
        builder.Services.AddSingleton<AppPreferencesService>();
        builder.Services.AddSingleton<AudioService>();
        builder.Services.AddSingleton<BadgeService>();
        builder.Services.AddSingleton<AnimationService>();
        builder.Services.AddSingleton<SongService>();
        builder.Services.AddSingleton<LearningEventService>();
        builder.Services.AddSingleton<NotificationService>();
        builder.Services.AddSingleton<ConnectivityService>();
        builder.Services.AddSingleton<DailyChallengeService>();
        builder.Services.AddSingleton<DailyWordService>();
        builder.Services.AddSingleton<AssistantService>();
        builder.Services.AddSingleton<VoiceCommandService>();
        builder.Services.AddSingleton<DotShapeService>();
        builder.Services.AddSingleton<DrawingRecognitionService>();
        builder.Services.AddTransient<MultiplayerService>();
        builder.Services.AddSingleton<AiCoachService>();
        builder.Services.AddSingleton<AdaptiveDifficultyEngine>();
        builder.Services.AddSingleton<ModuleProgressService>();
        builder.Services.AddSingleton(AudioManager.Current);
        builder.Services.AddSingleton<ISpeechToText>(SpeechToText.Default);

        // ViewModels
        builder.Services.AddTransient<ProfileSelectionViewModel>();
        builder.Services.AddTransient<HomeViewModel>();
        builder.Services.AddTransient<CategoryItemsViewModel>();
        builder.Services.AddTransient<ItemDetailViewModel>();
        builder.Services.AddTransient<GameViewModel>();
        builder.Services.AddTransient<ResultViewModel>();
        builder.Services.AddTransient<ParentalViewModel>();
        builder.Services.AddTransient<PreferencesViewModel>();
        builder.Services.AddTransient<SongsViewModel>();
        builder.Services.AddTransient<SongDetailViewModel>();
        builder.Services.AddTransient<CurriculumActivitiesViewModel>();

        // Views
        builder.Services.AddTransient<ProfileSelectionPage>();
        builder.Services.AddTransient<HomePage>();
        builder.Services.AddTransient<CategoryItemsPage>();
        builder.Services.AddTransient<ItemDetailPage>();
        builder.Services.AddTransient<GamePage>();
        builder.Services.AddTransient<ResultPage>();
        builder.Services.AddTransient<SongsPage>();
        builder.Services.AddTransient<SongDetailPage>();
        builder.Services.AddTransient<PreferencesPage>();
        builder.Services.AddTransient<MemoryGamePage>();
        builder.Services.AddTransient<ZoomGamePage>();
        builder.Services.AddTransient<SoundGamePage>();
        builder.Services.AddTransient<BalloonGamePage>();
        builder.Services.AddTransient<SequenceGamePage>();
        builder.Services.AddTransient<StoryGamePage>();
        builder.Services.AddTransient<CurriculumActivitiesPage>();
        builder.Services.AddTransient<TracingGamePage>();
        builder.Services.AddTransient<PuzzleGamePage>();
        builder.Services.AddTransient<QuizGamePage>();
        builder.Services.AddTransient<LetterDropGamePage>();
        builder.Services.AddTransient<MathGamePage>();
        builder.Services.AddTransient<WordScrambleGamePage>();
        builder.Services.AddTransient<GamesPage>();

        // ViewModels
        builder.Services.AddTransient<AchievementsViewModel>();
        builder.Services.AddTransient<ProgressViewModel>();
        builder.Services.AddTransient<ProfileViewModel>();
        builder.Services.AddTransient<MemoryGameViewModel>();
        builder.Services.AddTransient<ZoomGameViewModel>();
        builder.Services.AddTransient<SoundGameViewModel>();
        builder.Services.AddTransient<BalloonGameViewModel>();
        builder.Services.AddTransient<SequenceGameViewModel>();
        builder.Services.AddTransient<StoryGameViewModel>();
        builder.Services.AddTransient<TracingGameViewModel>();
        builder.Services.AddTransient<PuzzleGameViewModel>();
        builder.Services.AddTransient<QuizGameViewModel>();
        builder.Services.AddTransient<LetterDropGameViewModel>();
        builder.Services.AddTransient<MathGameViewModel>();
        builder.Services.AddTransient<WordScrambleGameViewModel>();
        builder.Services.AddTransient<GamesViewModel>();
        builder.Services.AddTransient<CategoryGamesViewModel>();
        builder.Services.AddTransient<DailyGoalViewModel>();
        builder.Services.AddTransient<MatchingGameViewModel>();
        builder.Services.AddTransient<FindMarkGameViewModel>();
        builder.Services.AddTransient<ColoringGameViewModel>();
        builder.Services.AddTransient<FlashcardViewModel>();
        builder.Services.AddTransient<MemoryGameV2ViewModel>();
        builder.Services.AddTransient<SortingGameViewModel>();
        builder.Services.AddTransient<VocabularyViewModel>();
        builder.Services.AddTransient<StoryReaderViewModel>();
        builder.Services.AddTransient<LeaderboardViewModel>();
        builder.Services.AddTransient<ProgressReportViewModel>();
        builder.Services.AddTransient<PronunciationGameViewModel>();
        builder.Services.AddTransient<AdventureMapViewModel>();

        // Pages
        builder.Services.AddTransient<OnboardingPage>();
        builder.Services.AddTransient<DailyGoalPage>();
        builder.Services.AddTransient<MatchingGamePage>();
        builder.Services.AddTransient<FindMarkGamePage>();
        builder.Services.AddTransient<ColoringGamePage>();
        builder.Services.AddTransient<FlashcardPage>();
        builder.Services.AddTransient<MemoryGameV2Page>();
        builder.Services.AddTransient<SortingGamePage>();
        builder.Services.AddTransient<VocabularyPage>();
        builder.Services.AddTransient<StoryReaderPage>();
        builder.Services.AddTransient<LeaderboardPage>();
        builder.Services.AddTransient<ProgressReportPage>();
        builder.Services.AddTransient<PronunciationGamePage>();
        builder.Services.AddTransient<ParentalPage>();
        builder.Services.AddTransient<AchievementsPage>();
        builder.Services.AddTransient<ProgressPage>();
        builder.Services.AddTransient<ProfilePage>();
        builder.Services.AddTransient<AdventureMapPage>();
        builder.Services.AddTransient<CategoryGamesPage>();
        builder.Services.AddTransient<LearningModulesViewModel>();
        builder.Services.AddTransient<LearningModulesPage>();
        builder.Services.AddTransient<ConnectDotsGameViewModel>();
        builder.Services.AddTransient<ConnectDotsGamePage>();
        builder.Services.AddTransient<DrawingGameViewModel>();
        builder.Services.AddTransient<DrawingGamePage>();
        builder.Services.AddTransient<MultiplayerViewModel>();
        builder.Services.AddTransient<MultiplayerPage>();
        builder.Services.AddTransient<ParentalDashboardViewModel>();
        builder.Services.AddTransient<KidsEducation.Views.Parental.ParentalDashboardPage>();
        builder.Services.AddSingleton<ShareService>();
        builder.Services.AddSingleton<ProgressBackupService>();
        builder.Services.AddSingleton<TaleService>();
        builder.Services.AddTransient<TalesViewModel>();
        builder.Services.AddTransient<TaleReaderViewModel>();
        builder.Services.AddTransient<TalesPage>();
        builder.Services.AddTransient<TaleReaderPage>();

        return builder.Build();
    }
}
