using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KidsEducation.Models;
using KidsEducation.Services;

namespace KidsEducation.ViewModels.Games;

public partial class GamesViewModel : ObservableObject
{
    private readonly NavigationService _navigationService;
    private readonly ContentService _contentService;
    private readonly ProfileService _profileService;

    [ObservableProperty] private List<GameLauncherCard> _mixedGameCards = new();

    public GamesViewModel(NavigationService navigationService, ContentService contentService, ProfileService profileService)
    {
        _navigationService = navigationService;
        _contentService = contentService;
        _profileService = profileService;
    }

    [RelayCommand]
    public Task InitializeAsync()
    {
        MixedGameCards = new List<GameLauncherCard>
        {
            new() { Emoji = "🔤", Title = "Doğruyu Seç", Description = "Karışık sorularla hızlı tekrar", Route = "quizgame?categoryId=mixed", AccentColor = GameCardPalette.Accent(0), BackgroundColor = GameCardPalette.Background(0) },
            new() { Emoji = "🧠", Title = "Hafıza Oyunu", Description = "Görseli adıyla eşleştir", Route = "memorygamev2?categoryId=mixed", AccentColor = GameCardPalette.Accent(1), BackgroundColor = GameCardPalette.Background(1) },
            new() { Emoji = "🔊", Title = "Sesli Tahmin", Description = "Sesi dinle, görseli bul", Route = "soundgame?categoryId=mixed", AccentColor = GameCardPalette.Accent(2), BackgroundColor = GameCardPalette.Background(2) },
            new() { Emoji = "🔍", Title = "Yakınlaştırma", Description = "Kısmi resimden tahmin et", Route = "zoomgame?categoryId=mixed", AccentColor = GameCardPalette.Accent(3), BackgroundColor = GameCardPalette.Background(3) },
            new() { Emoji = "🎈", Title = "Balon Patlat", Description = "Hedefi hızlı bul, combo yap", Route = "balloongame?categoryId=mixed", AccentColor = GameCardPalette.Accent(4), BackgroundColor = GameCardPalette.Background(4) },
            new() { Emoji = "🔗", Title = "Eşleştir", Description = "Görseli ismiyle eşleştir", Route = "matchinggame?categoryId=mixed", AccentColor = GameCardPalette.Accent(5), BackgroundColor = GameCardPalette.Background(5) },
            new() { Emoji = "🎯", Title = "Bul & İşaretle", Description = "Doğru görselleri tek tek bul", Route = "findmarkgame?categoryId=mixed", AccentColor = GameCardPalette.Accent(6), BackgroundColor = GameCardPalette.Background(6) },
            new() { Emoji = "🃏", Title = "Flashcard", Description = "Kartı çevir, ismi öğren", Route = "flashcard?categoryId=mixed", AccentColor = GameCardPalette.Accent(7), BackgroundColor = GameCardPalette.Background(7) },
            new() { Emoji = "📊", Title = "Kavram Sırala", Description = "Öğeleri doğru sıraya diz", Route = "sortinggame?categoryId=mixed", AccentColor = GameCardPalette.Accent(8), BackgroundColor = GameCardPalette.Background(8) },
            new() { Emoji = "🎤", Title = "Telaffuz", Description = "Kelimeyi dinle ve söyle", Route = "pronunciationgame?categoryId=mixed", AccentColor = GameCardPalette.Accent(9), BackgroundColor = GameCardPalette.Background(9) },
            new() { Emoji = "🔢", Title = "Nokta Birleştir", Description = "Noktaları sırayla birleştir, şekli keşfet!", Route = "connectdots", AccentColor = GameCardPalette.Accent(10), BackgroundColor = GameCardPalette.Background(10) },
            new() { Emoji = "✏️", Title = "Çizim Tanıma", Description = "Parmağınla çiz, AI tanısın!", Route = "drawinggame", AccentColor = GameCardPalette.Accent(11), BackgroundColor = GameCardPalette.Background(11) },
            new() { Emoji = "🖼", Title = "Şekil Boyama", Description = "Örneğe bak, şekli renklerle boya!", Route = "shapecoloring", AccentColor = GameCardPalette.Accent(12), BackgroundColor = GameCardPalette.Background(12) },
            new() { Emoji = "👨‍👧", Title = "Aile Yarışması", Description = "Aynı Wi-Fi'de ebeveyn vs çocuk!", Route = "multiplayer", AccentColor = "#DB2777", BackgroundColor = "#FFF0F9" },
        };
        foreach (var card in MixedGameCards)
        {
            card.IconImage = GetMixedGameIcon(card.Route);
        }

        return Task.CompletedTask;
    }

