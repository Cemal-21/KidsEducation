using KidsEducation.Views.Curriculum;
using KidsEducation.Views.Game;
using KidsEducation.Views.Home;
using KidsEducation.Views.Learning;
using KidsEducation.Views.Result;
using KidsEducation.Views.Settings;
using KidsEducation.Views.Songs;

namespace KidsEducation;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        RegisterRoutes();
    }

    private static void RegisterRoutes()
    {
        Routing.RegisterRoute("category", typeof(CategoryItemsPage));
        Routing.RegisterRoute("itemdetail", typeof(ItemDetailPage));
        Routing.RegisterRoute("game", typeof(GamePage));
        Routing.RegisterRoute("memorygame", typeof(MemoryGamePage));
        Routing.RegisterRoute("result", typeof(ResultPage));
        Routing.RegisterRoute("songs", typeof(SongsPage));
        Routing.RegisterRoute("songdetail", typeof(SongDetailPage));
        Routing.RegisterRoute("zoomgame", typeof(ZoomGamePage));
        Routing.RegisterRoute("soundgame", typeof(SoundGamePage));
        Routing.RegisterRoute("balloongame", typeof(BalloonGamePage));
        Routing.RegisterRoute("sequencegame", typeof(SequenceGamePage));
        Routing.RegisterRoute("storygame", typeof(StoryGamePage));
        Routing.RegisterRoute("curriculumactivities", typeof(CurriculumActivitiesPage));
        Routing.RegisterRoute("preferences", typeof(PreferencesPage));
    }
}
