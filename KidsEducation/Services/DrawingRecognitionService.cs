using KidsEducation.Models;
using Microsoft.Maui.Graphics;

namespace KidsEducation.Services;

/// <summary>
/// Heuristic shape recognizer tuned for finger drawings on small screens.
/// </summary>
public class DrawingRecognitionService
{
    public static readonly List<DrawingChallenge> Challenges = new()
    {
        new() { Id = "circle",   NameTr = "Daire",  NameEn = "Circle",   Emoji = "○", Hint = "Yuvarlak bir şekil çiz", ShapeType = DrawingShapeType.Circle },
        new() { Id = "square",   NameTr = "Kare",   NameEn = "Square",   Emoji = "□", Hint = "Dört köşeli bir kutu çiz", ShapeType = DrawingShapeType.Square },
        new() { Id = "triangle", NameTr = "Üçgen",  NameEn = "Triangle", Emoji = "△", Hint = "Üç kenarlı bir şekil çiz", ShapeType = DrawingShapeType.Triangle },
        new() { Id = "star",     NameTr = "Yıldız", NameEn = "Star",     Emoji = "☆", Hint = "5 köşeli bir yıldız çiz", ShapeType = DrawingShapeType.Star },
        new() { Id = "heart",    NameTr = "Kalp",   NameEn = "Heart",    Emoji = "♡", Hint = "Kalp şekli çiz", ShapeType = DrawingShapeType.Heart },
        new() { Id = "cross",    NameTr = "Artı",   NameEn = "Cross",    Emoji = "+", Hint = "Artı işareti çiz", ShapeType = DrawingShapeType.Cross },
        new() { Id = "arrow",    NameTr = "Ok",     NameEn = "Arrow",    Emoji = "→", Hint = "Sağa bakan bir ok çiz", ShapeType = DrawingShapeType.Arrow },
        new() { Id = "zigzag",   NameTr = "Zikzak", NameEn = "Zigzag",   Emoji = "Z", Hint = "Zikzak çizgi çiz", ShapeType = DrawingShapeType.ZigZag },
        new() { Id = "wave",     NameTr = "Dalga",  NameEn = "Wave",     Emoji = "~", Hint = "Dalgalı bir çizgi çiz", ShapeType = DrawingShapeType.Wave },
        new() { Id = "spiral",   NameTr = "Spiral", NameEn = "Spiral",   Emoji = "@", Hint = "Ortadan başlayıp dışa çıkan sarmal çiz", ShapeType = DrawingShapeType.Spiral },
    };

    public RecognitionResult Recognize(List<PointF> stroke, SizeF canvasSize) =>
        Recognize(new List<List<PointF>> { stroke }, canvasSize);

    public RecognitionResult Recognize(IReadOnlyList<List<PointF>> strokes, SizeF canvasSize)
    {
        var allPoints = Flatten(strokes);
        if (allPoints.Count < 10)
            return new RecognitionResult(DrawingShapeType.Circle, 0f, "Daha fazla çiz!");

        var features = ExtractFeatures(strokes, canvasSize);
        var scores = BuildScores(features);
        var best = scores.MaxBy(kv => kv.Value);
        return new RecognitionResult(best.Key, best.Value, null);
    }

    public RecognitionResult RecognizeTarget(List<PointF> stroke, SizeF canvasSize, DrawingShapeType target) =>
        RecognizeTarget(new List<List<PointF>> { stroke }, canvasSize, target);

    public RecognitionResult RecognizeTarget(IReadOnlyList<List<PointF>> strokes, SizeF canvasSize, DrawingShapeType target)
    {
        var allPoints = Flatten(strokes);
        if (allPoints.Count < 10)
            return new RecognitionResult(target, 0f, "Daha fazla çiz!");

        var features = ExtractFeatures(strokes, canvasSize);
        return new RecognitionResult(target, ScoreShape(target, features), null);
    }

