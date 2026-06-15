namespace KidsEducation.Models;

public class DrawingChallenge
{
    public string Id { get; set; } = "";
    public string NameTr { get; set; } = "";
    public string NameEn { get; set; } = "";
    public string Emoji { get; set; } = "";
    public string Hint { get; set; } = "";
    public DrawingShapeType ShapeType { get; set; }
}

public enum DrawingShapeType
{
    Circle,
    Square,
    Triangle,
    Star,
    Heart,
    Cross,
    Arrow,
    ZigZag,
    Wave,
    Spiral,
}
