using KidsEducation.Views.Curriculum;
using KidsEducation.Views.Game;
using KidsEducation.Views.Games;
using KidsEducation.Views.Home;
using KidsEducation.Views.Learning;
using KidsEducation.Views.Result;
using KidsEducation.Views.Settings;
using KidsEducation.Views.Songs;
using KidsEducation.Views.Parental;
using KidsEducation.Views.Vocabulary;
using KidsEducation.Views.Story;
using KidsEducation.Views.Leaderboard;
using KidsEducation.Views.Report;
using KidsEducation.Views.Pronunciation;
using KidsEducation.Views.Adventure;
using KidsEducation.Views.Tales;

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
        Routing.RegisterRoute("tracinggame", typeof(TracingGamePage));
        Routing.RegisterRoute("puzzlegame", typeof(PuzzleGamePage));
        Routing.RegisterRoute("quizgame", typeof(QuizGamePage));
        Routing.RegisterRoute("letterdrop", typeof(LetterDropGamePage));
        Routing.RegisterRoute("mathgame", typeof(MathGamePage));
        Routing.RegisterRoute("wordscramble", typeof(WordScrambleGamePage));
        Routing.RegisterRoute("games", typeof(GamesPage));
        Routing.RegisterRoute("categorygames", typeof(CategoryGamesPage));
        Routing.RegisterRoute("learningmodules", typeof(LearningModulesPage));
        Routing.RegisterRoute("dailygoal", typeof(DailyGoalPage));
        Routing.RegisterRoute("matchinggame", typeof(MatchingGamePage));
        Routing.RegisterRoute("findmarkgame", typeof(FindMarkGamePage));
        Routing.RegisterRoute("coloringgame", typeof(ColoringGamePage));
        Routing.RegisterRoute("flashcard", typeof(FlashcardPage));
        Routing.RegisterRoute("memorygamev2", typeof(MemoryGameV2Page));
        Routing.RegisterRoute("sortinggame", typeof(SortingGamePage));
        Routing.RegisterRoute("vocabulary", typeof(VocabularyPage));
        Routing.RegisterRoute("storyreader", typeof(StoryReaderPage));
        Routing.RegisterRoute("leaderboard", typeof(LeaderboardPage));
        Routing.RegisterRoute("progressreport", typeof(ProgressReportPage));
        Routing.RegisterRoute("pronunciationgame", typeof(PronunciationGamePage));
        Routing.RegisterRoute("adventuremap", typeof(AdventureMapPage));
        Routing.RegisterRoute("pinentry", typeof(PinEntryPage));
        Routing.RegisterRoute("pinsetup", typeof(PinSetupPage));
        Routing.RegisterRoute("connectdots", typeof(ConnectDotsGamePage));
        Routing.RegisterRoute("drawinggame", typeof(DrawingGamePage));
        Routing.RegisterRoute("multiplayer", typeof(MultiplayerPage));
        Routing.RegisterRoute("parentaldashboard", typeof(KidsEducation.Views.Parental.ParentalDashboardPage));
        Routing.RegisterRoute("tales", typeof(TalesPage));
        Routing.RegisterRoute("talereader", typeof(TaleReaderPage));
    }
}
