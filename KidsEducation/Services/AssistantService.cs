namespace KidsEducation.Services;

public class AssistantService
{
    private static readonly Dictionary<string, string[]> PageTips = new()
    {
        ["home"] =
        [
            "Merhaba. Bugün ne öğrenmek istersin?",
            "Günlük planını, oyunları, masalları veya konuları açabilirim.",
            "Mikrofona dokunup hayvanlar, oyunlar, günlük görev veya masallar diyebilirsin.",
            "İstersen zorlandığın konuları birlikte tekrar edebiliriz."
        ],
        ["games"] =
        [
            "Hangi oyunu denemek istersin?",
            "Çizim, boyama, matematik, hafıza veya eşleştirme diyebilirsin.",
            "Bir oyuna gitmek için adını söylemen yeterli.",
            "İstersen günlük görevlerine de dönebiliriz."
        ],
        ["learningmodules"] =
        [
            "Hangi konuyu öğrenmek istersin?",
            "Hayvanlar, meyveler, sayılar, trafik veya gezegenler diyebilirsin.",
            "Her kategoride resimler, sesler ve oyunlar seni bekliyor.",
            "Aradığın konuyu yazabilir ya da sesli söyleyebilirsin."
        ],
        ["category"] =
        [
            "Resimlere bakıp kelimeleri öğrenebilirsin.",
            "Bir kelimeye dokununca sesini dinleyebilirsin.",
            "Bu kategoriyle ilgili oyunları da açabilirim.",
            "Başka bir kategoriye geçmek istersen adını söyle."
        ],
        ["connectdots"] =
        [
            "Numaraları sırayla birleştir. Birden başla.",
            "Parmağını bir sonraki noktaya götür.",
            "Tüm noktaları birleştirince şekil tamamlanacak."
        ],
        ["drawinggame"] =
        [
            "Parmağınla şekli çiz, sonra tahmin ettir.",
            "Çizimi temiz yapmak tahmini kolaylaştırır.",
            "Baştan denemek istersen temizle düğmesine dokun."
        ],
        ["default"] =
        [
            "Sana yardım etmek için buradayım.",
            "Mikrofona dokunup ne yapmak istediğini söyleyebilirsin.",
            "Hayvanlar, oyunlar, günlük görev, masallar veya ana sayfa diyebilirsin.",
            "Yazı alanına bölüm adını yazıp hızlıca gidebilirsin."
        ],
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
        "home" => "\"Günlük görev\" veya \"Hayvanlar\" de.",
        "games" => "\"Matematik\", \"Boyama\" veya \"Çizim\" de.",
        "learningmodules" => "\"Meyveler\", \"Trafik\" veya \"Gezegenler\" de.",
        _ => "\"Ana sayfa\" veya \"Geri dön\" de."
    };
}
