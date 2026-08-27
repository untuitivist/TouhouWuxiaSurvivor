using TouhouWuxiaSurvivor.Tools.TileGenerator;

namespace TouhouWuxiaSurvivor.Tools.UiAssetGenerator;

/// <summary>
/// 绘制主菜单的幻想乡墨山长景，以多层色块和建筑细节替代只有三角轮廓的占位山水。
/// </summary>
internal sealed class UiBackdropPainter
{
    /// <summary>
    /// 生成包含月轮、远中近山、瀑布、雾带、神社、石桥、松竹与水光的透明像素景。
    /// </summary>
    public PixelCanvas PaintInkMountains()
    {
        var canvas = new PixelCanvas(320, 180);
        canvas.Fill(UiPixelPalette.Transparent);
        DrawMoon(canvas);
        DrawMountainLayer(canvas, 112, 0, new Rgba32(46, 70, 52, 150),
            new Rgba32(73, 94, 68, 125));
        DrawMountainLayer(canvas, 142, 23, new Rgba32(23, 43, 31, 218),
            new Rgba32(48, 70, 50, 190));
        DrawWaterfall(canvas, 147, 82);
        DrawMist(canvas, 116, 11, new Rgba32(154, 162, 130, 76));
        DrawMountainLayer(canvas, 169, 57, new Rgba32(8, 22, 15, 250),
            new Rgba32(26, 46, 32, 232));
        DrawMist(canvas, 148, 39, new Rgba32(131, 143, 112, 61));
        DrawShrine(canvas, 228, 119);
        DrawBridge(canvas, 183, 151);
        DrawPineGrove(canvas, 171, 160);
        DrawBambooGrove(canvas, 292, 151);
        DrawWater(canvas, 162);
        return canvas;
    }

    /// <summary>
    /// 用三层不规则圆面、暗侧月影和稀疏亮点绘制带像素体积的月轮。
    /// </summary>
    private static void DrawMoon(PixelCanvas canvas)
    {
        PixelDrawing.FillEllipse(canvas, 257, 34, 24, 24, new Rgba32(114, 105, 76, 78));
        PixelDrawing.FillEllipse(canvas, 257, 32, 20, 20, new Rgba32(207, 191, 138, 185));
        PixelDrawing.FillEllipse(canvas, 260, 29, 17, 17, new Rgba32(230, 214, 161, 224));
        PixelDrawing.FillEllipse(canvas, 249, 32, 5, 8, new Rgba32(183, 167, 118, 82));
        PixelDrawing.FillRect(canvas, 266, 20, 3, 2, new Rgba32(245, 229, 177, 170));
        canvas.SetPixel(248, 17, new Rgba32(237, 218, 164, 140));
        canvas.SetPixel(276, 39, new Rgba32(237, 218, 164, 120));
    }

    /// <summary>
    /// 按固定峰列生成山体，并叠加背光坡面、岩脊、台地和零散墨点形成真实层理。
    /// </summary>
    private static void DrawMountainLayer(
        PixelCanvas canvas, int baseline, int shift, Rgba32 body, Rgba32 ridge)
    {
        int[] widths = [47, 39, 55, 42, 50, 44, 53, 41];
        int[] heights = [38, 61, 47, 77, 52, 69, 43, 58];
        int left = -shift - 18;
        for (int index = 0; index < widths.Length; index++)
        {
            int right = left + widths[index];
            int peakX = left + widths[index] * (index % 3 + 2) / 5;
            int peakY = baseline - heights[index];
            PixelDrawing.FillTriangle(canvas, (left, baseline), (peakX, peakY),
                (right, baseline), body);
            PixelDrawing.FillTriangle(canvas, (peakX, peakY), (peakX + 4, peakY + 8),
                (right - 5, baseline), ridge);
            DrawRidges(canvas, peakX, peakY, left, right, baseline, ridge, index);
            left = right - 7;
        }

        PixelDrawing.FillRect(canvas, 0, baseline - 1, 320, 180 - baseline + 1, body);
    }

