using System.Text.Json;
using KidsEducation.Models;

namespace KidsEducation.Services;

public class ProfileService
{
    private const string ProfilesKey = "child_profiles";
    private const string ActiveProfileKey = "active_profile_id";
    private const string ParentalKey = "parental_settings";
    private const string DailyTimeKey = "daily_time_{0}_{1}"; // profileId_tarih
    private const string DailyActivityKey = "daily_activity_{0}_{1}"; // profileId_tarih

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly SkillCatalogService _skillCatalog;

    public ProfileService(SkillCatalogService skillCatalog)
    {
        _skillCatalog = skillCatalog;
    }

    // ── Profil işlemleri ─────────────────────────────────────

    public List<ChildProfile> GetAllProfiles()
    {
        var json = Preferences.Get(ProfilesKey, "[]");
        return JsonSerializer.Deserialize<List<ChildProfile>>(json, JsonOptions) ?? new();
    }

    public ChildProfile? GetActiveProfile()
    {
        var profiles = GetAllProfiles();
        var activeId = Preferences.Get(ActiveProfileKey, string.Empty);
        return profiles.FirstOrDefault(p => p.Id == activeId);
    }

    public void SaveProfile(ChildProfile profile)
    {
        var profiles = GetAllProfiles();
        var index = profiles.FindIndex(p => p.Id == profile.Id);

        if (index >= 0)
            profiles[index] = profile;
        else
            profiles.Add(profile);

        Preferences.Set(ProfilesKey, JsonSerializer.Serialize(profiles, JsonOptions));
    }

    public void SetActiveProfile(string profileId)
    {
        Preferences.Set(ActiveProfileKey, profileId);
        // Profil seçildiğinde streak'i kontrol et — oyun oynamadan gün geçtiyse sıfırla
        var profiles = GetAllProfiles();
        var profile = profiles.FirstOrDefault(p => p.Id == profileId);
        if (profile is null) return;

        var today = DateTime.Today;
        var lastPlayed = profile.LastPlayedAt.Date;

        if (lastPlayed < today.AddDays(-1) && profile.StreakDays > 0)
        {
            profile.StreakDays = 0;
            SaveProfile(profile);
        }
    }

    public void DeleteProfile(string profileId)
    {
        var profiles = GetAllProfiles();
        profiles.RemoveAll(p => p.Id == profileId);
        Preferences.Set(ProfilesKey, JsonSerializer.Serialize(profiles, JsonOptions));

        var activeId = Preferences.Get(ActiveProfileKey, string.Empty);
        if (activeId == profileId)
            Preferences.Remove(ActiveProfileKey);
    }

    // ── İlerleme güncelle ─────────────────────────────────────

    public void UpdateProgress(string profileId, string categoryId, GameSession session)
    {
        var profiles = GetAllProfiles();
        var profile = profiles.FirstOrDefault(p => p.Id == profileId);
        if (profile is null) return;

        if (!profile.CategoryProgresses.TryGetValue(categoryId, out var progress))
        {
            progress = new CategoryProgress { CategoryId = categoryId };
            profile.CategoryProgresses[categoryId] = progress;
        }

        progress.PlayCount++;
        progress.LastPlayedAt = DateTime.UtcNow;
        progress.CurrentDifficulty = session.DifficultyLevel;

        if (session.Score > progress.BestScore) progress.BestScore = session.Score;
        if (session.Stars > progress.BestStars) progress.BestStars = session.Stars;

        var previousPlayedAt = profile.LastPlayedAt;

        // Streak güncelle
        UpdateStreak(profile, previousPlayedAt);

        profile.TotalStars += session.Stars;
        profile.TotalXp += session.Score;
        profile.TotalLessonsCompleted += 1;
        profile.LastPlayedAt = DateTime.UtcNow;

        UpdateSkillProgress(profile, session);

        SaveProfile(profile);
        UpdateDailyActivity(profileId, categoryId, session);
    }

    private void UpdateSkillProgress(ChildProfile profile, GameSession session)
    {
        var skillIds = _skillCatalog.GetSkillIdsForSession(session);
        if (skillIds.Count == 0)
            return;

        foreach (var skillId in skillIds)
        {
            if (!profile.SkillProgresses.TryGetValue(skillId, out var progress))
            {
                progress = new SkillProgress { SkillId = skillId };
                profile.SkillProgresses[skillId] = progress;
            }

            progress.PlayCount++;
            progress.CorrectAnswers += session.CorrectCount;
            progress.TotalAnswers += session.TotalRounds;
            progress.LastPracticedAt = DateTime.UtcNow;
        }
    }