    private static string GetMixedGameIcon(string route)
    {
        if (route.StartsWith("quizgame", StringComparison.OrdinalIgnoreCase)) return "ui_check_3d.png";
        if (route.StartsWith("memorygame", StringComparison.OrdinalIgnoreCase)) return "ui_games_3d.png";
        if (route.StartsWith("soundgame", StringComparison.OrdinalIgnoreCase)) return "ui_songs_3d.png";
        if (route.StartsWith("zoomgame", StringComparison.OrdinalIgnoreCase)) return "category_objects.png";
        if (route.StartsWith("balloongame", StringComparison.OrdinalIgnoreCase)) return "color_red.png";
        if (route.StartsWith("matchinggame", StringComparison.OrdinalIgnoreCase)) return "ui_goal_3d.png";
        if (route.StartsWith("findmarkgame", StringComparison.OrdinalIgnoreCase)) return "ui_check_3d.png";
        if (route.StartsWith("flashcard", StringComparison.OrdinalIgnoreCase)) return "ui_learning_3d.png";
        if (route.StartsWith("sortinggame", StringComparison.OrdinalIgnoreCase)) return "ui_progress_3d.png";
        if (route.StartsWith("pronunciationgame", StringComparison.OrdinalIgnoreCase)) return "ui_mic_3d.png";
        if (route.StartsWith("connectdots", StringComparison.OrdinalIgnoreCase)) return "number_three.png";
        if (route.StartsWith("drawinggame", StringComparison.OrdinalIgnoreCase)) return "shape_star.png";
        if (route.StartsWith("shapecoloring", StringComparison.OrdinalIgnoreCase)) return "category_colors.png";
        if (route.StartsWith("multiplayer", StringComparison.OrdinalIgnoreCase)) return "ui_parental_3d.png";
        return "ui_games_3d.png";
    }

    [RelayCommand]
    public Task OpenMixedGameAsync(GameLauncherCard card) =>
        Shell.Current.GoToAsync(card.Route);

    [RelayCommand]
    public Task GoToMixedReviewAsync() =>
        Shell.Current.GoToAsync("quizgame?categoryId=mixed");

    [RelayCommand]
    public Task GoBackAsync() => _navigationService.GoBackAsync();
}

public partial class GameCategoryChip : ObservableObject
{
    public string Id { get; set; } = string.Empty;
    public string NameTr { get; set; } = string.Empty;
    public string Emoji { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string AccentColor { get; set; } = "#6C62F5";
    public string BackgroundColor { get; set; } = "#EEF0FF";
    [ObservableProperty] private bool _isSelected;
}

public static class GameCardPalette
{
    private static readonly string[] Accents =
    [
        "#6C62F5", "#0EA5A3", "#EA580C", "#2563EB", "#DB2777",
        "#16A34A", "#C026D3", "#D97706", "#0891B2", "#7C3AED"
    ];

    private static readonly string[] Backgrounds =
    [
        "#EEF0FF", "#E6FBF7", "#FEF3E8", "#EBF3FF", "#FFF0F9",
        "#EAFBF1", "#FAE8FF", "#FEF9E6", "#E0F7FA", "#F5F0FF"
    ];

    public static string Accent(int index) => Accents[index % Accents.Length];
    public static string Background(int index) => Backgrounds[index % Backgrounds.Length];
}
