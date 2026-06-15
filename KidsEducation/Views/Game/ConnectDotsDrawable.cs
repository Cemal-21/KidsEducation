using KidsEducation.ViewModels.Game;
using Microsoft.Maui.Graphics;

namespace KidsEducation.Views.Game;

public class ConnectDotsDrawable : IDrawable
{
    public ConnectDotsGameViewModel? ViewModel { get; set; }

    private static readonly Color LineColor = Color.FromArgb("#5148D4");
    private static readonly Color DotConnected = Color.FromArgb("#5148D4");
    private static readonly Color DotNext = Color.FromArgb("#FF9A3C");
    private static readonly Color DotPending = Color.FromArgb("#D1D5DB");
    private static readonly Color FillComplete = Color.FromArgb("#3016A34A");

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var vm = ViewModel;
        if (vm?.CurrentShape is null) return;

        var dots = vm.CurrentShape.Dots;
        var connected = vm.NextDotIndex;
        var size = new SizeF(dirtyRect.Width, dirtyRect.Height);

        // 1. Şekil tamamlandıysa içini doldur
        if (vm.IsCompleted && dots.Count > 2)
        {
            var path = new PathF();
            var first = ConnectDotsGameViewModel.ToScreen(dots[0], size);
            path.MoveTo(first.X, first.Y);
            for (int i = 1; i < dots.Count; i++)
            {
                var p = ConnectDotsGameViewModel.ToScreen(dots[i], size);
                path.LineTo(p.X, p.Y);
            }
            path.Close();
            canvas.FillColor = FillComplete;
            canvas.FillPath(path);
        }

        // 2. Bağlanan çizgiler
        canvas.StrokeColor = LineColor;
        canvas.StrokeSize = 5f;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.StrokeLineJoin = LineJoin.Round;

        for (int i = 0; i < connected - 1; i++)
        {
            var from = ConnectDotsGameViewModel.ToScreen(dots[i], size);
            var to = ConnectDotsGameViewModel.ToScreen(dots[i + 1], size);
            canvas.DrawLine(from.X, from.Y, to.X, to.Y);
        }

        // Tamamlandıysa kapanış çizgisi
        if (vm.IsCompleted && dots.Count > 1)
        {
            var last = ConnectDotsGameViewModel.ToScreen(dots[dots.Count - 1], size);
            var first = ConnectDotsGameViewModel.ToScreen(dots[0], size);
            canvas.DrawLine(last.X, last.Y, first.X, first.Y);
        }

        // 3. Canlı çizgi (parmak takibi)
        if (!vm.IsCompleted && vm.IsTouching && connected > 0)
        {
            var fromDot = ConnectDotsGameViewModel.ToScreen(dots[connected - 1], size);
            canvas.StrokeColor = Color.FromArgb("#885148D4");
            canvas.StrokeSize = 3f;
            canvas.StrokeDashPattern = new float[] { 7, 5 };
            canvas.DrawLine(fromDot.X, fromDot.Y, vm.CurrentTouchPoint.X, vm.CurrentTouchPoint.Y);
            canvas.StrokeDashPattern = null;
            canvas.StrokeSize = 5f;
        }

        // 4. Noktaları çiz
        for (int i = 0; i < dots.Count; i++)
        {
            var dot = dots[i];
            var pos = ConnectDotsGameViewModel.ToScreen(dot, size);
            bool isConn = i < connected;
            bool isNext = i == connected && !vm.IsCompleted;

            float radius = isNext ? 24f : 18f;

            // Gölge efekti (next dot)
            if (isNext)
            {
                canvas.FillColor = Color.FromArgb("#30FF9A3C");
                canvas.FillCircle(pos.X, pos.Y, radius + 8f);
            }

            // Daire
            canvas.FillColor = isConn ? DotConnected : isNext ? DotNext : DotPending;
            canvas.FillCircle(pos.X, pos.Y, radius);

            // Next dot kenar halkası
            if (isNext)
            {
                canvas.StrokeColor = Color.FromArgb("#FF6B00");
                canvas.StrokeSize = 2.5f;
                canvas.DrawCircle(pos.X, pos.Y, radius + 3f);
            }

            // Numara
            canvas.FontColor = (isConn || isNext) ? Colors.White : Color.FromArgb("#9CA3AF");
            canvas.FontSize = isNext ? 15f : 12f;
            canvas.DrawString(
                dot.Number.ToString(),
                pos.X - radius, pos.Y - radius,
                radius * 2, radius * 2,
                HorizontalAlignment.Center,
                VerticalAlignment.Center);
        }
    }
}