    private static StrokeFeatures ExtractFeatures(IReadOnlyList<List<PointF>> strokes, SizeF canvas)
    {
        var cleanStrokes = strokes
            .Where(s => s.Count > 1)
            .Select(s => s.ToList())
            .ToList();
        var pts = cleanStrokes.SelectMany(s => s).ToList();

        float minX = pts.Min(p => p.X), maxX = pts.Max(p => p.X);
        float minY = pts.Min(p => p.Y), maxY = pts.Max(p => p.Y);
        float width = Math.Max(1f, maxX - minX);
        float height = Math.Max(1f, maxY - minY);
        float cx = (minX + maxX) / 2f;
        float cy = (minY + maxY) / 2f;
        float aspect = width / height;
        float diag = MathF.Sqrt(width * width + height * height);

        float totalLen = 0f;
        int xFlips = 0, yFlips = 0;
        foreach (var stroke in cleanStrokes)
        {
            for (int i = 1; i < stroke.Count; i++)
                totalLen += Distance(stroke[i - 1], stroke[i]);

            for (int i = 2; i < stroke.Count; i++)
            {
                float dx1 = stroke[i - 1].X - stroke[i - 2].X;
                float dx2 = stroke[i].X - stroke[i - 1].X;
                float dy1 = stroke[i - 1].Y - stroke[i - 2].Y;
                float dy2 = stroke[i].Y - stroke[i - 1].Y;
                if (MathF.Abs(dx1) > 1f && MathF.Abs(dx2) > 1f && dx1 * dx2 < 0) xFlips++;
                if (MathF.Abs(dy1) > 1f && MathF.Abs(dy2) > 1f && dy1 * dy2 < 0) yFlips++;
            }
        }

        float closure = cleanStrokes.Count == 1
            ? Distance(cleanStrokes[0][0], cleanStrokes[0][^1]) / diag
            : 1f;

        var center = new PointF(cx, cy);
        float avgDist = pts.Average(p => Distance(p, center));
        float distStdDev = avgDist > 0
            ? MathF.Sqrt(pts.Average(p => MathF.Pow(Distance(p, center) - avgDist, 2)))
            : 0f;
        float circularity = avgDist > 0 ? 1f - (distStdDev / avgDist) : 0f;
        float density = totalLen / Math.Max(1f, width * height);
        float netX = cleanStrokes[^1][^1].X - cleanStrokes[0][0].X;
        float netY = cleanStrokes[^1][^1].Y - cleanStrokes[0][0].Y;

        return new StrokeFeatures
        {
            Aspect = aspect,
            Closure = closure,
            XFlips = xFlips,
            YFlips = yFlips,
            Circularity = circularity,
            TotalLength = totalLen,
            BBoxDiag = diag,
            BBoxWidth = width,
            BBoxHeight = height,
            Density = density,
            OverlapRatio = OverlapRatio(pts, diag),
            NetX = netX,
            NetY = netY,
            PointCount = pts.Count,
            StrokeCount = cleanStrokes.Count,
            CornerCount = CountCorners(cleanStrokes, diag),
            Crossness = EstimateCrossness(cleanStrokes, minX, minY, width, height),
            Heartness = EstimateHeartness(pts, minX, minY, width, height),
        };
    }

    private static Dictionary<DrawingShapeType, float> BuildScores(StrokeFeatures features) => new()
    {
        [DrawingShapeType.Circle] = ScoreCircle(features),
        [DrawingShapeType.Square] = ScoreSquare(features),
        [DrawingShapeType.Triangle] = ScoreTriangle(features),
        [DrawingShapeType.Star] = ScoreStar(features),
        [DrawingShapeType.Heart] = ScoreHeart(features),
        [DrawingShapeType.Cross] = ScoreCross(features),
        [DrawingShapeType.Arrow] = ScoreArrow(features),
        [DrawingShapeType.ZigZag] = ScoreZigZag(features),
        [DrawingShapeType.Wave] = ScoreWave(features),
        [DrawingShapeType.Spiral] = ScoreSpiral(features),
    };

    private static float ScoreShape(DrawingShapeType type, StrokeFeatures features) => type switch
    {
        DrawingShapeType.Circle => ScoreCircle(features),
        DrawingShapeType.Square => ScoreSquare(features),
        DrawingShapeType.Triangle => ScoreTriangle(features),
        DrawingShapeType.Star => ScoreStar(features),
        DrawingShapeType.Heart => ScoreHeart(features),
        DrawingShapeType.Cross => ScoreCross(features),
        DrawingShapeType.Arrow => ScoreArrow(features),
        DrawingShapeType.ZigZag => ScoreZigZag(features),
        DrawingShapeType.Wave => ScoreWave(features),
        DrawingShapeType.Spiral => ScoreSpiral(features),
        _ => 0f
    };

