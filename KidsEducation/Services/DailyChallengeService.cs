using KidsEducation.Enums;

namespace KidsEducation.Services;

public class DailyChallengeInfo
{
    public string CategoryId { get; set; } = string.Empty;
    public string CategoryNameTr { get; set; } = string.Empty;
    public string CategoryEmoji { get; set; } = string.Empty;
    public GameType GameType { get; set; } = GameType.MatchName;
    public string GameTypeNameTr { get; set; } = string.Empty;
    public string GameRoute { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public string DateKey { get; set; } = string.Empty;
}

public class DailyChallengeService
{
    private const string CompletedKeyPrefix = "daily_challenge_done_";

    private static readonly (string Id, string NameTr, string Emoji)[] Categories =
    [
        ("animals",     "Hayvanlar",  "🐾"),
        ("colors",      "Renkler",    "🎨"),
        ("fruits",      "Meyveler",   "🍎"),
        ("vegetables",  "Sebzeler",   "🥕"),
        ("vehicles",    "Taşıtlar",   "🚗"),
        ("shapes",      "Şekiller",   "🔷"),
        ("numbers",     "Sayılar",    "🔢"),
        ("letters",     "Harfler",    "🔤"),
        ("body",        "Vücudum",    "🫀"),
        ("seasons",     "Mevsimler",  "🌸"),
        ("professions", "Meslekler",  "💼"),
        ("countries",   "Ülkeler",    "🌍"),
        ("planets",     "Gezegenler", "🪐"),
        ("cities",      "İller",      "🏙️"),
        ("nature",      "Doğa",       "🌿"),
        ("weather",     "Hava Durumu","☀️"),
        ("opposites",   "Zıt Kavramlar", "↔️"),
        ("objects",     "Günlük Eşyalar", "🎒"),
        ("traffic",     "Trafik İşaretleri", "🚦"),
    ];

    private static readonly (GameType Type, string NameTr, string RouteTemplate)[] GameTypes =
    [
        (GameType.MatchName,  "Adını Bul",      "game?categoryId={0}&gameType=MatchName"),
        (GameType.ZoomGuess,  "Zoom Tahmin",    "zoomgame?categoryId={0}"),
        (GameType.SoundGuess, "Sesi Dinle",     "soundgame?categoryId={0}"),
        (GameType.Matching,   "Eşleştir",       "matchinggame?categoryId={0}"),
        (GameType.FindAndMark,"Bul & İşaretle", "findmarkgame?categoryId={0}"),
    ];

    public DailyChallengeInfo GetTodayChallenge()
    {
        var today = DateTime.Now.Date;
        var dateKey = today.ToString("yyyy-MM-dd");

        // Tarih tohumunu kullanarak deterministik rastgele seçim — her gün farklı ama tutarlı
        var seed = today.Year * 10000 + today.Month * 100 + today.Day;
        var rng = new Random(seed);

        var cat = Categories[rng.Next(Categories.Length)];
        var game = GameTypes[rng.Next(GameTypes.Length)];

        var route = string.Format(game.RouteTemplate, Uri.EscapeDataString(cat.Id));

        var isCompleted = Preferences.Get(CompletedKeyPrefix + dateKey, false);

        return new DailyChallengeInfo
        {
            CategoryId = cat.Id,
            CategoryNameTr = cat.NameTr,
            CategoryEmoji = cat.Emoji,
            GameType = game.Type,
            GameTypeNameTr = game.NameTr,
            GameRoute = route,
            IsCompleted = isCompleted,
            DateKey = dateKey
        };
    }

    public void MarkCompleted(string dateKey)
    {
        Preferences.Set(CompletedKeyPrefix + dateKey, true);
    }

    public bool IsTodayCompleted()
    {
        var dateKey = DateTime.Now.Date.ToString("yyyy-MM-dd");
        return Preferences.Get(CompletedKeyPrefix + dateKey, false);
    }
}
