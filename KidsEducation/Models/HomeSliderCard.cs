namespace KidsEducation.Models;

public enum SliderCardType { DailyWord, Challenge, Tales, Progress }

public class HomeSliderCard
{
    public SliderCardType Type { get; set; }
    public string Emoji     { get; set; } = "";
    public string IconSource { get; set; } = "";
    public string Title     { get; set; } = "";
    public string Subtitle  { get; set; } = "";
    public string Detail    { get; set; } = "";
    public string GradientFrom { get; set; } = "#5148D4";
    public string GradientTo   { get; set; } = "#3A86FF";
    public string ActionRoute  { get; set; } = "";
}
