namespace KidsEducation.Models;

public class SongItem
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Emoji { get; set; } = "🎵";
    public string ColorHex { get; set; } = "#6E64F7";
    public string ColorHex2 { get; set; } = "#3A86FF";
    public string DurationText { get; set; } = string.Empty;
    public string TempoText { get; set; } = string.Empty;
    public string LevelText { get; set; } = string.Empty;
    public string AudioFile { get; set; } = string.Empty;
    public List<string> Lines { get; set; } = new();
    public List<SongLyricLine> SyncedLines { get; set; } = new();

    public string PreviewLine => Lines.FirstOrDefault() ?? Subtitle;
    public string LyricsText => string.Join(Environment.NewLine, Lines);
    public List<SongLyricLine> DisplayLines =>
        Lines.Select(line => new SongLyricLine { Text = line }).ToList();
}

public class SongLyricLine
{
    public int StartMs { get; set; }
    public int EndMs { get; set; }
    public string Text { get; set; } = string.Empty;
}