    private static float ScoreCircle(StrokeFeatures f)
    {
        float s = 0f;
        s += Clamp01(f.Circularity) * 40f;
        s += (1f - Clamp01(f.Closure)) * 25f;
        s += Gaussian(f.Aspect, 1f, 0.35f) * 20f;
        s += Clamp01(1f - f.CornerCount / 8f) * 15f;
        return Math.Max(0f, s - f.Heartness * 45f - f.Crossness * 35f);
    }

    private static float ScoreSquare(StrokeFeatures f)
    {
        float s = 0f;
        s += Gaussian(f.Aspect, 1f, 0.3f) * 25f;
        s += (1f - Clamp01(f.Closure)) * 25f;
        s += Gaussian(f.CornerCount, 4f, 2f) * 40f;
        s += Clamp01(1f - f.Heartness) * 10f;
        return Math.Max(0f, s - f.Crossness * 35f);
    }

    private static float ScoreTriangle(StrokeFeatures f)
    {
        float s = 0f;
        s += (1f - Clamp01(f.Closure)) * 30f;
        s += Gaussian(f.CornerCount, 3f, 1.6f) * 45f;
        s += Gaussian(f.Aspect, 1.1f, 0.5f) * 15f;
        s += Clamp01(1f - f.Heartness) * 10f;
        return Math.Max(0f, s - f.Crossness * 35f);
    }

    private static float ScoreStar(StrokeFeatures f)
    {
        float s = 0f;
        s += Gaussian(f.CornerCount, 10f, 4f) * 35f;
        s += Clamp01(f.OverlapRatio) * 30f;
        s += Gaussian(f.Aspect, 1f, 0.45f) * 15f;
        s += (1f - Clamp01(f.Closure)) * 20f;
        return Math.Max(0f, s - f.Crossness * 30f);
    }

    private static float ScoreHeart(StrokeFeatures f)
    {
        float s = 0f;
        s += Clamp01(f.Heartness) * 60f;
        s += (1f - Clamp01(f.Closure)) * 15f;
        s += Gaussian(f.Aspect, 1f, 0.4f) * 15f;
        s += Gaussian(f.CornerCount, 2f, 3f) * 10f;
        return Math.Max(0f, s - f.Crossness * 35f);
    }

    private static float ScoreCross(StrokeFeatures f)
    {
        float s = 0f;
        s += Clamp01(f.Crossness) * 70f;
        s += Gaussian(f.Aspect, 1f, 0.45f) * 10f;
        s += Clamp01(f.StrokeCount / 2f) * 10f;
        s += Clamp01(f.Closure) * 10f;
        return s;
    }

    private static float ScoreArrow(StrokeFeatures f)
    {
        float netDir = MathF.Abs(f.NetX) / (MathF.Abs(f.NetX) + MathF.Abs(f.NetY) + 1f);
        float s = 0f;
        s += netDir * 35f;
        s += Clamp01(f.Closure) * 30f;
        s += Gaussian(f.CornerCount, 3f, 2f) * 25f;
        s += Gaussian(f.YFlips / (float)f.PointCount, 0.03f, 0.025f) * 10f;
        return Math.Max(0f, s - f.Crossness * 25f);
    }

    private static float ScoreZigZag(StrokeFeatures f)
    {
        float flipRate = (f.XFlips + f.YFlips) / (float)f.PointCount;
        float s = 0f;
        s += Gaussian(flipRate, 0.12f, 0.055f) * 45f;
        s += Gaussian(f.CornerCount, 5f, 3f) * 25f;
        s += Clamp01(f.Aspect - 1f) * 20f;
        s += Clamp01(f.Closure) * 10f;
        return Math.Max(0f, s - f.Crossness * 30f);
    }

    private static float ScoreWave(StrokeFeatures f)
    {
        float yFlipRate = f.YFlips / (float)f.PointCount;
        float xFlipRate = f.XFlips / (float)f.PointCount;
        float s = 0f;
        s += Gaussian(yFlipRate, 0.06f, 0.035f) * 40f;
        s += Clamp01(f.Aspect - 0.5f) * 25f;
        s += Clamp01(f.Closure) * 20f;
        s += (1f - Clamp01(xFlipRate * 5f)) * 15f;
        return Math.Max(0f, s - f.Crossness * 30f);
    }

