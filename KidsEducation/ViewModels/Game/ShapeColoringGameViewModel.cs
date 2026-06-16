using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KidsEducation.Services;
using Microsoft.Maui.Graphics;

namespace KidsEducation.ViewModels.Game;

// Åekil Boyama: Ã¼stte renkli referans, altta boÅŸ iskelet bÃ¶lgeler boyanÄ±r.
// Mevcut "Boyama" (ColoringGame) oyunundan baÄŸÄ±msÄ±z, ayrÄ± bir oyundur.
public partial class ShapeColoringGameViewModel : ObservableObject
{
    private readonly AudioService _audioService;
    private readonly NavigationService _navigationService;

    // Renk paleti (ColorChoice tipi ColoringGameViewModel ile paylaÅŸÄ±lÄ±r)
    public List<ColorChoice> Palette { get; } = new()
    {
        new("#EF4444", "KÄ±rmÄ±zÄ±"),
        new("#F97316", "Turuncu"),
        new("#EAB308", "SarÄ±"),
        new("#22C55E", "YeÅŸil"),
        new("#3B82F6", "Mavi"),
        new("#8B5CF6", "Mor"),
        new("#EC4899", "Pembe"),
        new("#92400E", "Kahve"),
        new("#6B7280", "Gri"),
        new("#FFFFFF", "Beyaz"),
    };

    [ObservableProperty] private ColorChoice? _selectedColor;
    [ObservableProperty] private List<PaintRegion> _regions = new();
    [ObservableProperty] private string _shapeEmoji = "ğŸ ";
    [ObservableProperty] private string _shapeName = "Ev";
    [ObservableProperty] private int _coloredCount;
    [ObservableProperty] private bool _isComplete;
    [ObservableProperty] private bool _showWarning;
    [ObservableProperty] private string _warningText = string.Empty;

    private int _shapeIndex;
    private int _warningToken;

    // Yeniden Ã§izim isteÄŸi â€” Page tarafÄ±ndan baÄŸlanÄ±r
    public Action? RequestRedraw { get; set; }

    private static readonly ShapeTemplate[] _shapes = ShapeLibrary.All;

    public ShapeColoringGameViewModel(AudioService audioService, NavigationService navigationService)
    {
        _audioService = audioService;
        _navigationService = navigationService;
    }

    [RelayCommand]
    public Task InitializeAsync()
    {
        _shapeIndex = Random.Shared.Next(_shapes.Length);
        LoadShape(_shapeIndex);
        SelectedColor = Palette[0];
        foreach (var c in Palette) c.IsSelected = c == SelectedColor;
        return Task.CompletedTask;
    }

    private void LoadShape(int index)
    {
        var t = _shapes[index];
        ShapeEmoji = t.Emoji;
        ShapeName = t.Name;
        ColoredCount = 0;
        IsComplete = false;
        HideWarning();
        Regions = t.Regions.Select((r, i) => r.Clone(i)).ToList();
        RequestRedraw?.Invoke();
    }

    [RelayCommand]
    public Task NextShapeAsync()
    {
        _shapeIndex = (_shapeIndex + 1) % _shapes.Length;
        LoadShape(_shapeIndex);
        return Task.CompletedTask;
    }

    [RelayCommand]
    public async Task SelectColorAsync(ColorChoice color)
    {
        SelectedColor = color;
        foreach (var c in Palette) c.IsSelected = c == color;
        await _audioService.PlayClickAsync();
    }

    // Page boyama tuvalinden dokunma noktasÄ± + tuval boyutu gÃ¶nderir
    public async Task PaintAtAsync(PointF screenPoint, SizeF canvasSize)
    {
        if (SelectedColor is null || IsComplete) return;

        var norm = ShapeGeometry.ToNormalized(screenPoint, canvasSize);
        // Ãœstte Ã§izilenin altta kalanÄ± boyamasÄ±nÄ± Ã¶nlemek iÃ§in sondan baÅŸa tara
        for (int i = Regions.Count - 1; i >= 0; i--)
        {
            var region = Regions[i];
            if (!region.Contains(norm)) continue;

            // Zaten doÄŸru renkle boyanmÄ±ÅŸsa bir ÅŸey yapma
            if (region.IsColored) return;

            // Renk doÄŸrulamasÄ±: Ã¶rnekteki hedef renge uymuyorsa uyar, boyama
            var correct = string.Equals(region.TargetColor, SelectedColor.Hex,
                StringComparison.OrdinalIgnoreCase);
            if (!correct)
            {
                await ShowWarningAsync("ğŸ¤” O renk deÄŸil, tekrar dene!");
                await _audioService.PlayWrongAsync();
                return;
            }

            // DoÄŸru renk â€” boya
            HideWarning();
            region.FilledColor = SelectedColor.Hex;
            ColoredCount++;
            RequestRedraw?.Invoke();
            await _audioService.PlayCorrectAsync();

            if (ColoredCount >= Regions.Count)
            {
                IsComplete = true;
                await _audioService.PlayCompleteAsync();
            }
            return;
        }
    }

