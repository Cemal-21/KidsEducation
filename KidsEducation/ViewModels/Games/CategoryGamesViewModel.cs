using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KidsEducation.Models;
using KidsEducation.Services;

namespace KidsEducation.ViewModels.Games;

public partial class CategoryGamesViewModel : ObservableObject
{
    private readonly ContentService _contentService;
    private readonly ProfileService _profileService;
    private readonly NavigationService _navigationService;

    [ObservableProperty] private string _categoryId = "animals";
    [ObservableProperty] private string _categoryName = "Konu";
    [ObservableProperty] private string _categoryImage = "ui_learning_3d.png";
    [ObservableProperty] private string _categoryEmoji = "🎮";
    [ObservableProperty] private List<GameLauncherCard> _gameCards = new();

    public CategoryGamesViewModel(
        ContentService contentService,
        ProfileService profileService,
        NavigationService navigationService)
    {
        _contentService = contentService;
        _profileService = profileService;
        _navigationService = navigationService;
    }

    [RelayCommand]
    public async Task InitializeAsync(string? categoryId)
    {
        CategoryId = string.IsNullOrWhiteSpace(categoryId) ? "animals" : categoryId;

        var profile = _profileService.GetActiveProfile();
        var categories = profile is not null
            ? await _contentService.GetCategoriesAsync(profile)
            : new List<Category>();

        var category = categories.FirstOrDefault(c => c.Id == CategoryId);
        CategoryName = category?.NameTr ?? CategoryId;
        CategoryImage = string.IsNullOrWhiteSpace(category?.Image)
            ? "ui_learning_3d.png"
            : category.Image;
        CategoryEmoji = category?.Emoji ?? "🎮";
        GameCards = BuildCards(CategoryId);
    }

    [RelayCommand]
    public Task OpenGameAsync(GameLauncherCard card) =>
        Shell.Current.GoToAsync(card.Route);

    [RelayCommand]
    public Task GoBackAsync() => _navigationService.GoBackAsync();

    private static List<GameLauncherCard> BuildCards(string categoryId)
    {
        var category = Uri.EscapeDataString(categoryId);
        var cards = new List<GameLauncherCard>
        {
            Card("🔤", "Doğruyu Seç", "Resme bak, doğru seçeneği bul", $"quizgame?categoryId={category}", 0),
            Card("🧠", "Hafıza Kartları", "Görseli adıyla eşleştir", $"memorygamev2?categoryId={category}", 1),
            Card("🔍", "Yakınlaştırma", "Kısmi resimden doğruyu tahmin et", $"zoomgame?categoryId={category}", 2),
            Card("🔊", "Sesli Tahmin", "İpucunu dinle, görseli seç", $"soundgame?categoryId={category}", 3),
            Card("🎈", "Balon Patlat", "Hedefi hızlı bul, combo yap", $"balloongame?categoryId={category}", 4),
            Card("📖", "Hikaye Modu", "Oku, anla, doğruyu seç", $"storygame?categoryId={category}", 5),
            Card("🧩", "Puzzle", "Parçaları doğru yerine getir", $"puzzlegame?categoryId={category}", 6),
            Card("🃏", "Flashcard", "Kartı çevir, ismi öğren", $"flashcard?categoryId={category}", 7),
            Card("🔗", "Eşleştir", "Görseli ismiyle eşleştir", $"matchinggame?categoryId={category}", 8),
            Card("🎯", "Bul & İşaretle", "Doğru görselleri tek tek bul", $"findmarkgame?categoryId={category}", 9),
            Card("⏱", "Zaman Yarışı", "Süre dolmadan doğru cevabı bul", $"game?categoryId={category}&gameType=MatchName&timed=true", 10),
            Card("📊", "Kavram Sırala", "Öğeleri doğru sıraya diz", $"sortinggame?categoryId={category}", 11),
            Card("🎤", "Telaffuz", "Kelimeyi dinle ve söyle", $"pronunciationgame?categoryId={category}", 12)
        };

        if (categoryId == "numbers")
        {
            cards.Add(Card("🔢", "Sayı Sırası", "Sayıları doğru sırayla diz", "sequencegame", 13));
            cards.Add(Card("➕", "Matematik", "Toplama ve çıkarma işlemleri", "mathgame", 14));
        }

        if (categoryId == "letters")
        {
            cards.Add(Card("✏️", "Harf İzleme", "Harfleri parmağınla çiz", $"tracinggame?categoryId={category}", 13));
            cards.Add(Card("🔡", "Harf Yerleştirme", "Eksik harfi bul", $"letterdrop?categoryId={category}", 14));
            cards.Add(Card("🔀", "Kelime Bul", "Karışık harfleri sıraya diz", $"wordscramble?categoryId={category}", 15));
        }

        if (categoryId == "colors")
        {
            cards.Add(Card("🎨", "Boyama", "Bölgelere renk seç", "coloringgame", 13));
            cards.Add(Card("🖼", "Şekil Boyama", "Örneğe bak, şekli boya", "shapecoloring", 14));
        }

        return cards;
    }

    private static GameLauncherCard Card(string emoji, string title, string description, string route, int index) => new()
    {
        Emoji = emoji,
        Title = title,
        Description = description,
        Route = route,
        IconImage = GetGameIcon(title),
        AccentColor = GameCardPalette.Accent(index),
        BackgroundColor = GameCardPalette.Background(index)
    };

    private static string GetGameIcon(string title) => title switch
    {
        "DoÄŸruyu SeÃ§" => "ui_check_3d.png",
        "HafÄ±za KartlarÄ±" => "ui_games_3d.png",
        "YakÄ±nlaÅŸtÄ±rma" => "category_objects.png",
        "Sesli Tahmin" => "ui_songs_3d.png",
        "Balon Patlat" => "color_red.png",
        "Hikaye Modu" => "ui_tales_3d.png",
        "Puzzle" => "shape_square.png",
        "Flashcard" => "ui_learning_3d.png",
        "EÅŸleÅŸtir" => "ui_goal_3d.png",
        "Bul & Ä°ÅŸaretle" => "ui_check_3d.png",
        "Zaman YarÄ±ÅŸÄ±" => "object_clock.png",
        "Kavram SÄ±rala" => "ui_progress_3d.png",
        "Telaffuz" => "ui_mic_3d.png",
        "SayÄ± SÄ±rasÄ±" => "number_three.png",
        "Matematik" => "number_ten.png",
        "Harf Ä°zleme" => "letter_a.png",
        "Harf YerleÅŸtirme" => "letter_b.png",
        "Kelime Bul" => "letter_k.png",
        "Boyama" => "category_colors.png",
        "Åekil Boyama" => "shape_star.png",
        _ => "ui_games_3d.png"
    };
}

public class GameLauncherCard
{
    public string Emoji { get; set; } = string.Empty;
    public string IconImage { get; set; } = "ui_games_3d.png";
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public string AccentColor { get; set; } = "#6C62F5";
    public string BackgroundColor { get; set; } = "#EEF0FF";
}