    private static float ScoreSpiral(StrokeFeatures f)
    {
        float flipRate = (f.XFlips + f.YFlips) / (float)f.PointCount;
        float s = 0f;
        s += Clamp01(f.Density / 3f) * 30f;
        s += Clamp01(f.Circularity * 0.5f) * 20f;
        s += Clamp01(flipRate * 3f) * 30f;
        s += Clamp01(f.Closure) * 20f;
        return Math.Max(0f, s - f.Crossness * 30f);
    }

    private static List<PointF> Flatten(IReadOnlyList<List<PointF>> strokes) =>
        strokes.SelectMany(s => s).ToList();

    private static float OverlapRatio(List<PointF> pts, float diag)
    {
        int overlaps = 0;
        int step = Math.Max(1, pts.Count / 30);
        float threshold = Math.Max(12f, diag * 0.055f);

        for (int i = pts.Count / 2; i < pts.Count; i += step)
        {
            for (int j = 0; j < i / 2; j += step)
            {
                if (Distance(pts[i], pts[j]) < threshold)
                {
                    overlaps++;
                    break;
                }
            }
        }

        return overlaps / (float)(pts.Count / step / 2 + 1);
    }

    private static int CountCorners(List<List<PointF>> strokes, float diag)
    {
        if (diag <= 1f) return 0;

        int corners = 0;
        float tolerance = Math.Max(6f, diag * 0.045f);

        foreach (var stroke in strokes)
        {
            var simplified = SimplifyStroke(stroke, tolerance);
            if (simplified.Count < 3) continue;

            bool closed = Distance(simplified[0], simplified[^1]) < diag * 0.18f;
            if (closed && simplified.Count > 3)
                simplified.RemoveAt(simplified.Count - 1);

            int count = simplified.Count;
            int start = closed ? 0 : 1;
            int end = closed ? count : count - 1;

            for (int i = start; i < end; i++)
            {
                var prev = simplified[(i - 1 + count) % count];
                var current = simplified[i % count];
                var next = simplified[(i + 1) % count];
                if (TurnAngle(prev, current, next) > 38f)
                    corners++;
            }
        }

        return corners;
    }

    private static List<PointF> SimplifyStroke(List<PointF> points, float tolerance)
    {
        if (points.Count <= 2) return points.ToList();

        var keep = new bool[points.Count];
        keep[0] = true;
        keep[^1] = true;
        SimplifySection(points, 0, points.Count - 1, tolerance, keep);

        var simplified = new List<PointF>();
        for (int i = 0; i < points.Count; i++)
        {
            if (keep[i])
                simplified.Add(points[i]);
        }

        return simplified;
    }

    private static void SimplifySection(List<PointF> points, int start, int end, float tolerance, bool[] keep)
    {
        float maxDistance = 0f;
        int index = -1;

        for (int i = start + 1; i < end; i++)
        {
            float distance = PerpendicularDistance(points[i], points[start], points[end]);
            if (distance > maxDistance)
            {
                maxDistance = distance;
                index = i;
            }
        }

        if (index >= 0 && maxDistance > tolerance)
        {
            keep[index] = true;
            SimplifySection(points, start, index, tolerance, keep);
            SimplifySection(points, index, end, tolerance, keep);
        }
    }

    private static float PerpendicularDistance(PointF point, PointF lineStart, PointF lineEnd)
    {
        float dx = lineEnd.X - lineStart.X;
        float dy = lineEnd.Y - lineStart.Y;
        float lengthSquared = dx * dx + dy * dy;
        if (lengthSquared <= 0.001f) return Distance(point, lineStart);

        float t = ((point.X - lineStart.X) * dx + (point.Y - lineStart.Y) * dy) / lengthSquared;
        t = Math.Clamp(t, 0f, 1f);
        var projected = new PointF(lineStart.X + t * dx, lineStart.Y + t * dy);
        return Distance(point, projected);
    }

    private static float TurnAngle(PointF previous, PointF current, PointF next)
    {
        float ax = current.X - previous.X;
        float ay = current.Y - previous.Y;
        float bx = next.X - current.X;
        float by = next.Y - current.Y;
        float magA = MathF.Sqrt(ax * ax + ay * ay);
        float magB = MathF.Sqrt(bx * bx + by * by);
        if (magA <= 0.001f || magB <= 0.001f) return 0f;

        float dot = (ax * bx + ay * by) / (magA * magB);
        dot = Math.Clamp(dot, -1f, 1f);
        return MathF.Acos(dot) * 180f / MathF.PI;
    }

