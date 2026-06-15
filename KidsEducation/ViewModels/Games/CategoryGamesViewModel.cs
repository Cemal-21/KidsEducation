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
        }

        return cards;
    }

    private static GameLauncherCard Card(string emoji, string title, string description, string route, int index) => new()
    {
        Emoji = emoji,
        Title = title,
        Description = description,
        Route = route,
        AccentColor = GameCardPalette.Accent(index),
        BackgroundColor = GameCardPalette.Background(index)
    };
}

public class GameLauncherCard
{
    public string Emoji { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public string AccentColor { get; set; } = "#6C62F5";
    public string BackgroundColor { get; set; } = "#EEF0FF";
}
