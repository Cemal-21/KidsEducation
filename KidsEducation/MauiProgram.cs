using KidsEducation.Services;
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
using KidsEducation.Views.Achievements;
using KidsEducation.Views.Curriculum;
using KidsEducation.Views.Game;
using KidsEducation.Views.Home;
using KidsEducation.Views.Learning;
using KidsEducation.Views.Profile;
using KidsEducation.Views.Progress;
using KidsEducation.Views.Result;
using KidsEducation.Views.Parental;
using KidsEducation.Views.Settings;
using KidsEducation.Views.Songs;
using KidsEducation.ViewModels.Parental;
using Plugin.Maui.Audio;
using Microsoft.Extensions.Logging;

namespace KidsEducation;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
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
        builder.Services.AddSingleton<AppPreferencesService>();
        builder.Services.AddSingleton<AudioService>();
        builder.Services.AddSingleton<BadgeService>();
        builder.Services.AddSingleton<AnimationService>();
        builder.Services.AddSingleton<SongService>();
        builder.Services.AddSingleton<LearningEventService>();
        builder.Services.AddSingleton<AdaptiveDifficultyEngine>();
        builder.Services.AddSingleton(AudioManager.Current);

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


        // Pages
        builder.Services.AddTransient<ParentalPage>();
        builder.Services.AddTransient<AchievementsPage>();
        builder.Services.AddTransient<ProgressPage>();
        builder.Services.AddTransient<ProfilePage>();

        return builder.Build();
    }
}
