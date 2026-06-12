using KidsEducation.Enums;

namespace KidsEducation.Models;

public class GameSession
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string CategoryId { get; set; } = string.Empty;
    public GameType GameType { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? FinishedAt { get; set; }
    public List<GameRound> Rounds { get; set; } = new();
    public int DifficultyLevel { get; set; } = 1;

    public int TotalRounds => Rounds.Count;
    public int CorrectCount => Rounds.Count(r => r.Result == GameResult.Correct);
    public int WrongCount => Rounds.Count(r => r.Result == GameResult.Wrong);
    public int Score => CorrectCount * 10;

    public int Stars => CorrectCount switch
    {
        var c when c == TotalRounds => 3,
        var c when c >= TotalRounds * 0.7 => 2,
        var c when c >= TotalRounds * 0.4 => 1,
        _ => 0
    };

    public string StarsText => Stars switch
    {
        3 => "Mükemmel! 🌟",
        2 => "Çok iyi! ⭐",
        1 => "İyi iş! 👏",
        _ => "Tekrar dene! 💪"
    };
}

public class GameRound
{
    public LearningItem CorrectItem { get; set; } = null!;
    public List<LearningItem> Options { get; set; } = new();
    public string? SelectedItemId { get; set; }
    public GameResult Result { get; set; } = GameResult.NotPlayed;
}