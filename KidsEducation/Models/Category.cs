using KidsEducation.Enums;

namespace KidsEducation.Models;

public class Category
{
    public string Id { get; set; } = string.Empty;
    public string NameTr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string Emoji { get; set; } = "📦";
    public string ColorHex { get; set; } = "#FF6B9D";
    public string ColorHex2 { get; set; } = "#FF4757";
    public string Image { get; set; } = string.Empty;
    public int[] AvailableForAgeGroups { get; set; } = Array.Empty<int>();

    // BackgroundHex: yoksa ColorHex'e düşer
    private string? _backgroundHex;
    public string BackgroundHex
    {
        get => _backgroundHex ?? ColorHex;
        set => _backgroundHex = value;
    }

    public int ProgressPercent { get; set; }
    public double ProgressBarWidth => 120 * ProgressPercent / 100.0;
    public double ProgressNormalized => ProgressPercent / 100.0;
    public bool IsLocked { get; set; }

    public bool IsAvailableFor(ChildProfile profile) =>
        AvailableForAgeGroups.Contains((int)profile.AgeGroup);
}
