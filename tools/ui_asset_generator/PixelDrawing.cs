using TouhouWuxiaSurvivor.Tools.TileGenerator;

namespace TouhouWuxiaSurvivor.Tools.UiAssetGenerator;

/// <summary>
/// 为 UI 像素资产提供矩形、直线、圆、三角形和任意多边形基础绘制操作。
/// </summary>
internal static class PixelDrawing
{
    /// <summary>
    /// 使用半开区间尺寸填充矩形，越界像素由画布安全忽略。
    /// </summary>
    public static void FillRect(
        PixelCanvas canvas, int x, int y, int width, int height, Rgba32 color)
    {
        for (int py = y; py < y + height; py++)
        {
            for (int px = x; px < x + width; px++)
            {
                canvas.SetPixel(px, py, color);
            }
        }
    }

    /// <summary>
    /// 使用 Bresenham 算法绘制单像素硬边直线，适合低分辨率装饰纹样。
    /// </summary>
    public static void Line(
        PixelCanvas canvas, int x0, int y0, int x1, int y1, Rgba32 color)
    {
        int dx = Math.Abs(x1 - x0);
        int sx = x0 < x1 ? 1 : -1;
        int dy = -Math.Abs(y1 - y0);
        int sy = y0 < y1 ? 1 : -1;
        int error = dx + dy;
        while (true)
        {
            canvas.SetPixel(x0, y0, color);
            if (x0 == x1 && y0 == y1)
            {
                break;
            }

            int doubled = error * 2;
            if (doubled >= dy)
            {
                error += dy;
                x0 += sx;
            }

            if (doubled <= dx)
            {
                error += dx;
                y0 += sy;
            }
        }
    }

    /// <summary>
    /// 填充圆形或椭圆近似，用于月亮、印章和敌人轮廓。
    /// </summary>
    public static void FillEllipse(
        PixelCanvas canvas, int centerX, int centerY, int radiusX, int radiusY, Rgba32 color)
    {
        for (int y = -radiusY; y <= radiusY; y++)
        {
            for (int x = -radiusX; x <= radiusX; x++)
            {
                if (x * x * radiusY * radiusY + y * y * radiusX * radiusX <=
                    radiusX * radiusX * radiusY * radiusY)
                {
                    canvas.SetPixel(centerX + x, centerY + y, color);
                }
            }
        }
    }

    /// <summary>
    /// 以扫描线填充由三个整数顶点组成的三角形，供山体和衣摆使用。
    /// </summary>
    public static void FillTriangle(
        PixelCanvas canvas,
        (int X, int Y) a,
        (int X, int Y) b,
        (int X, int Y) c,
        Rgba32 color)
    {
        int minX = Math.Min(a.X, Math.Min(b.X, c.X));
        int maxX = Math.Max(a.X, Math.Max(b.X, c.X));
        int minY = Math.Min(a.Y, Math.Min(b.Y, c.Y));
        int maxY = Math.Max(a.Y, Math.Max(b.Y, c.Y));
        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                int first = Edge(a, b, x, y);
                int second = Edge(b, c, x, y);
                int third = Edge(c, a, x, y);
                if ((first >= 0 && second >= 0 && third >= 0) ||
                    (first <= 0 && second <= 0 && third <= 0))
                {
                    canvas.SetPixel(x, y, color);
                }
            }
        }
    }

    /// <summary>
    /// 使用奇偶扫描线规则填充任意简单多边形，供山脊、屋檐和道路绘制自然的不规则轮廓。
    /// </summary>
    public static void FillPolygon(
        PixelCanvas canvas, IReadOnlyList<(int X, int Y)> points, Rgba32 color)
    {
        if (points.Count < 3)
        {
            return;
        }

        int minY = points.Min(point => point.Y);
        int maxY = points.Max(point => point.Y);
        var intersections = new List<int>(points.Count);
        for (int y = minY; y <= maxY; y++)
        {
            intersections.Clear();
            for (int index = 0; index < points.Count; index++)
            {
                (int X, int Y) first = points[index];
                (int X, int Y) second = points[(index + 1) % points.Count];
                if ((first.Y <= y && second.Y > y) ||
                    (second.Y <= y && first.Y > y))
                {
                    int x = first.X + (y - first.Y) * (second.X - first.X) /
                        (second.Y - first.Y);
                    intersections.Add(x);
                }
            }

            intersections.Sort();
            for (int index = 0; index + 1 < intersections.Count; index += 2)
            {
                FillRect(canvas, intersections[index], y,
                    intersections[index + 1] - intersections[index] + 1, 1, color);
            }
        }
    }

    /// <summary>
    /// 计算点相对有向边的二维叉积符号，供三角形内部测试使用。
    /// </summary>
    private static int Edge((int X, int Y) a, (int X, int Y) b, int x, int y) =>
        (x - a.X) * (b.Y - a.Y) - (y - a.Y) * (b.X - a.X);
}
