namespace KidsEducation.Models;

/// <summary>
/// İlerleme sayfasında kategori bazlı ilerleme göstermek için
/// </summary>
public class CategoryStat
{
    public string CategoryId { get; set; } = string.Empty;
    public string NameTr { get; set; } = string.Empty;
    public string Emoji { get; set; } = string.Empty;
    public string ColorHex { get; set; } = "#EDE9FF";
    public int ProgressPercent { get; set; }

    /// <summary>Progress bar genişliği (~180px alan)</summary>
    public double ProgressBarWidth => 180 * ProgressPercent / 100.0;
}
