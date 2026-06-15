namespace KidsEducation.Services;

public class AssistantService
{
    private static readonly Dictionary<string, string[]> PageTips = new()
    {
        ["home"] = new[]
        {
            "Merhaba! Bugün ne öğrenmek istersin? 🌟",
            "Günün kelimesine baktın mı? Hemen incele!",
            "Oyunlar bölümünde yeni maceralar seni bekliyor 🎮",
            "Mikrofona tıkla ve 'Hayvanlar' de — seni oraya götüreyim!",
            "Konular bölümünden istediğin kategoriyi seçebilirsin 📚",
        },
        ["games"] = new[]
        {
            "Hangi oyunu denemek istersin? Hepsini dene! 🎯",
            "Nokta Birleştir'i denedin mi? Çok eğlenceli!",
            "Çizim Tanıma'da şekil çizebilirsin ✏️",
            "Aile Yarışması'nda ebeveynine meydan oku! 👨‍👧",
        },
        ["learningmodules"] = new[]
        {
            "Hangi konuyu öğrenmek istersin? 🐾",
            "Hayvanlar kategorisini denedin mi?",
            "Her kategoride eğlenceli kelimeler var! 🌈",
        },
        ["category"] = new[]
        {
            "Resimlere bak ve kelimeleri öğren! 👀",
            "Bir kelimeye tıkla, sesini dinle 🔊",
            "Buradan oyun oynayabilirsin de!",
        },
        ["connectdots"] = new[]
        {
            "Numaraları sırayla birleştir! 1'den başla ☝️",
            "Parmağını bir sonraki noktaya götür",
            "Tüm noktaları birleştirince bir şekil çıkacak! 🎨",
        },
        ["drawinggame"] = new[]
        {
            "Parmağınla şekli çiz, sonra 'Tahmin Et'e bas! ✏️",
            "Temiz bir şekil çiz, AI daha kolay tanır",
            "Çizimi temizlemek için çöp kutusuna bas 🗑",
        },
        ["multiplayer"] = new[]
        {
            "Ebeveyn telefona IP adresini gir, çocuk bağlansın! 📡",
            "Aynı Wi-Fi ağında olmanız gerekiyor",
            "Ebeveyn soruları hazırlar, çocuk cevaplar! 🏆",
        },
        ["default"] = new[]
        {
            "Sana yardım etmemi ister misin? 🤗",
            "Mikrofona tıkla ve ne yapmak istediğini söyle!",
            "Buraya tıklayarak bana soru sorabilirsin 💬",
        },
    };

    private readonly Random _rng = new();

    public string GetTip(string pageKey)
    {
        var tips = PageTips.TryGetValue(pageKey, out var t) ? t : PageTips["default"];
        return tips[_rng.Next(tips.Length)];
    }

    public string GetVoiceExampleCommand(string pageKey) => pageKey switch
    {
        "home"           => "\"Oyunlar\" veya \"Hayvanlar\" de",
        "games"          => "\"Nokta birleştir\" veya \"Çizim\" de",
        "learningmodules"=> "\"Meyveler\" veya \"Renkler\" de",
        _                => "\"Ana sayfa\" veya \"Geri dön\" de",
    };
}
