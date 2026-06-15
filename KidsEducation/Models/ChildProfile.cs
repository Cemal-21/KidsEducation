using KidsEducation.Enums;

namespace KidsEducation.Models;

public class ChildProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public AgeGroup AgeGroup { get; set; } = AgeGroup.Toddler;
    public string AvatarEmoji { get; set; } = "🐰";
    public int TotalStars { get; set; }
    public int TotalXp { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastPlayedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, CategoryProgress> CategoryProgresses { get; set; } = new();

    // ── Streak ───────────────────────────────────────────────
    public int StreakDays { get; set; }

    // ── Toplam tamamlanan ders ───────────────────────────────
    public int TotalLessonsCompleted { get; set; }

    // ── Kazanılan rozetler: BadgeId → kazanılma tarihi ───────
    public Dictionary<string, DateTime> EarnedBadges { get; set; } = new();

    // ── Gelişim / beceri takibi ─────────────────────────────
    public Dictionary<string, SkillProgress> SkillProgresses { get; set; } = new();

    // ── Computed ─────────────────────────────────────────────
    public string TotalXpFormatted => TotalXp >= 1000
        ? $"{TotalXp / 1000.0:0.#}K"
        : TotalXp.ToString();

    public int Level => Math.Max(1, (TotalXp / 250) + 1);
    public int CurrentLevelXp => TotalXp % 250;
    public int NextLevelXp => 250;
    public double LevelProgress => Math.Min(1, CurrentLevelXp / (double)NextLevelXp);
    public int XpToNextLevel => Math.Max(0, NextLevelXp - CurrentLevelXp);
    public string LevelTitle => Level switch
    {
        <= 2 => "Yeni Kaşif",
        <= 4 => "Meraklı Öğrenci",
        <= 7 => "Bilgi Avcısı",
        <= 10 => "Süper Kaşif",
        _ => "Bilgi Ustası"
    };
    public string LevelText => $"Seviye {Level}";
    public string LevelProgressText => $"{CurrentLevelXp}/{NextLevelXp} XP";

    public string AgeGroupName => AgeGroup switch
    {
        AgeGroup.Toddler => "Minikler",
        AgeGroup.Explorer => "Keşifçiler",
        AgeGroup.Adventurer => "Kaşifler",
        _ => string.Empty
    };

    public string AgeRange => AgeGroup switch
    {
        AgeGroup.Toddler => "3-5 yaş",
        AgeGroup.Explorer => "5-7 yaş",
        AgeGroup.Adventurer => "7-9 yaş",
        _ => string.Empty
    };

    public int OptionCount => AgeGroup switch
    {
        AgeGroup.Toddler => 2,
        AgeGroup.Explorer => 4,
        AgeGroup.Adventurer => 4,
        _ => 4
    };

    public bool TimerEnabled => AgeGroup == AgeGroup.Adventurer;
}

public class CategoryProgress
{
    public string CategoryId { get; set; } = string.Empty;
    public int BestScore { get; set; }
    public int BestStars { get; set; }
    public int PlayCount { get; set; }
    public DateTime LastPlayedAt { get; set; }
    public int CurrentDifficulty { get; set; } = 0; // 0 = henüz ayarlanmadı, baseDifficulty kullanılır
}

public class SkillProgress
{
    public string SkillId { get; set; } = string.Empty;
    public int PlayCount { get; set; }
    public int CorrectAnswers { get; set; }
    public int TotalAnswers { get; set; }
    public DateTime LastPracticedAt { get; set; }

    public int AccuracyPercent =>
        TotalAnswers <= 0 ? 0 : (int)Math.Round(CorrectAnswers * 100.0 / TotalAnswers);
}
