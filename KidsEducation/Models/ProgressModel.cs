namespace KidsEducation.Models;

/// <summary>
/// "Kaldığın yerden devam et" kartı için veri.
/// progress_{profileId}.json içindeki "lastLesson" bloğundan deserialize edilir.
/// </summary>
public class LastLessonInfo
{
    public string LessonId { get; set; } = "";
    public string Title { get; set; } = "";
    public string CategoryEmoji { get; set; } = "";

    /// <summary>"Matematik · Bölüm 3 / 5" formatında</summary>
    public string CategoryAndChapter { get; set; } = "";

    /// <summary>0–100 arası tamamlanma yüzdesi</summary>
    public int ProgressPercent { get; set; }

    public string ProgressText => $"%{ProgressPercent} tamamlandı";

    /// <summary>
    /// XAML'daki progress bar WidthRequest'i için.
    /// Devam Et kartındaki bar alanı yaklaşık 160px.
    /// </summary>
    public double ProgressBarWidth => 160 * ProgressPercent / 100.0;
}

/// <summary>
/// "Bugünün görevi" satırı için veri.
/// progress_{profileId}.json içindeki "dailyGoal" bloğundan deserialize edilir.
/// </summary>
public class DailyGoalInfo
{
    public int RemainingLessons { get; set; } = 0;
    public int RemainingMinutes { get; set; } = 0;
    public int CompletedCount { get; set; } = 0;
    public int TotalCount { get; set; } = 4;
    public List<DailyQuestInfo> Quests { get; set; } = new();

    /// <summary>"3/4" formatında gösterim</summary>
    public string CompletionText => $"{CompletedCount}/{TotalCount}";

    public string HeadlineText => CompletedCount >= TotalCount
        ? "Bugünün görevleri tamam!"
        : $"{Math.Max(TotalCount - CompletedCount, 0)} mini görev kaldı";

    public string RewardText => CompletedCount >= TotalCount
        ? "Harika tempo, yarın seri devam."
        : "Oyun oynadıkça hedefler dolar.";
}

public class DailyQuestInfo
{
    public string Id { get; set; } = string.Empty;
    public string Emoji { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Progress { get; set; }
    public int Target { get; set; } = 1;

    public bool IsCompleted => Progress >= Target;
    public double ProgressRatio => Target <= 0 ? 0 : Math.Min(1, (double)Progress / Target);
    public double ProgressBarWidth => 180 * ProgressRatio;
    public string ProgressText => $"{Math.Min(Progress, Target)}/{Target}";
    public string StatusText => IsCompleted ? "Tamam" : ProgressText;
}

public class DailyActivityStats
{
    public int LessonsCompleted { get; set; }
    public int CorrectAnswers { get; set; }
    public int StarsEarned { get; set; }
    public int TotalScore { get; set; }
    public Dictionary<string, int> GameTypeCounts { get; set; } = new();
    public List<string> CategoryIds { get; set; } = new();
}
