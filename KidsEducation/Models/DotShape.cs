namespace KidsEducation.Models;

public class DotShape
{
    public string Id { get; set; } = "";
    public string NameTr { get; set; } = "";
    public string NameEn { get; set; } = "";
    public string Emoji { get; set; } = "";
    public List<DotPoint> Dots { get; set; } = new();
}

public record DotPoint(int Number, float NX, float NY);
