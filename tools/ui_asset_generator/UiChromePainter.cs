using TouhouWuxiaSurvivor.Tools.TileGenerator;

namespace TouhouWuxiaSurvivor.Tools.UiAssetGenerator;

/// <summary>
/// 手绘纸纹、朱砂印和祥云分隔等不随控件尺寸变化的武侠界面装饰资产。
/// </summary>
internal sealed class UiChromePainter
{
    private static readonly Rgba32 Transparent = new(0, 0, 0, 0);
    private static readonly Rgba32 Jade = UiPixelPalette.Jade;
    private static readonly Rgba32 Gold = UiPixelPalette.Gold;
    private static readonly Rgba32 Cinnabar = UiPixelPalette.Cinnabar;

    /// <summary>
    /// 生成可平铺的深色纸纤维，使用确定性模运算避免依赖随机状态。
    /// </summary>
    public PixelCanvas PaintPaperFiber()
    {
        var canvas = new PixelCanvas(64, 64);
        canvas.Fill(new Rgba32(18, 27, 20, 255));
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                int hash = (x * 37 + y * 61 + x * y * 3) % 97;
                if (hash < 4)
                {
                    canvas.SetPixel(x, y, new Rgba32(46, 54, 39, 118));
                }
                else if (hash > 93)
                {
                    canvas.SetPixel(x, y, new Rgba32(6, 11, 8, 126));
                }
            }
        }

        for (int strand = 0; strand < 11; strand++)
        {
            int x = (strand * 23 + 7) % 64;
            int y = (strand * 41 + 13) % 64;
            PixelDrawing.Line(canvas, x, y, x + 5 + strand % 4, y + strand % 2,
                new Rgba32(91, 96, 69, 72));
        }

        return canvas;
    }

    /// <summary>
    /// 绘制横向云纹分隔线，中点以朱砂菱形形成视觉停顿。
    /// </summary>
    public PixelCanvas PaintCloudDivider()
    {
        var canvas = new PixelCanvas(128, 8);
        canvas.Fill(Transparent);
        PixelDrawing.Line(canvas, 0, 4, 47, 4, new Rgba32(91, 105, 79, 190));
        PixelDrawing.Line(canvas, 81, 4, 127, 4, new Rgba32(91, 105, 79, 190));
        for (int offset = 0; offset < 2; offset++)
        {
            int x = 48 + offset * 18;
            PixelDrawing.Line(canvas, x, 4, x + 6, 1, Gold);
            PixelDrawing.Line(canvas, x + 6, 1, x + 12, 4, Gold);
            PixelDrawing.Line(canvas, x + 4, 5, x + 8, 5, UiPixelPalette.GoldDark);
        }

        PixelDrawing.FillRect(canvas, 62, 2, 4, 4, Cinnabar);
        canvas.SetPixel(63, 1, UiPixelPalette.CinnabarLight);
        canvas.SetPixel(64, 6, UiPixelPalette.CinnabarDark);
        return canvas;
    }

    /// <summary>
    /// 绘制无文字朱砂方印，以回纹和中心阴刻点避免依赖字体渲染。
    /// </summary>
    public PixelCanvas PaintSealStamp()
    {
        var canvas = new PixelCanvas(24, 24);
        canvas.Fill(Transparent);
        PixelDrawing.FillRect(canvas, 1, 2, 22, 20, UiPixelPalette.CinnabarDark);
        PixelDrawing.FillRect(canvas, 2, 1, 20, 22, UiPixelPalette.CinnabarDark);
        PixelDrawing.FillRect(canvas, 3, 3, 18, 18, Cinnabar);
        PixelDrawing.FillRect(canvas, 5, 5, 14, 14, new Rgba32(105, 24, 20, 255));
        Rgba32 carving = new(236, 197, 137, 255);
        PixelDrawing.Line(canvas, 5, 7, 17, 7, carving);
        PixelDrawing.Line(canvas, 7, 7, 7, 17, carving);
        PixelDrawing.Line(canvas, 7, 17, 18, 17, carving);
        PixelDrawing.Line(canvas, 17, 9, 17, 17, carving);
        PixelDrawing.FillRect(canvas, 10, 10, 5, 4, carving);
        canvas.SetPixel(4, 4, UiPixelPalette.CinnabarLight);
        canvas.SetPixel(19, 19, UiPixelPalette.CinnabarDark);
        return canvas;
    }
}
