using TouhouWuxiaSurvivor.Tools.TileGenerator;

namespace TouhouWuxiaSurvivor.Tools.UiAssetGenerator;

/// <summary>
/// 绘制月下神社、鸟居、参道与近景枝叶，以可辨识建筑替代背景中的抽象几何占位。
/// </summary>
internal sealed class UiBackdropShrinePainter
{
    /// <summary>先铺参道，再绘神社与鸟居，最后用灯笼和枝叶形成明确的前后遮挡。</summary>
    public void Draw(PixelCanvas canvas)
    {
        DrawApproach(canvas);
        DrawShrine(canvas);
        DrawTorii(canvas);
        DrawLanterns(canvas);
        DrawForegroundBranches(canvas);
    }

    /// <summary>使用向下展开的石阶多边形连接鸟居与前景，建立可行走空间和画面透视。</summary>
    private static void DrawApproach(PixelCanvas canvas)
    {
        PixelDrawing.FillPolygon(canvas,
            [(392, 249), (466, 249), (572, 360), (252, 360)],
            new Rgba32(34, 40, 36));
        PixelDrawing.FillPolygon(canvas,
            [(407, 252), (451, 252), (516, 360), (307, 360)],
            new Rgba32(48, 50, 41));
        for (int step = 0; step < 7; step++)
        {
            int y = 267 + step * 13;
            int halfWidth = 34 + step * 15;
            PixelDrawing.Line(canvas, 429 - halfWidth, y, 429 + halfWidth, y,
                new Rgba32(74, 72, 55));
        }
    }

    /// <summary>以宽飞檐、瓦脊、朱柱和透光障子组合出右侧神社主体，形成首页视觉焦点。</summary>
    private static void DrawShrine(PixelCanvas canvas)
    {
        PixelDrawing.FillRect(canvas, 423, 207, 217, 119, new Rgba32(38, 22, 21));
        PixelDrawing.FillRect(canvas, 438, 224, 202, 102, new Rgba32(74, 31, 27));
        PixelDrawing.FillRect(canvas, 452, 233, 188, 93, new Rgba32(27, 29, 27));
        DrawShoji(canvas, 466, 241, 48, 54);
        DrawShoji(canvas, 564, 241, 48, 54);
        PixelDrawing.FillRect(canvas, 522, 230, 34, 96, new Rgba32(9, 14, 15));
        PixelDrawing.FillRect(canvas, 528, 240, 22, 86, new Rgba32(52, 22, 22));
        PixelDrawing.FillRect(canvas, 424, 211, 9, 115, new Rgba32(115, 35, 30));
        PixelDrawing.FillRect(canvas, 623, 211, 9, 115, new Rgba32(105, 31, 28));

        (int X, int Y)[] roof =
        [
            (391, 213), (408, 207), (433, 203), (457, 192), (480, 188),
            (501, 177), (531, 181), (555, 191), (585, 198), (611, 202),
            (639, 211), (658, 214), (647, 227), (402, 227),
        ];
        PixelDrawing.FillPolygon(canvas, roof, new Rgba32(7, 14, 16));
        PixelDrawing.FillPolygon(canvas,
            [(406, 210), (462, 198), (505, 183), (533, 186), (584, 202),
             (642, 215), (635, 219), (414, 219)],
            new Rgba32(26, 35, 34));
        PixelDrawing.Line(canvas, 398, 216, 648, 220, new Rgba32(94, 75, 46));
        PixelDrawing.Line(canvas, 451, 197, 532, 180, new Rgba32(126, 98, 51));
        PixelDrawing.FillRect(canvas, 493, 176, 45, 5, new Rgba32(12, 18, 19));
        PixelDrawing.FillRect(canvas, 501, 172, 29, 3, new Rgba32(131, 95, 48));
        for (int x = 432; x < 633; x += 17)
        {
            PixelDrawing.Line(canvas, x, 210, x + 34, 216, new Rgba32(45, 55, 50));
        }
    }