    /// <summary>
    /// 从峰顶向两侧绘制长短不一的岩脊和台地断线，打破规则三角形的几何感。
    /// </summary>
    private static void DrawRidges(
        PixelCanvas canvas, int peakX, int peakY, int left, int right,
        int baseline, Rgba32 ridge, int index)
    {
        PixelDrawing.Line(canvas, peakX, peakY + 2, left + 8, baseline - 5, ridge);
        PixelDrawing.Line(canvas, peakX + 3, peakY + 8, right - 9, baseline - 8, ridge);
        PixelDrawing.Line(canvas, peakX - 8, peakY + 17, peakX + 5, peakY + 17, ridge);
        PixelDrawing.Line(canvas, peakX + 4, peakY + 29, right - 7, peakY + 29, ridge);
        for (int mark = 0; mark < 5; mark++)
        {
            int x = left + 5 + (mark * 11 + index * 7) % Math.Max(12, right - left - 9);
            int y = peakY + 13 + (mark * 9 + index * 3) % Math.Max(12, baseline - peakY - 17);
            PixelDrawing.Line(canvas, x, y, x + 3 + mark % 3, y, ridge);
        }
    }

    /// <summary>
    /// 以断续双线和卷曲端头画出穿山云雾，让透明背景仍保留远近遮挡关系。
    /// </summary>
    private static void DrawMist(PixelCanvas canvas, int y, int offset, Rgba32 color)
    {
        for (int segment = -1; segment < 7; segment++)
        {
            int x = segment * 56 + offset;
            PixelDrawing.Line(canvas, x, y, x + 34, y, color);
            PixelDrawing.Line(canvas, x + 8, y + 2, x + 44, y + 2, color);
            PixelDrawing.Line(canvas, x + 32, y - 2, x + 39, y - 2, color);
        }
    }

    /// <summary>
    /// 在中景山隙绘制分段瀑布和水雾亮点，为静态背景增加可感知的日常活动。
    /// </summary>
    private static void DrawWaterfall(PixelCanvas canvas, int x, int top)
    {
        Rgba32 water = new(125, 158, 139, 120);
        PixelDrawing.Line(canvas, x, top, x - 2, 128, water);
        PixelDrawing.Line(canvas, x + 3, top + 4, x + 1, 128, water);
        PixelDrawing.Line(canvas, x - 2, 129, x + 7, 129, new Rgba32(151, 171, 145, 78));
        canvas.SetPixel(x - 5, 127, water);
        canvas.SetPixel(x + 8, 131, water);
    }

    /// <summary>
    /// 绘制双层屋檐、鸟居、石阶与朱砂灯火，建立幻想乡神社的题材识别点。
    /// </summary>
    private static void DrawShrine(PixelCanvas canvas, int x, int y)
    {
        Rgba32 roof = new(5, 11, 8, 255);
        Rgba32 wood = new(43, 25, 19, 255);
        PixelDrawing.FillTriangle(canvas, (x, y), (x + 24, y - 10), (x + 51, y), roof);
        PixelDrawing.FillRect(canvas, x - 2, y, 55, 3, roof);
        PixelDrawing.FillTriangle(canvas, (x + 6, y + 8), (x + 25, y + 1),
            (x + 46, y + 8), roof);
        PixelDrawing.FillRect(canvas, x + 4, y + 8, 44, 3, roof);
        PixelDrawing.FillRect(canvas, x + 9, y + 11, 4, 27, wood);
        PixelDrawing.FillRect(canvas, x + 39, y + 11, 4, 27, wood);
        PixelDrawing.FillRect(canvas, x + 13, y + 15, 26, 3, wood);
        PixelDrawing.FillRect(canvas, x + 20, y + 18, 12, 18, roof);
        PixelDrawing.Line(canvas, x + 17, y + 38, x + 35, y + 38,
            UiPixelPalette.JadeDark);
        PixelDrawing.FillRect(canvas, x + 11, y + 25, 2, 2, UiPixelPalette.CinnabarLight);
        PixelDrawing.FillRect(canvas, x + 39, y + 25, 2, 2, UiPixelPalette.CinnabarLight);
    }

