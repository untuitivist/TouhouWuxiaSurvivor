using TouhouWuxiaSurvivor.Tools.TileGenerator;

namespace TouhouWuxiaSurvivor.Tools.UiAssetGenerator;

/// <summary>
/// 绘制主菜单专用的横向墨迹题签与角色铭牌，避免把设置页按钮直接放进场景首页。
/// </summary>
internal sealed class UiMenuPainter
{
    /// <summary>
    /// 按交互状态生成不规则墨迹题签；普通态只留落笔，悬停与按下才展开朱砂墨面。
    /// </summary>
    public PixelCanvas PaintEntry(UiControlState state)
    {
        var canvas = NewCanvas(48, 24);
        Rgba32 face = state switch
        {
            UiControlState.Hover => new Rgba32(92, 25, 27, 244),
            UiControlState.Pressed => new Rgba32(128, 34, 31, 250),
            UiControlState.Disabled => new Rgba32(16, 19, 20, 92),
            _ => new Rgba32(8, 12, 15, 76),
        };
        Rgba32 line = state switch
        {
            UiControlState.Hover => UiPixelPalette.GoldLight,
            UiControlState.Pressed => UiPixelPalette.CinnabarLight,
            UiControlState.Disabled => UiPixelPalette.Muted,
            _ => new Rgba32(73, 103, 94, 168),
        };
        DrawBrushBody(canvas, face, state is UiControlState.Hover or UiControlState.Pressed);
        PixelDrawing.FillRect(canvas, 3, 6, 2, 12,
            state == UiControlState.Disabled ? UiPixelPalette.Muted : UiPixelPalette.Cinnabar);
        PixelDrawing.Line(canvas, 8, 19, 35, 19, line);
        if (state is UiControlState.Hover or UiControlState.Pressed)
        {
            PixelDrawing.Line(canvas, 8, 4, 25, 4, UiPixelPalette.GoldLight);
            PixelDrawing.Line(canvas, 36, 6, 43, 11, UiPixelPalette.GoldDark);
            canvas.SetPixel(2, 12, UiPixelPalette.GoldLight);
        }

        return canvas;
    }

    /// <summary>生成只含细金角和朱砂定位点的菜单焦点轮廓。</summary>
    public PixelCanvas PaintFocus()
    {
        var canvas = NewCanvas(48, 24);
        PixelDrawing.Line(canvas, 5, 1, 22, 1, UiPixelPalette.GoldLight);
        PixelDrawing.Line(canvas, 26, 22, 42, 22, UiPixelPalette.GoldLight);
        PixelDrawing.Line(canvas, 1, 6, 1, 17, UiPixelPalette.GoldLight);
        PixelDrawing.Line(canvas, 46, 7, 46, 16, UiPixelPalette.GoldLight);
        canvas.SetPixel(3, 3, UiPixelPalette.CinnabarLight);
        canvas.SetPixel(44, 20, UiPixelPalette.CinnabarLight);
        return canvas;
    }

    /// <summary>
    /// 生成无底色的角色落款，仅以朱砂竖笔和两段金青横线维持信息边界。
    /// </summary>
    public PixelCanvas PaintRolePlaque()
    {
        var canvas = NewCanvas(64, 40);
        PixelDrawing.FillRect(canvas, 5, 7, 52, 27, new Rgba32(7, 11, 14, 214));
        PixelDrawing.FillRect(canvas, 9, 4, 39, 3, new Rgba32(7, 11, 14, 174));
        PixelDrawing.FillRect(canvas, 11, 34, 42, 2, new Rgba32(7, 11, 14, 174));
        canvas.SetPixel(58, 11, new Rgba32(7, 11, 14, 160));
        canvas.SetPixel(4, 30, new Rgba32(7, 11, 14, 190));
        PixelDrawing.FillRect(canvas, 3, 4, 2, 30, UiPixelPalette.CinnabarDark);
        PixelDrawing.FillRect(canvas, 6, 5, 1, 18, UiPixelPalette.GoldDark);
        PixelDrawing.Line(canvas, 9, 3, 43, 3, UiPixelPalette.GoldDark);
        PixelDrawing.Line(canvas, 9, 36, 50, 36, UiPixelPalette.Jade);
        PixelDrawing.Line(canvas, 48, 35, 58, 31, new Rgba32(42, 76, 68, 150));
        canvas.SetPixel(59, 30, UiPixelPalette.Cinnabar);
        canvas.SetPixel(2, 35, UiPixelPalette.GoldLight);
        return canvas;
    }

    /// <summary>建立透明画布，保留题签两端自然散开的墨迹边缘。</summary>
    private static PixelCanvas NewCanvas(int width, int height)
    {
        var canvas = new PixelCanvas(width, height);
        canvas.Fill(UiPixelPalette.Transparent);
        return canvas;
    }

    /// <summary>以错位矩形和端部缺口模拟落笔不均的横向墨迹，而不是规则卡片。</summary>
    private static void DrawBrushBody(PixelCanvas canvas, Rgba32 color, bool expanded)
    {
        int length = expanded ? 43 : 31;
        PixelDrawing.FillRect(canvas, 5, 6, length, 12, color);
        PixelDrawing.FillRect(canvas, 8, 4, length - 9, 2, color);
        PixelDrawing.FillRect(canvas, 10, 18, length - 13, 2, color);
        canvas.SetPixel(expanded ? 45 : 35, 8, color);
        canvas.SetPixel(expanded ? 43 : 33, 19, color);
        canvas.SetPixel(4, 10, color);
    }
}