    private async Task ShowWarningAsync(string text)
    {
        WarningText = text;
        ShowWarning = true;
        var token = ++_warningToken;
        await Task.Delay(2200);
        if (token == _warningToken) ShowWarning = false;
    }

    private void HideWarning()
    {
        _warningToken++;
        ShowWarning = false;
    }

    [RelayCommand]
    public Task ResetAsync()
    {
        foreach (var r in Regions) r.FilledColor = null;
        ColoredCount = 0;
        IsComplete = false;
        HideWarning();
        RequestRedraw?.Invoke();
        return Task.CompletedTask;
    }

    [RelayCommand]
    public async Task GoBackAsync() => await _navigationService.GoBackAsync();
}

// Boyanabilir bÃ¶lge (poligon veya daire)
public partial class PaintRegion : ObservableObject
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TargetColor { get; set; } = "#E5E7EB"; // referansta gÃ¶sterilecek renk
    public bool IsCircle { get; set; }
    public PointF[] Polygon { get; set; } = Array.Empty<PointF>();
    public PointF Center { get; set; }
    public float Radius { get; set; }

    [ObservableProperty] private string? _filledColor;

    public bool IsColored => FilledColor is not null;

    public PaintRegion Clone(int newId) => new()
    {
        Id = newId,
        Name = Name,
        TargetColor = TargetColor,
        IsCircle = IsCircle,
        Polygon = Polygon,
        Center = Center,
        Radius = Radius,
        FilledColor = null,
    };

    public bool Contains(PointF norm) =>
        IsCircle ? ShapeGeometry.InCircle(norm, Center, Radius)
                 : ShapeGeometry.InPolygon(norm, Polygon);
}

// HazÄ±r ÅŸekil tanÄ±mÄ±
public class ShapeTemplate
{
    public string Emoji { get; init; } = "";
    public string Name { get; init; } = "";
    public PaintRegion[] Regions { get; init; } = Array.Empty<PaintRegion>();
}

// Normalize [0,1] koordinatlar â€” uniform Ã¶lÃ§ek + ortalama ile ekrana taÅŸÄ±nÄ±r
public static class ShapeGeometry
{
    public static (float Scale, float OffsetX, float OffsetY) Transform(SizeF size)
    {
        float scale = MathF.Min(size.Width, size.Height);
        float ox = (size.Width - scale) / 2f;
        float oy = (size.Height - scale) / 2f;
        return (scale, ox, oy);
    }

    public static PointF ToScreen(PointF norm, SizeF size)
    {
        var (scale, ox, oy) = Transform(size);
        return new PointF(ox + norm.X * scale, oy + norm.Y * scale);
    }

    public static PointF ToNormalized(PointF screen, SizeF size)
    {
        var (scale, ox, oy) = Transform(size);
        if (scale <= 0) return new PointF(-1, -1);
        return new PointF((screen.X - ox) / scale, (screen.Y - oy) / scale);
    }

    public static bool InCircle(PointF p, PointF center, float radius)
    {
        float dx = p.X - center.X, dy = p.Y - center.Y;
        return dx * dx + dy * dy <= radius * radius;
    }

    public static bool InPolygon(PointF p, PointF[] poly)
    {
        if (poly.Length < 3) return false;
        bool inside = false;
        for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
        {
            if (((poly[i].Y > p.Y) != (poly[j].Y > p.Y)) &&
                (p.X < (poly[j].X - poly[i].X) * (p.Y - poly[i].Y) /
                       (poly[j].Y - poly[i].Y) + poly[i].X))
            {
                inside = !inside;
            }
        }
        return inside;
    }
}

// Åekil kÃ¼tÃ¼phanesi â€” basit, tanÄ±nabilir resimler
public static class ShapeLibrary
{
    private static PointF P(float x, float y) => new(x, y);

    private static PaintRegion Poly(string name, string color, params PointF[] pts) =>
        new() { Name = name, TargetColor = color, IsCircle = false, Polygon = pts };

    private static PaintRegion Circle(string name, string color, float cx, float cy, float r) =>
        new() { Name = name, TargetColor = color, IsCircle = true, Center = new PointF(cx, cy), Radius = r };

