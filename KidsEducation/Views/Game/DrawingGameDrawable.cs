using KidsEducation.ViewModels.Game;
using Microsoft.Maui.Graphics;

namespace KidsEducation.Views.Game;

public class DrawingGameDrawable : IDrawable
{
    public DrawingGameViewModel? ViewModel { get; set; }

    private static readonly Color StrokeColor = Color.FromArgb("#3B3799");
    private static readonly Color BgColor = Color.FromArgb("#F8F7FF");
    private static readonly Color GridColor = Color.FromArgb("#EAE8FF");

    public void Draw(ICanvas canvas, RectF rect)
    {
        var vm = ViewModel;

        // Arka plan
        canvas.FillColor = BgColor;
        canvas.FillRectangle(rect);

        // Hafif grid (kağıt hissi)
        DrawGrid(canvas, rect);

        if (vm is null) return;

        // Tamamlanan çizgiler
        canvas.StrokeColor = StrokeColor;
        canvas.StrokeSize = 5f;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.StrokeLineJoin = LineJoin.Round;

        foreach (var stroke in vm.Strokes)
        {
            if (stroke.Count < 2) continue;
            var path = BuildPath(stroke);
            canvas.DrawPath(path);
        }

        // Aktif çizgi
        if (vm.CurrentStroke is { Count: >= 2 })
        {
            canvas.StrokeColor = Color.FromArgb("#5148D4");
            var path = BuildPath(vm.CurrentStroke);
            canvas.DrawPath(path);
        }

        // Sonuç overlay
        if (vm.State == DrawingGameState.Result)
        {
            canvas.FillColor = vm.IsCorrect
                ? Color.FromArgb("#2016A34A")
                : Color.FromArgb("#20DC2626");
            canvas.FillRectangle(rect);
        }
    }

    private static void DrawGrid(ICanvas canvas, RectF rect)
    {
        canvas.StrokeColor = GridColor;
        canvas.StrokeSize = 1f;
        const float step = 32f;
        for (float x = step; x < rect.Width; x += step)
            canvas.DrawLine(x, 0, x, rect.Height);
        for (float y = step; y < rect.Height; y += step)
            canvas.DrawLine(0, y, rect.Width, y);
    }

    private static PathF BuildPath(List<PointF> pts)
    {
        var path = new PathF();
        path.MoveTo(pts[0].X, pts[0].Y);
        for (int i = 1; i < pts.Count; i++)
            path.LineTo(pts[i].X, pts[i].Y);
        return path;
    }
}
