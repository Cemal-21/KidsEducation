using CommunityToolkit.Mvvm.ComponentModel;

namespace KidsEducation.Models;

public partial class MemoryCard : ObservableObject
{
    [ObservableProperty] private string _id = Guid.NewGuid().ToString();
    [ObservableProperty] private string _itemId = string.Empty;
    [ObservableProperty] private string _imagePath = string.Empty;
    [ObservableProperty] private string _nameTr = string.Empty;

    [ObservableProperty] private bool _isFlipped;
    [ObservableProperty] private bool _isMatched;
}

public class MemorySession
{
    public string CategoryId { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? FinishedAt { get; set; }
    public int DifficultyLevel { get; set; } = 1;

    public List<MemoryCard> Cards { get; set; } = new();

    public int TotalPairs => Cards.Count / 2;
    public int MatchedPairs => Cards.Count(c => c.IsMatched) / 2;
    public int Moves { get; set; }

    public bool IsComplete => MatchedPairs == TotalPairs;
}
