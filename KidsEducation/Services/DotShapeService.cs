using KidsEducation.Models;

namespace KidsEducation.Services;

public class DotShapeService
{
    private readonly List<DotShape> _shapes = BuildShapes();
    private int _currentIndex;

    public DotShape GetNext()
    {
        var shape = _shapes[_currentIndex % _shapes.Count];
        _currentIndex++;
        return shape;
    }

    public int TotalShapes => _shapes.Count;

    private static List<DotShape> BuildShapes() => new()
    {
        new DotShape
        {
            Id = "star", NameTr = "Yıldız", NameEn = "Star", Emoji = "⭐",
            Dots = new()
            {
                new(1,  0.50f, 0.08f),  // üst tepe
                new(2,  0.60f, 0.36f),  // iç sağ üst
                new(3,  0.90f, 0.37f),  // sağ dış
                new(4,  0.66f, 0.55f),  // iç sağ alt
                new(5,  0.75f, 0.84f),  // sağ alt dış
                new(6,  0.50f, 0.67f),  // iç alt
                new(7,  0.25f, 0.84f),  // sol alt dış
                new(8,  0.34f, 0.55f),  // iç sol alt
                new(9,  0.10f, 0.37f),  // sol dış
                new(10, 0.40f, 0.36f),  // iç sol üst
            }
        },
        new DotShape
        {
            Id = "heart", NameTr = "Kalp", NameEn = "Heart", Emoji = "❤️",
            Dots = new()
            {
                new(1,  0.50f, 0.92f),
                new(2,  0.73f, 0.72f),
                new(3,  0.90f, 0.50f),
                new(4,  0.88f, 0.27f),
                new(5,  0.67f, 0.13f),
                new(6,  0.50f, 0.28f),  // üst çentik
                new(7,  0.33f, 0.13f),
                new(8,  0.12f, 0.27f),
                new(9,  0.10f, 0.50f),
                new(10, 0.27f, 0.72f),
            }
        },
        new DotShape
        {
            Id = "fish", NameTr = "Balık", NameEn = "Fish", Emoji = "🐟",
            Dots = new()
            {
                new(1,  0.88f, 0.48f),  // ağız üst
                new(2,  0.80f, 0.27f),  // baş üst
                new(3,  0.52f, 0.12f),  // sırt üst
                new(4,  0.27f, 0.20f),  // kuyruk taban üst
                new(5,  0.05f, 0.10f),  // kuyruk üst uç
                new(6,  0.20f, 0.50f),  // kuyruk orta (içbükey)
                new(7,  0.05f, 0.90f),  // kuyruk alt uç
                new(8,  0.27f, 0.80f),  // kuyruk taban alt
                new(9,  0.52f, 0.88f),  // karın
                new(10, 0.80f, 0.73f),  // baş alt
            }
        },
        new DotShape
        {
            Id = "cat", NameTr = "Kedi", NameEn = "Cat", Emoji = "🐱",
            Dots = new()
            {
                new(1,  0.50f, 0.88f),  // çene
                new(2,  0.73f, 0.78f),  // sağ çene
                new(3,  0.90f, 0.58f),  // sağ yanak
                new(4,  0.85f, 0.35f),  // sağ kulak dış tabanı
                new(5,  0.73f, 0.12f),  // sağ kulak ucu
                new(6,  0.63f, 0.33f),  // sağ kulak iç tabanı
                new(7,  0.50f, 0.28f),  // alın
                new(8,  0.37f, 0.33f),  // sol kulak iç tabanı
                new(9,  0.27f, 0.12f),  // sol kulak ucu
                new(10, 0.15f, 0.35f),  // sol kulak dış tabanı
                new(11, 0.10f, 0.58f),  // sol yanak
                new(12, 0.27f, 0.78f),  // sol çene
            }
        },
        new DotShape
        {
            Id = "house", NameTr = "Ev", NameEn = "House", Emoji = "🏠",
            Dots = new()
            {
                new(1,  0.10f, 0.93f),  // sol alt
                new(2,  0.90f, 0.93f),  // sağ alt
                new(3,  0.90f, 0.48f),  // sağ duvar üst
                new(4,  0.97f, 0.48f),  // sağ saçak
                new(5,  0.50f, 0.08f),  // çatı tepesi
                new(6,  0.32f, 0.28f),  // baca sağ taban
                new(7,  0.28f, 0.13f),  // baca sağ üst
                new(8,  0.20f, 0.13f),  // baca sol üst
                new(9,  0.16f, 0.28f),  // baca sol taban
                new(10, 0.03f, 0.48f),  // sol saçak
                new(11, 0.10f, 0.48f),  // sol duvar üst
            }
        },
        new DotShape
        {
            Id = "rocket", NameTr = "Roket", NameEn = "Rocket", Emoji = "🚀",
            Dots = new()
            {
                new(1,  0.50f, 0.05f),  // burun
                new(2,  0.65f, 0.28f),  // gövde sağ üst
                new(3,  0.65f, 0.62f),  // gövde sağ alt
                new(4,  0.82f, 0.72f),  // sağ kanat ucu
                new(5,  0.65f, 0.80f),  // sağ kanat iç
                new(6,  0.65f, 0.95f),  // nozül sağ
                new(7,  0.35f, 0.95f),  // nozül sol
                new(8,  0.35f, 0.80f),  // sol kanat iç
                new(9,  0.18f, 0.72f),  // sol kanat ucu
                new(10, 0.35f, 0.62f),  // gövde sol alt
                new(11, 0.35f, 0.28f),  // gövde sol üst
            }
        },
    };
}
