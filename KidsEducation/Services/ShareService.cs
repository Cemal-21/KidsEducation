namespace KidsEducation.Services;

public class ShareService
{
    private readonly ProfileService _profileService;
    private readonly ContentService _contentService;

    public ShareService(ProfileService profileService, ContentService contentService)
    {
        _profileService = profileService;
        _contentService = contentService;
    }

    // ── Oyun sonucu paylaş ──────────────────────────────────
    public async Task ShareGameResultAsync(string profileName, string categoryName,
        int stars, int score, int correct, int total)
    {
        var starsText = stars switch
        {
            3 => "⭐⭐⭐ Mükemmel",
            2 => "⭐⭐ Çok İyi",
            1 => "⭐ İyi",
            _ => "💪 Devam Et"
        };

        var text = $"""
            🦉 KidsEğitim'de harika bir sonuç!

            👤 {profileName}
            📚 Konu: {categoryName}
            {starsText}
            ✅ {correct}/{total} doğru
            🏆 {score} puan

            Çocuğunla birlikte öğren! 🎉
            """;

        await Share.Default.RequestAsync(new ShareTextRequest
        {
            Title = "Oyun Sonucunu Paylaş",
            Text  = text
        });
    }

    // ── Rozet/başarım paylaş ───────────────────────────────
    public async Task ShareAchievementAsync(string profileName, string badgeName,
        string badgeEmoji, string badgeDescription)
    {
        var text = $"""
            {badgeEmoji} KidsEğitim'de yeni rozet kazandım!

            👤 {profileName}
            🏅 {badgeName}
            📝 {badgeDescription}

            Sen de çocuğunla birlikte öğren! 🦉
            """;

        await Share.Default.RequestAsync(new ShareTextRequest
        {
            Title = "Rozet Kazandım!",
            Text  = text
        });
    }

    // ── Genel ilerleme paylaş ──────────────────────────────
    public async Task ShareProgressAsync(string profileName, int totalStars,
        int streakDays, string levelTitle)
    {
        var streakText = streakDays > 0 ? $"🔥 {streakDays} günlük seri" : "";

        var text = $"""
            🦉 KidsEğitim ilerleme raporu!

            👤 {profileName}
            ⭐ {totalStars} yıldız kazanıldı
            🏅 Seviye: {levelTitle}
            {streakText}

            Eğlenerek öğren, büyürken kazan! 🎉
            """;

        await Share.Default.RequestAsync(new ShareTextRequest
        {
            Title = "İlerleme Raporunu Paylaş",
            Text  = text
        });
    }
}