    public static readonly ShapeTemplate[] All =
    {
        // EV
        new ShapeTemplate
        {
            Emoji = "ğŸ ", Name = "Ev",
            Regions = new[]
            {
                Circle("GÃ¼neÅŸ", "#EAB308", 0.83f, 0.16f, 0.10f),
                Poly("Ã‡atÄ±", "#EF4444", P(0.16f,0.42f), P(0.50f,0.16f), P(0.84f,0.42f)),
                Poly("Duvar", "#F97316", P(0.25f,0.42f), P(0.75f,0.42f), P(0.75f,0.86f), P(0.25f,0.86f)),
                Poly("Pencere", "#3B82F6", P(0.56f,0.52f), P(0.70f,0.52f), P(0.70f,0.66f), P(0.56f,0.66f)),
                Poly("KapÄ±", "#92400E", P(0.38f,0.60f), P(0.50f,0.60f), P(0.50f,0.86f), P(0.38f,0.86f)),
            }
        },
        // BALIK
        new ShapeTemplate
        {
            Emoji = "ğŸŸ", Name = "BalÄ±k",
            Regions = new[]
            {
                Poly("Ãœst YÃ¼zgeÃ§", "#EAB308", P(0.42f,0.34f), P(0.56f,0.16f), P(0.60f,0.34f)),
                Poly("GÃ¶vde", "#F97316",
                    P(0.18f,0.50f), P(0.34f,0.32f), P(0.60f,0.32f),
                    P(0.72f,0.50f), P(0.60f,0.68f), P(0.34f,0.68f)),
                Poly("Kuyruk", "#EF4444", P(0.72f,0.50f), P(0.92f,0.34f), P(0.92f,0.66f)),
                Circle("GÃ¶z", "#6B7280", 0.30f, 0.46f, 0.035f),
            }
        },
        // Ã‡Ä°Ã‡EK
        new ShapeTemplate
        {
            Emoji = "ğŸŒ¸", Name = "Ã‡iÃ§ek",
            Regions = new[]
            {
                Circle("Yaprak Ãœst", "#EC4899", 0.50f, 0.22f, 0.13f),
                Circle("Yaprak Sol", "#EC4899", 0.30f, 0.40f, 0.13f),
                Circle("Yaprak SaÄŸ", "#EC4899", 0.70f, 0.40f, 0.13f),
                Circle("Yaprak Alt", "#EC4899", 0.50f, 0.55f, 0.13f),
                Circle("Merkez", "#EAB308", 0.50f, 0.40f, 0.10f),
                Poly("GÃ¶vde", "#22C55E", P(0.47f,0.62f), P(0.53f,0.62f), P(0.53f,0.92f), P(0.47f,0.92f)),
                Poly("Yaprak", "#22C55E", P(0.50f,0.74f), P(0.74f,0.66f), P(0.53f,0.84f)),
            }
        },
        // ARABA
        new ShapeTemplate
        {
            Emoji = "ğŸš—", Name = "Araba",
            Regions = new[]
            {
                Poly("GÃ¶vde", "#3B82F6",
                    P(0.10f,0.56f), P(0.28f,0.56f), P(0.36f,0.40f), P(0.64f,0.40f),
                    P(0.72f,0.56f), P(0.90f,0.56f), P(0.90f,0.70f), P(0.10f,0.70f)),
                Poly("Cam", "#FFFFFF", P(0.40f,0.43f), P(0.60f,0.43f), P(0.65f,0.54f), P(0.37f,0.54f)),
                Circle("Sol Teker", "#6B7280", 0.30f, 0.72f, 0.09f),
                Circle("SaÄŸ Teker", "#6B7280", 0.70f, 0.72f, 0.09f),
            }
        },
        // ROKET
        new ShapeTemplate
        {
            Emoji = "", Name = "Roket",
            Regions = new[]
            {
                Poly("Gövde", "#3B82F6", P(0.42f,0.18f), P(0.58f,0.18f), P(0.66f,0.68f), P(0.34f,0.68f)),
                Poly("Burun", "#EF4444", P(0.42f,0.18f), P(0.50f,0.06f), P(0.58f,0.18f)),
                Circle("Pencere", "#FFFFFF", 0.50f, 0.36f, 0.08f),
                Poly("Sol Kanat", "#F97316", P(0.34f,0.56f), P(0.18f,0.78f), P(0.38f,0.70f)),
                Poly("Sağ Kanat", "#F97316", P(0.66f,0.56f), P(0.82f,0.78f), P(0.62f,0.70f)),
                Poly("Alev", "#EAB308", P(0.42f,0.68f), P(0.50f,0.92f), P(0.58f,0.68f)),
            }
        },
        // AĞAÇ
        new ShapeTemplate
        {
            Emoji = "", Name = "Ağaç",
            Regions = new[]
            {
                Poly("Gövde", "#92400E", P(0.43f,0.55f), P(0.57f,0.55f), P(0.60f,0.90f), P(0.40f,0.90f)),
                Circle("Sol Yaprak", "#22C55E", 0.36f, 0.42f, 0.17f),
                Circle("Orta Yaprak", "#22C55E", 0.50f, 0.30f, 0.20f),
                Circle("Sağ Yaprak", "#22C55E", 0.64f, 0.42f, 0.17f),
                Circle("Meyve", "#EF4444", 0.58f, 0.38f, 0.04f),
            }
        },
        // KELEBEK
        new ShapeTemplate
        {
            Emoji = "", Name = "Kelebek",
            Regions = new[]
            {
                Circle("Sol Kanat Üst", "#EC4899", 0.32f, 0.34f, 0.16f),
                Circle("Sağ Kanat Üst", "#EC4899", 0.68f, 0.34f, 0.16f),
                Circle("Sol Kanat Alt", "#8B5CF6", 0.34f, 0.58f, 0.13f),
                Circle("Sağ Kanat Alt", "#8B5CF6", 0.66f, 0.58f, 0.13f),
                Poly("Gövde", "#6B7280", P(0.47f,0.24f), P(0.53f,0.24f), P(0.55f,0.78f), P(0.45f,0.78f)),
                Circle("Baş", "#6B7280", 0.50f, 0.18f, 0.06f),
            }
        },
        // ROBOT
        new ShapeTemplate
        {
            Emoji = "", Name = "Robot",
            Regions = new[]
            {
                Poly("Baş", "#6B7280", P(0.30f,0.16f), P(0.70f,0.16f), P(0.70f,0.42f), P(0.30f,0.42f)),
                Circle("Sol Göz", "#3B82F6", 0.42f, 0.28f, 0.04f),
                Circle("Sağ Göz", "#3B82F6", 0.58f, 0.28f, 0.04f),
                Poly("Gövde", "#EAB308", P(0.26f,0.48f), P(0.74f,0.48f), P(0.74f,0.82f), P(0.26f,0.82f)),
                Poly("Sol Kol", "#F97316", P(0.16f,0.52f), P(0.26f,0.52f), P(0.26f,0.76f), P(0.16f,0.76f)),
                Poly("Sağ Kol", "#F97316", P(0.74f,0.52f), P(0.84f,0.52f), P(0.84f,0.76f), P(0.74f,0.76f)),
            }
        },
        // PASTA
        new ShapeTemplate
        {
            Emoji = "", Name = "Pasta",
            Regions = new[]
            {
                Poly("Alt Kat", "#F97316", P(0.22f,0.58f), P(0.78f,0.58f), P(0.78f,0.86f), P(0.22f,0.86f)),
                Poly("Üst Kat", "#EC4899", P(0.32f,0.38f), P(0.68f,0.38f), P(0.68f,0.58f), P(0.32f,0.58f)),
                Poly("Mum", "#3B82F6", P(0.47f,0.20f), P(0.53f,0.20f), P(0.53f,0.38f), P(0.47f,0.38f)),
                Circle("Alev", "#EAB308", 0.50f, 0.16f, 0.05f),
                Circle("Süs Sol", "#FFFFFF", 0.36f, 0.70f, 0.035f),
                Circle("Süs Sağ", "#FFFFFF", 0.64f, 0.70f, 0.035f),
            }
        },
        // UÇAK
        new ShapeTemplate
        {
            Emoji = "", Name = "Uçak",
            Regions = new[]
            {
                Poly("Gövde", "#3B82F6", P(0.12f,0.48f), P(0.74f,0.38f), P(0.90f,0.50f), P(0.74f,0.62f), P(0.12f,0.52f)),
                Poly("Üst Kanat", "#EF4444", P(0.42f,0.45f), P(0.58f,0.18f), P(0.64f,0.48f)),
                Poly("Alt Kanat", "#EF4444", P(0.42f,0.55f), P(0.58f,0.82f), P(0.64f,0.52f)),
                Poly("Kuyruk", "#F97316", P(0.14f,0.45f), P(0.24f,0.28f), P(0.28f,0.48f)),
                Circle("Pencere 1", "#FFFFFF", 0.54f, 0.48f, 0.03f),
                Circle("Pencere 2", "#FFFFFF", 0.64f, 0.48f, 0.03f),
            }
        },
    };
}

