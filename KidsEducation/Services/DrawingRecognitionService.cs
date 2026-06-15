using KidsEducation.Models;
using Microsoft.Maui.Graphics;

namespace KidsEducation.Services;

/// <summary>
/// Heuristic-based shape recognizer. Analyzes stroke geometry without any ML dependency.
/// </summary>
public class DrawingRecognitionService
{
    public static readonly List<DrawingChallenge> Challenges = new()
    {
        new() { Id = "circle",   NameTr = "Daire",    NameEn = "Circle",   Emoji = "⭕", Hint = "Yuvarlak bir şekil çiz",        ShapeType = DrawingShapeType.Circle   },
        new() { Id = "square",   NameTr = "Kare",     NameEn = "Square",   Emoji = "⬜", Hint = "Dört köşeli bir kutu çiz",      ShapeType = DrawingShapeType.Square   },
        new() { Id = "triangle", NameTr = "Üçgen",    NameEn = "Triangle", Emoji = "🔺", Hint = "Üç kenarlı bir şekil çiz",      ShapeType = DrawingShapeType.Triangle },
        new() { Id = "star",     NameTr = "Yıldız",   NameEn = "Star",     Emoji = "⭐", Hint = "5 köşeli bir yıldız çiz",       ShapeType = DrawingShapeType.Star     },
        new() { Id = "heart",    NameTr = "Kalp",     NameEn = "Heart",    Emoji = "❤️", Hint = "Kalp şekli çiz",                ShapeType = DrawingShapeType.Heart    },
        new() { Id = "cross",    NameTr = "Artı",     NameEn = "Cross",    Emoji = "➕", Hint = "Artı işareti çiz",              ShapeType = DrawingShapeType.Cross    },
        new() { Id = "arrow",    NameTr = "Ok",       NameEn = "Arrow",    Emoji = "➡️", Hint = "Sağa bakan bir ok çiz",         ShapeType = DrawingShapeType.Arrow    },
        new() { Id = "zigzag",   NameTr = "Zikzak",  NameEn = "Zigzag",   Emoji = "〰️", Hint = "Zikzak çizgi çiz",             ShapeType = DrawingShapeType.ZigZag   },
        new() { Id = "wave",     NameTr = "Dalga",    NameEn = "Wave",     Emoji = "🌊", Hint = "Dalgalı bir çizgi çiz",         ShapeType = DrawingShapeType.Wave     },
        new() { Id = "spiral",   NameTr = "Spiral",   NameEn = "Spiral",   Emoji = "🌀", Hint = "Ortadan başlayıp dışa çıkan sarmal çiz", ShapeType = DrawingShapeType.Spiral },
    };

    public RecognitionResult Recognize(List<PointF> stroke, SizeF canvasSize)
    {
        if (stroke.Count < 10)
            return new RecognitionResult(DrawingShapeType.Circle, 0f, "Daha fazla çiz!");

        var features = ExtractFeatures(stroke, canvasSize);

        var scores = new Dictionary<DrawingShapeType, float>
        {
            [DrawingShapeType.Circle]   = ScoreCircle(features),
            [DrawingShapeType.Square]   = ScoreSquare(features),
            [DrawingShapeType.Triangle] = ScoreTriangle(features),
            [DrawingShapeType.Star]     = ScoreStar(features),
            [DrawingShapeType.Heart]    = ScoreHeart(features),
            [DrawingShapeType.Cross]    = ScoreCross(features),
            [DrawingShapeType.Arrow]    = ScoreArrow(features),
            [DrawingShapeType.ZigZag]   = ScoreZigZag(features),
            [DrawingShapeType.Wave]     = ScoreWave(features),
            [DrawingShapeType.Spiral]   = ScoreSpiral(features),
        };

        var best = scores.MaxBy(kv => kv.Value);
        return new RecognitionResult(best.Key, best.Value, null);
    }

    // ── Feature extraction ────────────────────────────────────────────────────

