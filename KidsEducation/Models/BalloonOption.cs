using CommunityToolkit.Mvvm.ComponentModel;

namespace KidsEducation.Models;

public partial class BalloonOption : ObservableObject
{
    public string ItemId { get; set; } = string.Empty;
    public string NameTr { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty;
    public string BackgroundColor { get; set; } = "#FFFFFF";
    public string StrokeColor { get; set; } = "#FFE2C1";
    public int Row { get; set; }
    public int Column { get; set; }

    [ObservableProperty] private bool _isPopped;
    [ObservableProperty] private bool _isWrong;

    public double PopOpacity => IsPopped ? 0.35 : 1.0;
    public double PopScale => IsPopped ? 0.72 : 1.0;

    partial void OnIsPoppedChanged(bool value)
    {
        OnPropertyChanged(nameof(PopOpacity));
        OnPropertyChanged(nameof(PopScale));
    }
}
