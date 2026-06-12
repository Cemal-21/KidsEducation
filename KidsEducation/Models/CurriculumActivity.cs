namespace KidsEducation.Models;

public class CurriculumActivity
{
    public string Id { get; set; } = string.Empty;
    public string SkillId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Emoji { get; set; } = string.Empty;
    public string MebArea { get; set; } = string.Empty;
    public string DurationText { get; set; } = string.Empty;
    public string Materials { get; set; } = string.Empty;
    public string Goal { get; set; } = string.Empty;
    public string ParentPrompt { get; set; } = string.Empty;
    public string StepsText { get; set; } = string.Empty;
}
