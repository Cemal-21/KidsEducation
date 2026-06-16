using KidsEducation.ViewModels.Game;
using Microsoft.Maui.Graphics;

namespace KidsEducation.Views.Game;

// İki modda çalışır:
//  Reference = üstteki renkli örnek (hedef renkler ile dolu)
//  Paint     = alttaki boyanabilir iskelet (boş başlar, kullanıcı boyar)
public class ShapeColoringDrawable : IDrawable
{
    public ShapeColoringGameViewModel? ViewModel { get; set; }
    public bool IsReference { get; set; }

    private static readonly Color OutlineColor = Color.FromArgb("#172033");
    private static readonly Color EmptyFill = Color.FromArgb("#FFFFFF");

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var vm = ViewModel;
        if (vm?.Regions is null || vm.Regions.Count == 0) return;

        var size = new SizeF(dirtyRect.Width, dirtyRect.Height);

        foreach (var region in vm.Regions)
        {
            Color fill;
            if (IsReference)
                fill = Color.FromArgb(region.TargetColor);
            else
                fill = region.FilledColor is not null ? Color.FromArgb(region.FilledColor) : EmptyFill;

            if (region.IsCircle)
            {
                var c = ShapeGeometry.ToScreen(region.Center, size);
                var (scale, _, _) = ShapeGeometry.Transform(size);
                float r = region.Radius * scale;

                canvas.FillColor = fill;
                canvas.FillCircle(c.X, c.Y, r);

                canvas.StrokeColor = OutlineColor;
                canvas.StrokeSize = 2.5f;
                canvas.DrawCircle(c.X, c.Y, r);
            }
            else
            {
                var path = new PathF();
                var first = ShapeGeometry.ToScreen(region.Polygon[0], size);
                path.MoveTo(first.X, first.Y);
                for (int i = 1; i < region.Polygon.Length; i++)
                {
                    var p = ShapeGeometry.ToScreen(region.Polygon[i], size);
                    path.LineTo(p.X, p.Y);
                }
                path.Close();

                canvas.FillColor = fill;
                canvas.FillPath(path);

                canvas.StrokeColor = OutlineColor;
                canvas.StrokeSize = 2.5f;
                canvas.StrokeLineJoin = LineJoin.Round;
                canvas.DrawPath(path);
            }
        }
    }
}
