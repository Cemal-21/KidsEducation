using Microsoft.Maui.Graphics;

namespace KidsEducation.Models;

public class TracingDrawable : IDrawable
{
    private readonly List<List<PointF>> _strokes = new();
    private List<PointF>? _currentStroke;

    public double TotalStrokeLength { get; private set; }
    public int PointCount => _strokes.Sum(stroke => stroke.Count);

    public void StartStroke(PointF point)
    {
        _currentStroke = new List<PointF> { point };
        _strokes.Add(_currentStroke);
    }

    public void AddPoint(PointF point)
    {
        if (_currentStroke is null) return;

        if (_currentStroke.Count > 0)
        {
            var last = _currentStroke[^1];
            var dx = point.X - last.X;
            var dy = point.Y - last.Y;
            TotalStrokeLength += Math.Sqrt(dx * dx + dy * dy);
        }

        _currentStroke.Add(point);
    }

    public void EndStroke() => _currentStroke = null;

    public void Clear()
    {
        _strokes.Clear();
        _currentStroke = null;
        TotalStrokeLength = 0;
    }

    public RectF? GetStrokeBounds()
    {
        var points = _strokes.SelectMany(stroke => stroke).ToList();
        if (points.Count == 0) return null;

        var minX = points.Min(p => p.X);
        var minY = points.Min(p => p.Y);
        var maxX = points.Max(p => p.X);
        var maxY = points.Max(p => p.Y);

        return new RectF(minX, minY, maxX - minX, maxY - minY);
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.StrokeColor = Color.FromArgb("#6C63FF");
        canvas.StrokeSize = 14;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.StrokeLineJoin = LineJoin.Round;
        canvas.Alpha = 0.85f;

        foreach (var stroke in _strokes)
        {
            if (stroke.Count < 2) continue;

            var path = new PathF();
            path.MoveTo(stroke[0]);
            for (int i = 1; i < stroke.Count; i++)
                path.LineTo(stroke[i]);

            canvas.DrawPath(path);
        }
    }
}