    /// <summary>绘制带木格与暖色纸面的障子窗，让建筑在深色场景中拥有局部生活气息。</summary>
    private static void DrawShoji(PixelCanvas canvas, int x, int y, int width, int height)
    {
        PixelDrawing.FillRect(canvas, x, y, width, height, new Rgba32(131, 106, 68));
        PixelDrawing.FillRect(canvas, x + 3, y + 3, width - 6, height - 6,
            new Rgba32(189, 158, 96));
        for (int line = 1; line < 4; line++)
        {
            int gridX = x + line * width / 4;
            PixelDrawing.FillRect(canvas, gridX, y + 2, 2, height - 4,
                new Rgba32(64, 39, 31));
        }
        for (int line = 1; line < 3; line++)
        {
            int gridY = y + line * height / 3;
            PixelDrawing.FillRect(canvas, x + 2, gridY, width - 4, 2,
                new Rgba32(64, 39, 31));
        }
    }

    /// <summary>在参道入口设置朱砂鸟居，并以不等宽木构和暗侧边体现像素体积。</summary>
    private static void DrawTorii(PixelCanvas canvas)
    {
        PixelDrawing.FillRect(canvas, 334, 218, 9, 70, new Rgba32(104, 28, 25));
        PixelDrawing.FillRect(canvas, 399, 218, 9, 70, new Rgba32(91, 24, 23));
        PixelDrawing.FillRect(canvas, 326, 213, 90, 8, new Rgba32(151, 40, 34));
        PixelDrawing.FillRect(canvas, 319, 209, 104, 5, new Rgba32(82, 26, 25));
        PixelDrawing.FillRect(canvas, 339, 230, 64, 5, new Rgba32(125, 33, 29));
        PixelDrawing.FillRect(canvas, 331, 286, 15, 4, new Rgba32(46, 43, 37));
        PixelDrawing.FillRect(canvas, 396, 286, 15, 4, new Rgba32(46, 43, 37));
        PixelDrawing.Line(canvas, 323, 209, 357, 205, new Rgba32(177, 54, 39));
        PixelDrawing.Line(canvas, 386, 205, 419, 209, new Rgba32(117, 34, 30));
    }

    /// <summary>沿参道布置纸灯笼和木桩，用小面积暖色建立节奏而不形成装饰性光球。</summary>
    private static void DrawLanterns(PixelCanvas canvas)
    {
        (int X, int Y, int Scale)[] lanterns =
        [
            (314, 276, 1), (429, 266, 1), (273, 317, 2), (488, 310, 2),
        ];
        foreach ((int x, int y, int scale) in lanterns)
        {
            PixelDrawing.FillRect(canvas, x - scale, y, 8 * scale, 10 * scale,
                new Rgba32(87, 28, 25));
            PixelDrawing.FillRect(canvas, x + scale, y + 2 * scale, 4 * scale, 6 * scale,
                new Rgba32(235, 167, 78));
            PixelDrawing.Line(canvas, x + 3 * scale, y + 10 * scale,
                x + 3 * scale, y + 18 * scale, new Rgba32(36, 29, 26));
        }
    }

    /// <summary>用右上松枝与底部芒草压住画面边缘，形成摄影式前景而非满屏重复树形。</summary>
    private static void DrawForegroundBranches(PixelCanvas canvas)
    {
        Rgba32 branch = new(5, 13, 16);
        Rgba32 needle = new(12, 30, 27);
        PixelDrawing.Line(canvas, 642, 14, 570, 53, branch);
        PixelDrawing.Line(canvas, 618, 28, 589, 8, branch);
        PixelDrawing.Line(canvas, 604, 35, 548, 29, branch);
        for (int index = 0; index < 9; index++)
        {
            int x = 574 + index * 7;
            int y = 39 - index * 3;
            PixelDrawing.Line(canvas, x, y, x - 14, y - 10, needle);
            PixelDrawing.Line(canvas, x, y, x - 17, y + 6, needle);
        }
        for (int x = 8; x < 640; x += 19)
        {
            int height = 12 + x % 17;
            PixelDrawing.Line(canvas, x, 360, x + x % 5 - 2, 360 - height, branch);
            PixelDrawing.Line(canvas, x + 2, 351, x + 8, 345 - x % 4, needle);
        }
    }
}
