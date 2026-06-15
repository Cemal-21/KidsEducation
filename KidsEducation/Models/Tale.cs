namespace KidsEducation.Models;

public class Tale
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Emoji { get; set; } = "📖";
    public string Category { get; set; } = "klasik";
    public int AgeMin { get; set; } = 3;
    public int AgeMax { get; set; } = 10;
    public int DurationSeconds { get; set; } = 120;
    public List<TalePage> Pages { get; set; } = new();

    public string DurationText => DurationSeconds < 60
        ? $"{DurationSeconds} sn"
        : $"{DurationSeconds / 60} dk";

    public string AgeRangeText => $"{AgeMin}-{AgeMax} yaş";
    public int PageCount => Pages.Count;
}

public class TalePage
{
    public int PageNumber { get; set; }
    public string Emoji { get; set; } = "";
    public string Text { get; set; } = "";
    public string AudioFile { get; set; } = "";
}
