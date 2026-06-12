using KidsEducation.Models;

namespace KidsEducation.Services;

public class BadgeService
{
    /// <summary>
    /// Tüm rozet tanımları — buraya yeni rozetler eklenebilir.
    /// </summary>
    private static readonly List<Badge> AllBadges = new()
    {
        // ── Ders sayısı rozetleri ────────────────────────────
        new Badge
        {
            Id = "lesson_5",
            Emoji = "📚",
            NameTr = "Meraklı Okuyucu",
            DescriptionTr = "5 ders tamamla",
            ConditionType = BadgeConditionType.LessonCount,
            Threshold = 5
        },
        new Badge
        {
            Id = "lesson_10",
            Emoji = "🎓",
            NameTr = "Bilgi Avcısı",
            DescriptionTr = "10 ders tamamla",
            ConditionType = BadgeConditionType.LessonCount,
            Threshold = 10
        },
        new Badge
        {
            Id = "lesson_25",
            Emoji = "🚀",
            NameTr = "Süper Öğrenci",
            DescriptionTr = "25 ders tamamla",
            ConditionType = BadgeConditionType.LessonCount,
            Threshold = 25
        },
        new Badge
        {
            Id = "lesson_50",
            Emoji = "🏆",
            NameTr = "Efsane",
            DescriptionTr = "50 ders tamamla",
            ConditionType = BadgeConditionType.LessonCount,
            Threshold = 50
        },

        // ── Streak rozetleri ─────────────────────────────────
        new Badge
        {
            Id = "streak_3",
            Emoji = "🔥",
            NameTr = "Isınıyor",
            DescriptionTr = "3 gün üst üste oyna",
            ConditionType = BadgeConditionType.StreakDays,
            Threshold = 3
        },
        new Badge
        {
            Id = "streak_7",
            Emoji = "⚡",
            NameTr = "Hafta Şampiyonu",
            DescriptionTr = "7 gün üst üste oyna",
            ConditionType = BadgeConditionType.StreakDays,
            Threshold = 7
        },
        new Badge
        {
            Id = "streak_30",
            Emoji = "🌟",
            NameTr = "Ay Kahramanı",
            DescriptionTr = "30 gün üst üste oyna",
            ConditionType = BadgeConditionType.StreakDays,
            Threshold = 30
        },

        // ── Yıldız rozetleri ─────────────────────────────────
        new Badge
        {
            Id = "stars_50",
            Emoji = "⭐",
            NameTr = "Yıldız Toplayıcı",
            DescriptionTr = "50 yıldız kazan",
            ConditionType = BadgeConditionType.StarCount,
            Threshold = 50
        },
        new Badge
        {
            Id = "stars_200",
            Emoji = "🌠",
            NameTr = "Yıldız Fırtınası",
            DescriptionTr = "200 yıldız kazan",
            ConditionType = BadgeConditionType.StarCount,
            Threshold = 200
        },

        // ── XP rozetleri ─────────────────────────────────────
        new Badge
        {
            Id = "xp_500",
            Emoji = "💎",
            NameTr = "XP Ustası",
            DescriptionTr = "500 XP kazan",
            ConditionType = BadgeConditionType.XpCount,
            Threshold = 500
        },
        new Badge
        {
            Id = "xp_2000",
            Emoji = "👑",
            NameTr = "Büyük Usta",
            DescriptionTr = "2000 XP kazan",
            ConditionType = BadgeConditionType.XpCount,
            Threshold = 2000
        },
    };

    /// <summary>
    /// Profil verisiyle tüm rozetleri değerlendirir,
    /// kazanılanları işaretler ve döner.
    /// </summary>
    public List<Badge> EvaluateBadges(ChildProfile profile)
    {
        var result = new List<Badge>();

        foreach (var template in AllBadges)
        {
            var badge = new Badge
            {
                Id = template.Id,
                Emoji = template.Emoji,
                NameTr = template.NameTr,
                DescriptionTr = template.DescriptionTr,
                ConditionType = template.ConditionType,
                Threshold = template.Threshold,
            };

            // Profilde kayıtlı kazanma tarihi varsa kullan
            if (profile.EarnedBadges.TryGetValue(template.Id, out var earnedAt))
            {
                badge.IsEarned = true;
                badge.EarnedAt = earnedAt;
            }
            else
            {
                // Koşul sağlanıyor mu kontrol et
                bool conditionMet = template.ConditionType switch
                {
                    BadgeConditionType.LessonCount => profile.TotalLessonsCompleted >= template.Threshold,
                    BadgeConditionType.StreakDays => profile.StreakDays >= template.Threshold,
                    BadgeConditionType.StarCount => profile.TotalStars >= template.Threshold,
                    BadgeConditionType.XpCount => profile.TotalXp >= template.Threshold,
                    _ => false
                };

                if (conditionMet)
                {
                    badge.IsEarned = true;
                    badge.EarnedAt = DateTime.UtcNow;
                    // Profile'a kaydet (kalıcılık için ProfileService.SaveAsync çağrılmalı)
                    profile.EarnedBadges[template.Id] = badge.EarnedAt.Value;
                }
            }

            result.Add(badge);
        }

        // Kazanılanlar önce, kilitlenenler sonra
        return result
            .OrderByDescending(b => b.IsEarned)
            .ThenBy(b => b.Threshold)
            .ToList();
    }

    /// <summary>
    /// Ders/oyun bittikten sonra yeni kazanılan rozetleri döner.
    /// Kutlama ekranı için kullanılır.
    /// </summary>
    public List<Badge> GetNewlyEarnedBadges(ChildProfile profile)
    {
        var all = EvaluateBadges(profile);
        return all.Where(b => b.IsEarned && b.EarnedAt?.Date == DateTime.UtcNow.Date).ToList();
    }
}
