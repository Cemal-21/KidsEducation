namespace KidsEducation.Models;

public class Badge
{
    public string Id { get; set; } = string.Empty;
    public string Emoji { get; set; } = "🏅";
    public string NameTr { get; set; } = string.Empty;
    public string DescriptionTr { get; set; } = string.Empty;

    /// <summary>Kazanılma koşulu türü</summary>
    public BadgeConditionType ConditionType { get; set; }

    /// <summary>Eşik değeri (örn. 5 ders, 7 gün streak)</summary>
    public int Threshold { get; set; }

    /// <summary>Kazanıldı mı? (profile'dan hesaplanır)</summary>
    public bool IsEarned { get; set; }

    /// <summary>Kazanılma tarihi</summary>
    public DateTime? EarnedAt { get; set; }

    /// <summary>Kilitli rozetler soluk görünür</summary>
    public bool IsLocked => !IsEarned;

    public string EarnedDateText => EarnedAt.HasValue
        ? EarnedAt.Value.ToString("d MMMM yyyy", new System.Globalization.CultureInfo("tr-TR"))
        : string.Empty;
}

public enum BadgeConditionType
{
    LessonCount,   // Toplam tamamlanan ders sayısı
    StreakDays,    // Günlük seri (streak)
    StarCount,     // Toplam yıldız
    XpCount        // Toplam XP
}