    private static float EstimateCrossness(List<List<PointF>> strokes, float minX, float minY, float width, float height)
    {
        if (width <= 1f || height <= 1f || strokes.Count < 2) return 0f;

        float horizontal = 0f;
        float vertical = 0f;

        foreach (var stroke in strokes)
        {
            float sx = stroke.Min(p => p.X);
            float ex = stroke.Max(p => p.X);
            float sy = stroke.Min(p => p.Y);
            float ey = stroke.Max(p => p.Y);
            float sw = (ex - sx) / width;
            float sh = (ey - sy) / height;
            float centerX = ((sx + ex) / 2f - minX) / width;
            float centerY = ((sy + ey) / 2f - minY) / height;
            float centerScore = (1f - Clamp01(MathF.Abs(centerX - 0.5f) / 0.35f))
                * (1f - Clamp01(MathF.Abs(centerY - 0.5f) / 0.35f));

            if (sw > sh * 1.8f)
                horizontal = Math.Max(horizontal, Clamp01(sw) * centerScore);
            if (sh > sw * 1.8f)
                vertical = Math.Max(vertical, Clamp01(sh) * centerScore);
        }

        return MathF.Sqrt(horizontal * vertical);
    }

    private static float EstimateHeartness(List<PointF> pts, float minX, float minY, float width, float height)
    {
        if (width <= 1f || height <= 1f) return 0f;

        var norm = pts
            .Select(p => new PointF((p.X - minX) / width, (p.Y - minY) / height))
            .ToList();

        var bottom = norm.MaxBy(p => p.Y);
        var bottomCentered = 1f - Clamp01(MathF.Abs(bottom.X - 0.5f) / 0.35f);
        var topWidth = RangeWidth(norm.Where(p => p.Y >= 0.12f && p.Y <= 0.58f));
        var bottomWidth = RangeWidth(norm.Where(p => p.Y >= 0.72f));
        var topWide = Clamp01((topWidth - 0.55f) / 0.35f);
        var bottomNarrow = Clamp01((0.55f - bottomWidth) / 0.4f);

        var sideTopPoints = norm.Where(p => p.X < 0.38f || p.X > 0.62f).ToList();
        var centerTopPoints = norm.Where(p => p.X >= 0.38f && p.X <= 0.62f && p.Y < 0.55f).ToList();
        var sideTop = sideTopPoints.Count > 0 ? sideTopPoints.Min(p => p.Y) : 0f;
        var centerTop = centerTopPoints.Count > 0 ? centerTopPoints.Min(p => p.Y) : sideTop;
        var topDip = Clamp01((centerTop - sideTop) / 0.22f);
        var aspectScore = Gaussian(width / height, 1f, 0.45f);

        return Clamp01(
            bottomCentered * 0.25f +
            topWide * 0.2f +
            bottomNarrow * 0.25f +
            topDip * 0.2f +
            aspectScore * 0.1f);
    }

    private static float RangeWidth(IEnumerable<PointF> points)
    {
        var list = points.ToList();
        return list.Count == 0 ? 0f : list.Max(p => p.X) - list.Min(p => p.X);
    }

    private static float Distance(PointF a, PointF b) =>
        MathF.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));

    private static float Clamp01(float v) => Math.Clamp(v, 0f, 1f);

    private static float Gaussian(float x, float mean, float sigma) =>
        MathF.Exp(-0.5f * MathF.Pow((x - mean) / sigma, 2));

    private class StrokeFeatures
    {
        public float Aspect;
        public float Closure;
        public int XFlips;
        public int YFlips;
        public float Circularity;
        public float TotalLength;
        public float BBoxDiag;
        public float BBoxWidth;
        public float BBoxHeight;
        public float Density;
        public float OverlapRatio;
        public float NetX;
        public float NetY;
        public int PointCount;
        public int StrokeCount;
        public int CornerCount;
        public float Crossness;
        public float Heartness;
    }
}

public record RecognitionResult(DrawingShapeType ShapeType, float Confidence, string? ErrorMessage);
