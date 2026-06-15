namespace KidsEducation.Services;

public class AssistantService
{
    private static readonly Dictionary<string, string[]> PageTips = new()
    {
        ["home"] = new[]
        {
            "Merhaba. Bugün ne öğrenmek istersin?",
            "Oyunlar bölümünde kısa ve eğlenceli etkinlikler var.",
            "Mikrofona dokunup hayvanlar, oyunlar veya şarkılar diyebilirsin.",
            "Konular bölümünden istediğin kategoriyi hızlıca açabilirim.",
            "Yazı alanına gitmek istediğin bölümü yazman yeterli."
        },
        ["games"] = new[]
        {
            "Hangi oyunu denemek istersin?",
            "Nokta birleştir, çizim ya da kelime oyunlarını açabilirim.",
            "Bir oyuna gitmek için adını söylemen yeterli.",
            "İstersen ana sayfaya da dönebiliriz."
        },
        ["learningmodules"] = new[]
        {
            "Hangi konuyu öğrenmek istersin?",
            "Hayvanlar, meyveler, sayılar veya harfler diyebilirsin.",
            "Her kategoride resimler, sesler ve oyunlar seni bekliyor.",
            "İstersen aradığın konuyu yazı alanına da yazabilirsin."
        },
        ["category"] = new[]
        {
            "Resimlere bakıp kelimeleri öğrenebilirsin.",
            "Bir kelimeye dokununca sesini dinleyebilirsin.",
            "Bu kategoriyle ilgili oyunları da açabilirim.",
            "Başka bir kategoriye geçmek istersen adını söyle."
        },
        ["connectdots"] = new[]
        {
            "Numaraları sırayla birleştir. Birden başla.",
            "Parmağını bir sonraki noktaya götür.",
            "Tüm noktaları birleştirince şekil tamamlanacak."
        },
        ["drawinggame"] = new[]
        {
            "Parmağınla şekli çiz, sonra tahmin ettir.",
            "Çizimi temiz yapmak tahmini kolaylaştırır.",
            "Baştan denemek istersen temizle düğmesine dokun."
        },
        ["multiplayer"] = new[]
        {
            "Aile yarışması için iki cihazın aynı Wi-Fi ağında olması gerekir.",
            "Ebeveyn soruları hazırlar, çocuk cevaplar.",
            "Bağlantı sorununda ana sayfaya dönüp yeniden deneyebilirsin."
        },
        ["default"] = new[]
        {
            "Sana yardım etmek için buradayım.",
            "Mikrofona dokunup ne yapmak istediğini söyleyebilirsin.",
            "Yazı alanına bölüm adını yazıp hızlıca gidebilirsin.",
            "Hayvanlar, oyunlar, şarkılar, konular veya ana sayfa diyebilirsin."
        },
    };

    private readonly Random _rng = new();

    public string GetTip(string pageKey)
    {
        var tips = PageTips.TryGetValue(pageKey, out var pageTips)
            ? pageTips
            : PageTips["default"];

        return tips[_rng.Next(tips.Length)];
    }

    public string GetVoiceExampleCommand(string pageKey) => pageKey switch
    {
        "home" => "\"Oyunlar\" veya \"Hayvanlar\" de.",
        "games" => "\"Nokta birleştir\" veya \"Çizim\" de.",
        "learningmodules" => "\"Meyveler\" veya \"Renkler\" de.",
        _ => "\"Ana sayfa\" veya \"Geri dön\" de."
    };
}
