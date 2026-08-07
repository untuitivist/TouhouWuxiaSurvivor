using TouhouWuxiaSurvivor.Tools.TileGenerator;

namespace TouhouWuxiaSurvivor.Tools.UiAssetGenerator;

/// <summary>
/// 手绘纸纹、卷轴边框、朱砂印与墨山等武侠界面装饰资产。
/// </summary>
internal sealed class UiChromePainter
{
    private static readonly Rgba32 Transparent = new(0, 0, 0, 0);
    private static readonly Rgba32 Ink = new(7, 14, 10, 245);
    private static readonly Rgba32 Jade = new(82, 104, 79, 255);
    private static readonly Rgba32 Gold = new(154, 136, 91, 255);
    private static readonly Rgba32 Cinnabar = new(143, 37, 29, 255);

    /// <summary>
    /// 生成可平铺的深色纸纤维，使用确定性模运算避免依赖随机状态。
    /// </summary>
    public PixelCanvas PaintPaperFiber()
    {
        var canvas = new PixelCanvas(64, 64);
        canvas.Fill(new Rgba32(17, 25, 20, 255));
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                int hash = (x * 37 + y * 61 + x * y * 3) % 97;
                if (hash < 5)
                {
                    canvas.SetPixel(x, y, new Rgba32(39, 48, 37, 92));
                }
                else if (hash > 92)
                {
                    canvas.SetPixel(x, y, new Rgba32(5, 10, 7, 82));
                }
            }
        }

        return canvas;
    }

    /// <summary>
    /// 绘制适合九宫格拉伸的卷轴面板，中心保持平坦、四角带朱砂收笔。
    /// </summary>
    public PixelCanvas PaintScrollPanel()
    {
        var canvas = new PixelCanvas(24, 24);
        canvas.Fill(new Rgba32(12, 20, 15, 246));
        PixelDrawing.FillRect(canvas, 1, 1, 22, 22, new Rgba32(26, 35, 27, 250));
        PixelDrawing.FillRect(canvas, 3, 3, 18, 18, new Rgba32(15, 24, 18, 252));
        FrameCorners(canvas, Gold, Cinnabar);
        return canvas;
    }

    /// <summary>
    /// 绘制动态图鉴窗九宫格边框，比通用面板增加双层金线与角部云头。
    /// </summary>
    public PixelCanvas PaintPreviewFrame()
    {
        var canvas = new PixelCanvas(24, 24);
        canvas.Fill(new Rgba32(5, 11, 8, 255));
        PixelDrawing.FillRect(canvas, 2, 2, 20, 20, new Rgba32(42, 54, 40, 255));
        PixelDrawing.FillRect(canvas, 4, 4, 16, 16, new Rgba32(8, 15, 11, 255));
        FrameCorners(canvas, Gold, Jade);
        return canvas;
    }

    /// <summary>
    /// 绘制横向云纹分隔线，中点以朱砂菱形形成视觉停顿。
    /// </summary>
    public PixelCanvas PaintCloudDivider()
    {
        var canvas = new PixelCanvas(128, 8);
        canvas.Fill(Transparent);
        PixelDrawing.Line(canvas, 0, 4, 51, 4, new Rgba32(91, 105, 79, 190));
        PixelDrawing.Line(canvas, 77, 4, 127, 4, new Rgba32(91, 105, 79, 190));
        for (int offset = 0; offset < 3; offset++)
        {
            PixelDrawing.Line(canvas, 52 + offset * 4, 4, 58 + offset * 4, 1, Gold);
            PixelDrawing.Line(canvas, 58 + offset * 4, 1, 64 + offset * 4, 4, Gold);
        }

        PixelDrawing.FillRect(canvas, 62, 2, 4, 4, Cinnabar);
        return canvas;
    }

    /// <summary>
    /// 绘制无文字朱砂方印，以回纹和中心阴刻点避免依赖字体渲染。
    /// </summary>
    public PixelCanvas PaintSealStamp()
    {
        var canvas = new PixelCanvas(24, 24);
        canvas.Fill(Transparent);
        PixelDrawing.FillRect(canvas, 2, 2, 20, 20, Cinnabar);
        PixelDrawing.FillRect(canvas, 4, 4, 16, 16, new Rgba32(86, 22, 18, 255));
        PixelDrawing.Line(canvas, 5, 6, 18, 6, new Rgba32(232, 187, 132, 255));
        PixelDrawing.Line(canvas, 6, 6, 6, 18, new Rgba32(232, 187, 132, 255));
        PixelDrawing.Line(canvas, 6, 18, 18, 18, new Rgba32(232, 187, 132, 255));
        PixelDrawing.Line(canvas, 18, 8, 18, 18, new Rgba32(232, 187, 132, 255));
        PixelDrawing.FillRect(canvas, 10, 9, 5, 5, new Rgba32(232, 187, 132, 255));
        return canvas;
    }

    /// <summary>
    /// 绘制透明墨山景，包括远山、近峰、月轮、神社剪影与少量朱砂灯火。
    /// </summary>
    public PixelCanvas PaintInkMountains()
    {
        var canvas = new PixelCanvas(320, 180);
        canvas.Fill(Transparent);
        PixelDrawing.FillEllipse(canvas, 251, 37, 22, 22, new Rgba32(205, 194, 155, 118));
        PixelDrawing.FillEllipse(canvas, 257, 32, 18, 18, new Rgba32(221, 209, 166, 152));
        DrawMountainRange(canvas, 116, new Rgba32(30, 50, 38, 150), 0);
        DrawMountainRange(canvas, 145, new Rgba32(15, 31, 23, 218), 29);
        DrawMist(canvas, 116, 58, new Rgba32(126, 143, 120, 68));
        DrawMountainRange(canvas, 169, new Rgba32(7, 18, 13, 245), 61);
        DrawMist(canvas, 151, 26, new Rgba32(110, 128, 104, 52));
        DrawPineGrove(canvas, 187, 149);
        DrawBambooGrove(canvas, 294, 138);
        DrawTorii(canvas, 239, 118);
        PixelDrawing.FillRect(canvas, 257, 138, 2, 2, new Rgba32(205, 72, 46, 255));
        PixelDrawing.FillRect(canvas, 274, 137, 2, 2, new Rgba32(205, 72, 46, 255));
        return canvas;
    }

    /// <summary>
    /// 在九宫格边框四角绘制金线转角和第二强调色像素。
    /// </summary>
    private static void FrameCorners(PixelCanvas canvas, Rgba32 line, Rgba32 accent)
    {
        PixelDrawing.Line(canvas, 0, 7, 0, 0, line);
        PixelDrawing.Line(canvas, 0, 0, 7, 0, line);
        PixelDrawing.Line(canvas, 16, 0, 23, 0, line);
        PixelDrawing.Line(canvas, 23, 0, 23, 7, line);
        PixelDrawing.Line(canvas, 0, 16, 0, 23, line);
        PixelDrawing.Line(canvas, 0, 23, 7, 23, line);
        PixelDrawing.Line(canvas, 16, 23, 23, 23, line);
        PixelDrawing.Line(canvas, 23, 16, 23, 23, line);
        canvas.SetPixel(2, 2, accent);
        canvas.SetPixel(21, 2, accent);
        canvas.SetPixel(2, 21, accent);
        canvas.SetPixel(21, 21, accent);
    }

    /// <summary>
    /// 按固定峰顶序列绘制一层山脉，并用偏移制造层间错落。
    /// </summary>
    private static void DrawMountainRange(PixelCanvas canvas, int baseline, Rgba32 color, int shift)
    {
        int[] peaks = [16, 62, 104, 151, 205, 254, 302, 348];
        for (int index = 0; index < peaks.Length - 1; index++)
        {
            int left = peaks[index] - shift;
            int right = peaks[index + 1] - shift;
            int top = baseline - 36 - (index * 17 % 38);
            PixelDrawing.FillTriangle(canvas, (left, baseline), ((left + right) / 2, top),
                (right, baseline), color);
            Rgba32 ridge = new(
                (byte)Math.Min(255, color.R + 20),
                (byte)Math.Min(255, color.G + 22),
                (byte)Math.Min(255, color.B + 18),
                (byte)Math.Min(255, color.A + 8));
            PixelDrawing.Line(canvas, (left + right) / 2, top, left + 9, baseline - 4, ridge);
            PixelDrawing.Line(canvas, (left + right) / 2, top, right - 13, baseline - 8, ridge);
        }

        PixelDrawing.FillRect(canvas, 0, baseline - 1, canvas.Width, canvas.Height - baseline + 1, color);
    }

    /// <summary>
    /// 绘制神社鸟居与屋檐剪影，作为幻想乡而非通用山水的识别点。
    /// </summary>
    private static void DrawTorii(PixelCanvas canvas, int x, int y)
    {
        Rgba32 silhouette = new(7, 12, 9, 255);
        PixelDrawing.FillTriangle(canvas, (x + 2, y - 7), (x + 42, y - 7),
            (x + 35, y - 12), silhouette);
        PixelDrawing.FillRect(canvas, x, y, 44, 4, silhouette);
        PixelDrawing.FillRect(canvas, x + 4, y - 4, 36, 3, silhouette);
        PixelDrawing.FillRect(canvas, x + 8, y + 4, 4, 29, silhouette);
        PixelDrawing.FillRect(canvas, x + 32, y + 4, 4, 29, silhouette);
        PixelDrawing.FillRect(canvas, x + 8, y + 12, 28, 3, silhouette);
    }

    /// <summary>
    /// 以断续横线画出穿山薄雾，留出透明间隙使远近山层仍可辨认。
    /// </summary>
    private static void DrawMist(PixelCanvas canvas, int y, int offset, Rgba32 color)
    {
        for (int segment = -1; segment < 7; segment++)
        {
            int x = segment * 58 + offset;
            PixelDrawing.Line(canvas, x, y, x + 34, y, color);
            PixelDrawing.Line(canvas, x + 8, y + 2, x + 46, y + 2, color);
        }
    }

    /// <summary>
    /// 用分层三角树冠和细树干组成近景松林，强化武侠山门的纵深和轮廓节奏。
    /// </summary>
    private static void DrawPineGrove(PixelCanvas canvas, int x, int baseline)
    {
        Rgba32 pine = new(5, 16, 11, 252);
        for (int index = 0; index < 5; index++)
        {
            int treeX = x + index * 9;
            int height = 22 + index % 3 * 5;
            PixelDrawing.FillRect(canvas, treeX, baseline - height, 2, height, pine);
            PixelDrawing.FillTriangle(canvas, (treeX - 6, baseline - height + 8),
                (treeX + 1, baseline - height), (treeX + 7, baseline - height + 8), pine);
            PixelDrawing.FillTriangle(canvas, (treeX - 7, baseline - height + 14),
                (treeX + 1, baseline - height + 5), (treeX + 8, baseline - height + 14), pine);
        }
    }

    /// <summary>
    /// 用细竿和交错竹叶补足画面右缘，让无限幻想乡具备东方题材的日常植被信号。
    /// </summary>
    private static void DrawBambooGrove(PixelCanvas canvas, int x, int baseline)
    {
        Rgba32 bamboo = new(16, 39, 24, 230);
        for (int index = 0; index < 4; index++)
        {
            int stemX = x + index * 6;
            PixelDrawing.Line(canvas, stemX, baseline, stemX + index % 2, baseline - 32, bamboo);
            PixelDrawing.Line(canvas, stemX, baseline - 18, stemX - 6, baseline - 23, bamboo);
            PixelDrawing.Line(canvas, stemX + 1, baseline - 25, stemX + 7, baseline - 29, bamboo);
        }
    }
}
