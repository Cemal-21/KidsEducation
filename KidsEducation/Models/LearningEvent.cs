namespace KidsEducation.Models;

public class LearningEvent
{
    public string ProfileId { get; set; } = string.Empty;
    public string CategoryId { get; set; } = string.Empty;
    public string ItemId { get; set; } = string.Empty;
    public string GameType { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public int DifficultyLevel { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}