    private static void UpdateStreak(ChildProfile profile, DateTime previousPlayedAt)
    {
        var today = DateTime.Today;
        var lastPlayed = previousPlayedAt.Date;

        if (lastPlayed == today)
        {
            if (profile.StreakDays <= 0)
                profile.StreakDays = 1;

            return; // Bugün zaten oynandı
        }

        if (lastPlayed == today.AddDays(-1))
            profile.StreakDays = Math.Max(profile.StreakDays, 0) + 1; // Dün oynandı, seri devam
        else
            profile.StreakDays = 1; // Seri koptu, sıfırla
    }

    // ── Günlük görevler ─────────────────────────────────────

    public DailyGoalInfo GetDailyGoal(ChildProfile profile)
    {
        var stats = GetDailyActivity(profile.Id);

        var quests = new List<DailyQuestInfo>
        {
            new()
            {
                Id = "play_one_game",
                Emoji = "🎮",
                Title = "Bir mini oyun bitir",
                Description = "Herhangi bir oyunu tamamla",
                Progress = stats.LessonsCompleted,
                Target = 1
            },
            new()
            {
                Id = "answer_five",
                Emoji = "✅",
                Title = "5 doğru cevap ver",
                Description = "Tahmin oyunlarında doğru seçenekleri bul",
                Progress = stats.CorrectAnswers,
                Target = 5
            },
            new()
            {
                Id = "earn_stars",
                Emoji = "⭐",
                Title = "2 yıldız topla",
                Description = "Oyun sonuçlarından yıldız kazan",
                Progress = stats.StarsEarned,
                Target = 2
            }
        };

        if (stats.GameTypeCounts.TryGetValue("SoundGuess", out var soundGuessCount) && soundGuessCount > 0)
        {
            quests.Add(new DailyQuestInfo
            {
                Id = "sound_guess",
                Emoji = "🔊",
                Title = "Sesli ipucunu çöz",
                Description = "Sesli Tahmin oyununu bitir",
                Progress = soundGuessCount,
                Target = 1
            });
        }
        else
        {
            quests.Add(new DailyQuestInfo
            {
                Id = "try_sound_guess",
                Emoji = "🔊",
                Title = "Sesli Tahmin dene",
                Description = "Bir ipucu dinle ve doğru görseli seç",
                Progress = 0,
                Target = 1
            });
        }

        return new DailyGoalInfo
        {
            RemainingLessons = quests.Count(q => !q.IsCompleted),
            RemainingMinutes = Math.Max(0, quests.Count(q => !q.IsCompleted) * 3),
            CompletedCount = quests.Count(q => q.IsCompleted),
            TotalCount = quests.Count,
            Quests = quests
        };
    }

    private DailyActivityStats GetDailyActivity(string profileId)
    {
        var key = string.Format(DailyActivityKey, profileId, DateTime.Today.ToString("yyyyMMdd"));
        var json = Preferences.Get(key, "{}");

        return JsonSerializer.Deserialize<DailyActivityStats>(json, JsonOptions)
               ?? new DailyActivityStats();
    }

    private void UpdateDailyActivity(string profileId, string categoryId, GameSession session)
    {
        var stats = GetDailyActivity(profileId);

        stats.LessonsCompleted++;
        stats.CorrectAnswers += session.CorrectCount;
        stats.StarsEarned += session.Stars;
        stats.TotalScore += session.Score;

        var gameType = session.GameType.ToString();
        stats.GameTypeCounts.TryGetValue(gameType, out var currentGameCount);
        stats.GameTypeCounts[gameType] = currentGameCount + 1;

        if (!string.IsNullOrWhiteSpace(categoryId) && !stats.CategoryIds.Contains(categoryId))
            stats.CategoryIds.Add(categoryId);

        var key = string.Format(DailyActivityKey, profileId, DateTime.Today.ToString("yyyyMMdd"));
        Preferences.Set(key, JsonSerializer.Serialize(stats, JsonOptions));
    }

    // ── Ebeveyn ayarları ─────────────────────────────────────

    public ParentalSettings GetParentalSettings()
    {
        var json = Preferences.Get(ParentalKey, "{}");
        return JsonSerializer.Deserialize<ParentalSettings>(json, JsonOptions)
               ?? new ParentalSettings();
    }

    public void SaveParentalSettings(ParentalSettings settings)
    {
        Preferences.Set(ParentalKey, JsonSerializer.Serialize(settings, JsonOptions));
    }

