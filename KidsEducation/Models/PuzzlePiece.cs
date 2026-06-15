using CommunityToolkit.Mvvm.ComponentModel;

namespace KidsEducation.Models;

public partial class PuzzlePiece : ObservableObject
{
    public int CorrectIndex { get; set; }
    public string ImageSource { get; set; } = string.Empty;
    public double OffsetX { get; set; }
    public double OffsetY { get; set; }
    public string ItemNameTr { get; set; } = string.Empty;

    [ObservableProperty] private bool _isSelected;

    public bool IsCorrectPosition(int currentIndex) => CorrectIndex == currentIndex;
}