    /// <summary>
    /// 以石拱、桥栏和水中倒影连接山路与神社，使右半区具备可游历空间而非单一剪影。
    /// </summary>
    private static void DrawBridge(PixelCanvas canvas, int x, int y)
    {
        Rgba32 stone = new(50, 62, 49, 245);
        PixelDrawing.Line(canvas, x, y, x + 37, y - 4, stone);
        PixelDrawing.Line(canvas, x, y + 3, x + 37, y - 1, stone);
        for (int post = 0; post < 5; post++)
        {
            int px = x + post * 9;
            PixelDrawing.Line(canvas, px, y, px, y - 5 - post % 2, stone);
            canvas.SetPixel(px, y - 6 - post % 2, UiPixelPalette.GoldDark);
        }

        PixelDrawing.Line(canvas, x + 4, y + 6, x + 34, y + 2,
            new Rgba32(74, 83, 65, 102));
    }

    /// <summary>
    /// 以多层不对称针叶簇和露出的树干绘制近景松林，遮住规整山脚并增强纵深。
    /// </summary>
    private static void DrawPineGrove(PixelCanvas canvas, int x, int baseline)
    {
        Rgba32 pine = new(5, 18, 12, 255);
        Rgba32 pineLight = new(20, 42, 27, 240);
        for (int index = 0; index < 6; index++)
        {
            int treeX = x + index * 10;
            int height = 24 + index % 3 * 7;
            PixelDrawing.FillRect(canvas, treeX, baseline - height, 2, height, pine);
            for (int tier = 0; tier < 3; tier++)
            {
                int crownY = baseline - height + 5 + tier * 7;
                PixelDrawing.FillTriangle(canvas, (treeX - 7 - tier, crownY + 7),
                    (treeX + 1, crownY), (treeX + 8 + tier, crownY + 7), pine);
                PixelDrawing.Line(canvas, treeX + 1, crownY + 1,
                    treeX + 6 + tier, crownY + 6, pineLight);
            }
        }
    }

    /// <summary>
    /// 用分节竹竿和成对竹叶填满画面右缘，同时保留月轮周围的呼吸空间。
    /// </summary>
    private static void DrawBambooGrove(PixelCanvas canvas, int x, int baseline)
    {
        Rgba32 bamboo = new(21, 53, 31, 246);
        Rgba32 leaf = new(34, 70, 40, 230);
        for (int index = 0; index < 5; index++)
        {
            int stemX = x + index * 6;
            int lean = index % 2;
            PixelDrawing.Line(canvas, stemX, baseline, stemX + lean, baseline - 43, bamboo);
            for (int joint = 11; joint < 40; joint += 10)
            {
                int jointY = baseline - joint;
                PixelDrawing.Line(canvas, stemX - 1, jointY, stemX + 2, jointY, leaf);
                PixelDrawing.Line(canvas, stemX, jointY, stemX - 7, jointY - 5, leaf);
                PixelDrawing.Line(canvas, stemX + 1, jointY - 3, stemX + 8, jointY - 8, leaf);
            }
        }
    }

    /// <summary>
    /// 在底部铺设横向水纹、桥影与零散月光，使背景收束为可辨识的河岸而非纯色地块。
    /// </summary>
    private static void DrawWater(PixelCanvas canvas, int y)
    {
        Rgba32 water = new(43, 67, 52, 116);
        Rgba32 glint = new(151, 151, 106, 82);
        for (int line = 0; line < 5; line++)
        {
            int lineY = y + line * 4;
            for (int segment = 0; segment < 7; segment++)
            {
                int x = segment * 51 + (line % 2) * 14;
                PixelDrawing.Line(canvas, x, lineY, x + 24 + segment % 3 * 4, lineY, water);
            }
        }

        PixelDrawing.Line(canvas, 245, y + 3, 274, y + 3, glint);
        PixelDrawing.Line(canvas, 251, y + 7, 269, y + 7, glint);
        PixelDrawing.Line(canvas, 256, y + 11, 265, y + 11, glint);
    }
}