    // ── Günlük süre takibi ────────────────────────────────────

    /// <summary>Bugün oynanan süreyi dakika olarak döner</summary>
    public int GetTodayPlayedMinutes(string profileId)
    {
        var key = string.Format(DailyTimeKey, profileId, DateTime.Today.ToString("yyyyMMdd"));
        return Preferences.Get(key, 0);
    }

    /// <summary>Oynanan süreyi ekler (dakika)</summary>
    public void AddPlayedMinutes(string profileId, int minutes)
    {
        var key = string.Format(DailyTimeKey, profileId, DateTime.Today.ToString("yyyyMMdd"));
        var current = Preferences.Get(key, 0);
        Preferences.Set(key, current + minutes);
    }

    /// <summary>Günlük limit aşıldı mı?</summary>
    public bool IsDailyLimitReached(string profileId)
    {
        var settings = GetParentalSettings();
        if (settings.DailyTimeLimitMinutes <= 0) return false;

        var played = GetTodayPlayedMinutes(profileId);
        return played >= settings.DailyTimeLimitMinutes;
    }

    // ── Haftalık rapor verisi ─────────────────────────────────

    public WeeklyReport GetWeeklyReport(string profileId)
    {
        var profile = GetAllProfiles().FirstOrDefault(p => p.Id == profileId);
        if (profile is null) return new WeeklyReport();

        var today = DateTime.Today;
        var weekStart = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);

        var weeklyStars = 0;
        var weeklyLessons = 0;
        var dailyMinutes = new int[7];

        foreach (var cp in profile.CategoryProgresses.Values)
        {
            if (cp.LastPlayedAt.Date >= weekStart)
            {
                weeklyStars += cp.BestStars;
                weeklyLessons += cp.PlayCount;

                int dayIndex = (int)(cp.LastPlayedAt.Date - weekStart).TotalDays;
                if (dayIndex >= 0 && dayIndex < 7)
                    dailyMinutes[dayIndex] += 5; // Ortalama 5 dk/ders
            }
        }

        return new WeeklyReport
        {
            ProfileName = profile.Name,
            WeeklyStars = weeklyStars,
            WeeklyLessons = weeklyLessons,
            TotalStars = profile.TotalStars,
            TotalLessons = profile.TotalLessonsCompleted,
            StreakDays = profile.StreakDays,
            DailyMinutes = dailyMinutes,
            TodayMinutes = GetTodayPlayedMinutes(profileId),
            DailyLimit = GetParentalSettings().DailyTimeLimitMinutes
        };
    }
}

public class WeeklyReport
{
    public string ProfileName { get; set; } = string.Empty;
    public int WeeklyStars { get; set; }
    public int WeeklyLessons { get; set; }
    public int TotalStars { get; set; }
    public int TotalLessons { get; set; }
    public int StreakDays { get; set; }
    public int[] DailyMinutes { get; set; } = new int[7];
    public int TodayMinutes { get; set; }
    public int DailyLimit { get; set; }

    public string DailyLimitText => DailyLimit > 0
        ? $"{TodayMinutes} / {DailyLimit} dk"
        : "Sınırsız";

    public bool IsLimitReached => DailyLimit > 0 && TodayMinutes >= DailyLimit;

    // Bar grafik için — Pazartesi'den bugüne
    public List<DailyBarStat> DailyStats
    {
        get
        {
            var days = new[] { "Pzt", "Sal", "Çar", "Per", "Cum", "Cmt", "Paz" };
            var today = (int)DateTime.Today.DayOfWeek;
            // DayOfWeek: Sunday=0, Monday=1 ... ama array'imiz Pzt=0 başlıyor
            var todayIdx = today == 0 ? 6 : today - 1;
            var max = DailyMinutes.Max() is 0 ? 1 : DailyMinutes.Max();

            return DailyMinutes.Select((m, i) => new DailyBarStat
            {
                DayLabel = days[i],
                Minutes = m,
                HeightRatio = (double)m / max,
                IsToday = i == todayIdx
            }).ToList();
        }
    }
}

public class DailyBarStat
{
    public string DayLabel { get; set; } = string.Empty;
    public int Minutes { get; set; }
    public double HeightRatio { get; set; }
    public bool IsToday { get; set; }
    public string MinutesText => Minutes > 0 ? $"{Minutes}dk" : "";
    public Color BarColor => IsToday ? Color.FromArgb("#5148D4") : Color.FromArgb("#D0CCFF");
    public double BarHeight => Math.Max(4, HeightRatio * 60);
}
