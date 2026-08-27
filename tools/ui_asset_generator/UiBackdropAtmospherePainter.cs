using TouhouWuxiaSurvivor.Tools.TileGenerator;

namespace TouhouWuxiaSurvivor.Tools.UiAssetGenerator;

/// <summary>
/// 在 640×360 背景上添加细像素星光、流云、落叶与水纹，统一原生分辨率的空气细节。
/// </summary>
internal sealed class UiBackdropAtmospherePainter
{
    /// <summary>绘制不会干扰左侧菜单识读的夜空和地景微细节。</summary>
    public void Draw(PixelCanvas canvas)
    {
        DrawStars(canvas);
        DrawCloudThreads(canvas);
        DrawDriftingLeaves(canvas);
        DrawWaterGlints(canvas);
    }

    /// <summary>使用固定坐标和三档亮度布置稀疏星点，避免随机噪声与装饰性光斑。</summary>
    private static void DrawStars(PixelCanvas canvas)
    {
        (int X, int Y)[] stars =
        [
            (37, 39), (91, 25), (148, 54), (216, 32), (274, 68),
            (338, 26), (389, 50), (436, 21), (575, 72), (592, 44),
            (117, 84), (309, 92), (418, 103), (617, 89), (253, 119),
        ];
        Rgba32[] colors =
        [
            new Rgba32(210, 195, 139),
            new Rgba32(102, 139, 137),
            new Rgba32(232, 219, 177),
        ];
        for (int index = 0; index < stars.Length; index++)
        {
            (int x, int y) = stars[index];
            canvas.SetPixel(x, y, colors[index % colors.Length]);
            if (index % 5 == 0)
            {
                canvas.SetPixel(x + 1, y, new Rgba32(226, 203, 142));
            }
        }
    }

    /// <summary>用断续的一像素云丝建立横向节奏，同时让标题附近保留干净负空间。</summary>
    private static void DrawCloudThreads(PixelCanvas canvas)
    {
        Rgba32 cloud = new(75, 94, 91);
        for (int band = 0; band < 3; band++)
        {
            int y = 107 + band * 24;
            int start = 28 + band * 173;
            PixelDrawing.Line(canvas, start, y, start + 91, y, cloud);
            PixelDrawing.Line(canvas, start + 26, y + 3, start + 137, y + 3, cloud);
            PixelDrawing.Line(canvas, start + 106, y - 2, start + 157, y - 2, cloud);
        }
    }

    /// <summary>在中右天空加入少量错位枫叶像素，为静态夜景提供方向感而不制造噪声。</summary>
    private static void DrawDriftingLeaves(PixelCanvas canvas)
    {
        foreach ((int x, int y) in new[] { (356, 132), (383, 119), (420, 145), (451, 126) })
        {
            canvas.SetPixel(x, y, new Rgba32(145, 54, 39));
            canvas.SetPixel(x + 1, y + 1, new Rgba32(190, 72, 43));
            PixelDrawing.Line(canvas, x + 2, y + 2, x + 4, y + 3,
                new Rgba32(86, 39, 31));
        }
    }

    /// <summary>在右下水面绘制错位短线，提供月光反射和前景层次。</summary>
    private static void DrawWaterGlints(PixelCanvas canvas)
    {
        Rgba32 gold = new(116, 102, 66);
        Rgba32 jade = new(42, 77, 70);
        for (int row = 0; row < 7; row++)
        {
            int y = 314 + row * 6;
            int x = 345 + row * 19;
            PixelDrawing.Line(canvas, x, y, x + 55 - row * 2, y,
                row % 2 == 0 ? gold : jade);
            PixelDrawing.Line(canvas, x + 105, y + 2, x + 145, y + 2, jade);
        }
    }
}