    private static StrokeFeatures ExtractFeatures(List<PointF> pts, SizeF canvas)
    {
        float minX = pts.Min(p => p.X), maxX = pts.Max(p => p.X);
        float minY = pts.Min(p => p.Y), maxY = pts.Max(p => p.Y);
        float width = maxX - minX;
        float height = maxY - minY;
        float cx = (minX + maxX) / 2f;
        float cy = (minY + maxY) / 2f;

        // aspect ratio (0 = tall, 1 = square, >1 = wide)
        float aspect = height > 0 ? width / height : 1f;

        // bounding box diagonal
        float diag = MathF.Sqrt(width * width + height * height);

        // total stroke length
        float totalLen = 0f;
        for (int i = 1; i < pts.Count; i++)
            totalLen += Distance(pts[i - 1], pts[i]);

        // closure ratio: distance between first and last point vs total length
        float closure = diag > 0 ? Distance(pts[0], pts[^1]) / diag : 1f;

        // direction changes (sign flips in dx)
        int xFlips = 0, yFlips = 0;
        for (int i = 2; i < pts.Count; i++)
        {
            float dx1 = pts[i - 1].X - pts[i - 2].X;
            float dx2 = pts[i].X - pts[i - 1].X;
            float dy1 = pts[i - 1].Y - pts[i - 2].Y;
            float dy2 = pts[i].Y - pts[i - 1].Y;
            if (dx1 * dx2 < 0) xFlips++;
            if (dy1 * dy2 < 0) yFlips++;
        }

        // average distance from center
        float avgDist = pts.Average(p => Distance(p, new PointF(cx, cy)));
        float distStdDev = 0f;
        if (diag > 0)
        {
            float mean = avgDist;
            distStdDev = MathF.Sqrt(pts.Average(p => MathF.Pow(Distance(p, new PointF(cx, cy)) - mean, 2)));
        }

        // circularity: how uniform is the distance from center?
        float circularity = avgDist > 0 ? 1f - (distStdDev / avgDist) : 0f;

        // overall density: stroke length vs bounding box area
        float boxArea = width * height;
        float density = boxArea > 0 ? totalLen / boxArea : 0f;

        // convexity approximation: count times we go far from center then come back
        float overlapRatio = OverlapRatio(pts);

        // net horizontal displacement (arrow detection)
        float netX = pts[^1].X - pts[0].X;
        float netY = pts[^1].Y - pts[0].Y;

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
            OverlapRatio = overlapRatio,
            NetX = netX,
            NetY = netY,
            PointCount = pts.Count,
        };
    }

    // ── Shape scorers ─────────────────────────────────────────────────────────

    private static float ScoreCircle(StrokeFeatures f)
    {
        float s = 0f;
        s += Clamp01(f.Circularity) * 40f;                     // uniform distance from center
        s += (1f - Clamp01(f.Closure)) * 25f;                  // closed (end near start)
        s += Gaussian(f.Aspect, 1f, 0.35f) * 20f;              // roughly square bounding box
        s += Clamp01(1f - f.XFlips / (float)f.PointCount) * 15f; // smooth (few direction changes)
        return s;
    }

    private static float ScoreSquare(StrokeFeatures f)
    {
        float s = 0f;
        s += Gaussian(f.Aspect, 1f, 0.3f) * 30f;
        s += (1f - Clamp01(f.Closure)) * 20f;
        s += Clamp01(f.XFlips / 4f) * 25f;   // ~4 corners = 4 x direction changes
        s += Clamp01(f.YFlips / 4f) * 25f;
        return s;
    }

    private static float ScoreTriangle(StrokeFeatures f)
    {
        float s = 0f;
        s += (1f - Clamp01(f.Closure)) * 25f;
        s += Gaussian(f.XFlips, 2f, 1.5f) * 35f;  // ~2 x-flips
        s += Gaussian(f.YFlips, 2f, 1.5f) * 30f;  // ~2 y-flips
        s += Gaussian(f.Aspect, 1.2f, 0.4f) * 10f;
        return s;
    }

    private static float ScoreStar(StrokeFeatures f)
    {
        float s = 0f;
        // Stars have many direction changes and overlap with themselves
        s += Gaussian(f.XFlips / (float)f.PointCount, 0.08f, 0.04f) * 30f;
        s += Clamp01(f.OverlapRatio) * 40f;
        s += Gaussian(f.Aspect, 1f, 0.4f) * 15f;
        s += (1f - Clamp01(f.Closure)) * 15f;
        return s;
    }

    private static float ScoreHeart(StrokeFeatures f)
    {
        float s = 0f;
        s += (1f - Clamp01(f.Closure)) * 20f;
        s += Gaussian(f.Aspect, 1.1f, 0.3f) * 25f;
        s += Gaussian(f.XFlips / (float)f.PointCount, 0.04f, 0.02f) * 30f;
        s += Gaussian(f.YFlips / (float)f.PointCount, 0.05f, 0.03f) * 25f;
        return s;
    }

    private static float ScoreCross(StrokeFeatures f)
    {
        float s = 0f;
        s += Gaussian(f.Aspect, 1f, 0.3f) * 25f;
        s += Gaussian(f.XFlips, 2f, 1f) * 35f;
        s += Gaussian(f.YFlips, 2f, 1f) * 35f;
        s += Clamp01(f.OverlapRatio) * 5f;
        return s;
    }

    private static float ScoreArrow(StrokeFeatures f)
    {
        float s = 0f;
        // Arrow: mostly horizontal net movement, few y direction changes
        float netDir = MathF.Abs(f.NetX) / (MathF.Abs(f.NetX) + MathF.Abs(f.NetY) + 1f);
        s += netDir * 35f;
        s += Clamp01(f.Closure) * 30f;  // end far from start (not closed)
        s += Gaussian(f.YFlips / (float)f.PointCount, 0.03f, 0.02f) * 25f;
        s += Gaussian(f.XFlips / (float)f.PointCount, 0.02f, 0.01f) * 10f;
        return s;
    }

    private static float ScoreZigZag(StrokeFeatures f)
    {
        float s = 0f;
        // ZigZag: many x or y flips, mostly linear net movement
        float flipRate = (f.XFlips + f.YFlips) / (float)f.PointCount;
        s += Gaussian(flipRate, 0.12f, 0.05f) * 50f;
        s += Clamp01(f.Aspect - 1f) * 25f;  // wide bounding box
        s += Clamp01(f.Closure) * 25f;
        return s;
    }

    private static float ScoreWave(StrokeFeatures f)
    {
        float s = 0f;
        // Wave: smooth y oscillation, net horizontal movement
        float yFlipRate = f.YFlips / (float)f.PointCount;
        s += Gaussian(yFlipRate, 0.06f, 0.03f) * 40f;
        s += Clamp01(f.Aspect - 0.5f) * 30f;  // wide
        s += Clamp01(f.Closure) * 20f;
        s += (1f - Clamp01(f.XFlips / (float)f.PointCount * 5f)) * 10f; // fewer x flips than zigzag
        return s;
    }

    private static float ScoreSpiral(StrokeFeatures f)
    {
        float s = 0f;
        // Spiral: many direction changes, density increases toward center, not closed
        s += Clamp01(f.Density / 3f) * 30f;
        s += Clamp01(f.Circularity * 0.5f) * 20f;
        s += Clamp01((f.XFlips + f.YFlips) / (float)f.PointCount * 3f) * 30f;
        s += Clamp01(f.Closure) * 20f;
        return s;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static float OverlapRatio(List<PointF> pts)
    {
        // Sample points and check if they're "close" to earlier points in the stroke
        int overlaps = 0;
        int step = Math.Max(1, pts.Count / 30);
        for (int i = pts.Count / 2; i < pts.Count; i += step)
        {
            for (int j = 0; j < i / 2; j += step)
            {
                if (Distance(pts[i], pts[j]) < 20f) { overlaps++; break; }
            }
        }
        return overlaps / (float)(pts.Count / step / 2 + 1);
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
    }
}

public record RecognitionResult(DrawingShapeType ShapeType, float Confidence, string? ErrorMessage);
