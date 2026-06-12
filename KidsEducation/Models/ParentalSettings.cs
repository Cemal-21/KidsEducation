namespace KidsEducation.Models;

/// <summary>
/// Ebeveyn paneli ayarları — Preferences'a ayrı kaydedilir
/// </summary>
public class ParentalSettings
{
    /// <summary>Günlük oyun süresi limiti (dakika). 0 = sınırsız</summary>
    public int DailyTimeLimitMinutes { get; set; } = 30;

    /// <summary>Hangi kategori ID'leri gizlensin</summary>
    public List<string> HiddenCategoryIds { get; set; } = new();

    /// <summary>Haftalık rapor bildirimi açık mı</summary>
    public bool WeeklyReportEnabled { get; set; } = true;

    /// <summary>Ses efektleri açık mı</summary>
    public bool SoundEnabled { get; set; } = true;
}
