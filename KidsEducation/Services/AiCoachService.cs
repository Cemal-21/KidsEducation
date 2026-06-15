using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using KidsEducation.Models;

namespace KidsEducation.Services;

public class CoachTip
{
    public string Emoji { get; set; } = "💡";
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string CategoryId { get; set; } = string.Empty;
    public string ActionRoute { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

public class AiCoachService
{
    private const string ApiUrl = "https://api.anthropic.com/v1/messages";
    private const string Model = "claude-haiku-4-5-20251001";
    private const string CachedTipKey = "ai_coach_tip_json";
    private const string CachedTipDateKey = "ai_coach_tip_date";

    private readonly AppPreferencesService _prefs;
    private readonly ProfileService _profileService;
    private readonly NotificationService _notificationService;
    private readonly HttpClient _http;

    public AiCoachService(
        AppPreferencesService prefs,
        ProfileService profileService,
        NotificationService notificationService)
    {
        _prefs = prefs;
        _profileService = profileService;
        _notificationService = notificationService;
        _http = new HttpClient();
        _http.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
    }

    // ── Önbellek: aynı gün için tekrar API çağrısı yapma ──
    public CoachTip? GetCachedTip()
    {
        var dateStr = Preferences.Get(CachedTipDateKey, string.Empty);
        if (dateStr != DateTime.Today.ToString("yyyy-MM-dd")) return null;

        var json = Preferences.Get(CachedTipKey, string.Empty);
        if (string.IsNullOrEmpty(json)) return null;

        try { return JsonSerializer.Deserialize<CoachTip>(json); }
        catch { return null; }
    }

    private void CacheTip(CoachTip tip)
    {
        Preferences.Set(CachedTipDateKey, DateTime.Today.ToString("yyyy-MM-dd"));
        Preferences.Set(CachedTipKey, JsonSerializer.Serialize(tip));
    }

    // ── Ana metod: öneri üret ──────────────────────────────
    public async Task<CoachTip?> GetTodaysTipAsync()
    {
        var cached = GetCachedTip();
        if (cached is not null) return cached;

        if (!_prefs.HasApiKey) return BuildOfflineTip();

        var profile = _profileService.GetActiveProfile();
        if (profile is null) return BuildOfflineTip();

        try
        {
            var tip = await CallClaudeAsync(profile);
            if (tip is not null) CacheTip(tip);
            return tip ?? BuildOfflineTip();
        }
        catch
        {
            return BuildOfflineTip();
        }
    }

    private async Task<CoachTip?> CallClaudeAsync(ChildProfile profile)
    {
        var report = _profileService.GetWeeklyReport(profile.Id);
        var topCats = profile.CategoryProgresses
            .OrderByDescending(k => k.Value.BestStars)
            .Take(3)
            .Select(k => k.Key)
            .ToList();
        var weakCats = profile.CategoryProgresses
            .Where(k => k.Value.BestStars < 2)
            .Select(k => k.Key)
            .Take(2)
            .ToList();

        var systemPrompt =
            "Sen KidsEducation uygulaması için bir çocuk eğitim koçusun. " +
            "Türkçe, kısa, sıcak ve motive edici bir öğrenme önerisi oluştur. " +
            "JSON formatında yanıt ver: {\"emoji\":\"...\",\"title\":\"...\",\"body\":\"...\",\"categoryId\":\"...\",\"actionRoute\":\"...\"}. " +
            "title max 40 karakter, body max 100 karakter olsun. " +
            "actionRoute şu route'lardan biri olmalı: game?categoryId=X&gameType=MatchName, zoomgame?categoryId=X, soundgame?categoryId=X, memorygamev2?categoryId=X. " +
            "X yerine önerilen kategori id'sini yaz (animals/colors/fruits/vegetables/vehicles/shapes/numbers/letters/body/seasons/emotions/professions/countries/planets/cities/nature/weather/opposites/objects/traffic).";

        var userMessage =
            $"Çocuk: {profile.Name}, {profile.AgeGroupName} ({profile.AgeRange})\n" +
            $"Bu hafta: {report.WeeklyLessons} ders, {report.WeeklyStars} yıldız\n" +
            $"Seri: {profile.StreakDays} gün\n" +
            $"En iyi konular: {string.Join(", ", topCats)}\n" +
            $"Gelişim gereken: {string.Join(", ", weakCats)}\n" +
            "Bugün için en uygun öğrenme önerisini ver.";

        var requestBody = new
        {
            model = Model,
            max_tokens = 300,
            system = systemPrompt,
            messages = new[] { new { role = "user", content = userMessage } }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("x-api-key", _prefs.AnthropicApiKey);

        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;

        var responseJson = await response.Content.ReadAsStringAsync();
        var parsed = JsonDocument.Parse(responseJson);
        var text = parsed.RootElement
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString() ?? "";

        // JSON bloğunu ayıkla
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start < 0 || end < 0) return null;
        var tipJson = text[start..(end + 1)];

        return JsonSerializer.Deserialize<CoachTip>(tipJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }

    // ── API key olmadan statik öneri ──────────────────────
    private static CoachTip BuildOfflineTip()
    {
        var today = DateTime.Today;
        var seed = today.Year * 10000 + today.Month * 100 + today.Day;
        var rng = new Random(seed + 99);

        var tips = new[]
        {
            new CoachTip { Emoji="🐾", Title="Hayvanları keşfet!", Body="Bugün hayvanları tanımayı pratik yap. Her tekrar beyni güçlendirir!", CategoryId="animals", ActionRoute="game?categoryId=animals&gameType=MatchName" },
            new CoachTip { Emoji="🎨", Title="Renkler zamanı!", Body="Renkleri bilmek dünyayı anlamlandırır. Bugün renk oyunu oyna!", CategoryId="colors", ActionRoute="zoomgame?categoryId=colors" },
            new CoachTip { Emoji="🍎", Title="Meyveler & sebzeler", Body="Sağlıklı beslenmek için önce tanımak lazım. Hadi oynayalım!", CategoryId="fruits", ActionRoute="soundgame?categoryId=fruits" },
            new CoachTip { Emoji="🔢", Title="Sayıları say!", Body="Matematiksel düşünme erken yaşta başlar. Sayı oyunu seni bekliyor!", CategoryId="numbers", ActionRoute="game?categoryId=numbers&gameType=MatchName" },
            new CoachTip { Emoji="🔤", Title="Harfler öğren!", Body="Okuma yolculuğu tek bir harfle başlar. Bugün harf oyunu oyna!", CategoryId="letters", ActionRoute="game?categoryId=letters&gameType=MatchName" },
            new CoachTip { Emoji="🌸", Title="Mevsim sürprizi!", Body="Doğayı anlamak mevsimlerle başlar. Bugün mevsim oyunu var!", CategoryId="seasons", ActionRoute="sortinggame?categoryId=seasons" },
            new CoachTip { Emoji="🪐", Title="Gezegen yolculuğu", Body="Uzayı keşfetmek merakı büyütür. Bugün gezegenleri tanı!", CategoryId="planets", ActionRoute="zoomgame?categoryId=planets" },
            new CoachTip { Emoji="↔️", Title="Zıtları bul!", Body="Büyük-küçük, hızlı-yavaş... Kavramları oyunla pekiştir.", CategoryId="opposites", ActionRoute="game?categoryId=opposites&gameType=MatchName" },
            new CoachTip { Emoji="🎒", Title="Eşyaları tanı", Body="Günlük eşyaları öğrenmek çevreyi anlamayı kolaylaştırır.", CategoryId="objects", ActionRoute="soundgame?categoryId=objects" },
            new CoachTip { Emoji="🚦", Title="Trafik zamanı", Body="Güvenli yaşam için trafik işaretlerini öğrenelim.", CategoryId="traffic", ActionRoute="game?categoryId=traffic&gameType=MatchName" },
            new CoachTip { Emoji="🌿", Title="Doğa keşfi", Body="Dağ, deniz, orman... Doğayı tanımak harika bir macera.", CategoryId="nature", ActionRoute="memorygamev2?categoryId=nature" },
        };

        return tips[rng.Next(tips.Length)];
    }

    // ── Bildirim olarak gönder ────────────────────────────
    public async Task SendTipAsNotificationAsync()
    {
        var tip = await GetTodaysTipAsync();
        if (tip is null) return;

        await _notificationService.ShowCoachNotificationAsync(tip.Emoji, tip.Title, tip.Body);
    }
}
