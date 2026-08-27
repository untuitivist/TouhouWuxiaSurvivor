using TouhouWuxiaSurvivor.Tools.TileGenerator;

namespace TouhouWuxiaSurvivor.Tools.UiAssetGenerator;

/// <summary>
/// 绘制主菜单的天空、月轮、不规则山岭与河湾，负责建立幻想乡夜景的空间纵深。
/// </summary>
internal sealed class UiBackdropLandscapePainter
{
    /// <summary>依次铺设天空、月亮、远山、中景和水面，给建筑层留下右侧视觉焦点。</summary>
    public void Draw(PixelCanvas canvas)
    {
        DrawSky(canvas);
        DrawMoon(canvas);
        DrawFarMountains(canvas);
        DrawMiddleMountains(canvas);
        DrawRiver(canvas);
    }

    /// <summary>使用深靛到灰青的原生分段渐变和稀疏抖色表现有空气感的夜空。</summary>
    private static void DrawSky(PixelCanvas canvas)
    {
        UiBackdropPixelDrawing.FillVerticalGradient(canvas, 0, 276,
            new Rgba32(10, 13, 21), new Rgba32(35, 44, 43));
        UiBackdropPixelDrawing.Dither(canvas, 0, 128, 640, 120, 11,
            new Rgba32(39, 51, 49));
        PixelDrawing.FillRect(canvas, 0, 242, 640, 34, new Rgba32(29, 41, 38));
    }

    /// <summary>用暖灰光晕、象牙月面和错位阴影绘制有层次但保持硬边的月轮。</summary>
    private static void DrawMoon(PixelCanvas canvas)
    {
        PixelDrawing.FillEllipse(canvas, 507, 78, 63, 63, new Rgba32(62, 61, 49));
        PixelDrawing.FillEllipse(canvas, 507, 75, 57, 57, new Rgba32(126, 118, 83));
        PixelDrawing.FillEllipse(canvas, 510, 70, 49, 49, new Rgba32(224, 211, 158));
        PixelDrawing.FillEllipse(canvas, 490, 67, 13, 20, new Rgba32(184, 171, 123));
        PixelDrawing.FillEllipse(canvas, 533, 51, 7, 5, new Rgba32(201, 187, 137));
        PixelDrawing.FillEllipse(canvas, 530, 93, 10, 7, new Rgba32(211, 197, 145));
        PixelDrawing.Line(canvas, 483, 37, 525, 29, new Rgba32(239, 226, 174));
    }

    /// <summary>以长折线多边形形成远山轮廓，并用细脊线避免重复三角峰的机械感。</summary>
    private static void DrawFarMountains(PixelCanvas canvas)
    {
        (int X, int Y)[] silhouette =
        [
            (-18, 251), (24, 241), (62, 218), (88, 223), (126, 196),
            (155, 201), (190, 172), (215, 179), (249, 146), (278, 163),
            (309, 186), (350, 174), (382, 181), (414, 150), (442, 160),
            (470, 188), (511, 168), (546, 177), (580, 154), (610, 166),
            (660, 191), (660, 278), (-18, 278),
        ];
        PixelDrawing.FillPolygon(canvas, silhouette, new Rgba32(47, 66, 61));
        DrawRidge(canvas, [(82, 205), (105, 211), (139, 181), (161, 190)],
            new Rgba32(76, 91, 78));
        DrawRidge(canvas, [(188, 159), (217, 174), (249, 148), (280, 177)],
            new Rgba32(72, 91, 80));
        DrawRidge(canvas, [(433, 153), (456, 169), (489, 144), (521, 163)],
            new Rgba32(78, 96, 82));
        for (int y = 198; y < 252; y += 13)
        {
            PixelDrawing.Line(canvas, 75 + y % 19, y, 153 + y % 27, y,
                new Rgba32(58, 78, 70));
            PixelDrawing.Line(canvas, 278 + y % 31, y + 4, 373 + y % 17, y + 4,
                new Rgba32(59, 79, 72));
        }
    }

    /// <summary>在远山前叠加低矮林坡与山谷缺口，使地平线具有前后遮挡而非平直色带。</summary>
    private static void DrawMiddleMountains(PixelCanvas canvas)
    {
        (int X, int Y)[] slope =
        [
            (-20, 300), (25, 279), (62, 285), (99, 257), (132, 266),
            (170, 240), (202, 251), (242, 223), (278, 243), (316, 229),
            (350, 247), (390, 231), (423, 245), (462, 225), (500, 239),
            (538, 220), (577, 237), (615, 224), (660, 218),
            (660, 316), (-20, 316),
        ];
        PixelDrawing.FillPolygon(canvas, slope, new Rgba32(18, 34, 31));
        (int X, int Y)[] shadow =
        [
            (-12, 310), (48, 279), (103, 294), (162, 260), (224, 286),
            (284, 253), (349, 281), (415, 249), (480, 275), (544, 245),
            (606, 266), (660, 249), (660, 327), (-12, 327),
        ];
        PixelDrawing.FillPolygon(canvas, shadow, new Rgba32(11, 25, 23));
        UiBackdropPixelDrawing.Dither(canvas, 0, 249, 640, 62, 9,
            new Rgba32(25, 46, 40));
    }

    /// <summary>用冷灰青水面、岸线和横向反射收束前景，并保留右下角色信息的可读暗区。</summary>
    private static void DrawRiver(PixelCanvas canvas)
    {
        UiBackdropPixelDrawing.FillVerticalGradient(canvas, 300, 360,
            new Rgba32(15, 31, 30), new Rgba32(7, 18, 21));
        PixelDrawing.FillPolygon(canvas,
            [(0, 300), (112, 293), (237, 306), (346, 287), (465, 301),
             (553, 286), (640, 294), (640, 311), (0, 311)],
            new Rgba32(8, 24, 22));
        for (int row = 0; row < 8; row++)
        {
            int y = 310 + row * 6;
            PixelDrawing.Line(canvas, 16 + row * 23, y, 92 + row * 29, y,
                new Rgba32(33, 62, 56));
            PixelDrawing.Line(canvas, 270 + row * 19, y + 2, 338 + row * 27, y + 2,
                new Rgba32(62, 77, 62));
        }
    }

    /// <summary>逐段连接山脊控制点，使高光沿自然折线延伸而不会切成完整几何面。</summary>
    private static void DrawRidge(
        PixelCanvas canvas, IReadOnlyList<(int X, int Y)> points, Rgba32 color)
    {
        for (int index = 0; index + 1 < points.Count; index++)
        {
            PixelDrawing.Line(canvas, points[index].X, points[index].Y,
                points[index + 1].X, points[index + 1].Y, color);
        }
    }
}
