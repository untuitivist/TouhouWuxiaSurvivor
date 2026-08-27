using TouhouWuxiaSurvivor.Tools.TileGenerator;

namespace TouhouWuxiaSurvivor.Tools.UiAssetGenerator;

/// <summary>
/// 绘制按钮、页签、输入框、列表、勾选、滚动条和分隔线所需的全部像素状态。
/// </summary>
internal sealed class UiControlPainter
{
    /// <summary>
    /// 根据交互状态生成低对比墨签按钮，悬停和按下才强化朱砂漆面与旧金收边。
    /// </summary>
    public PixelCanvas PaintButton(UiControlState state)
    {
        Rgba32 face = state switch
        {
            UiControlState.Hover => new Rgba32(67, 25, 27, 248),
            UiControlState.Pressed => new Rgba32(96, 29, 29, 252),
            UiControlState.Disabled => new Rgba32(18, 20, 21, 190),
            _ => new Rgba32(14, 18, 20, 232),
        };
        Rgba32 edge = state switch
        {
            UiControlState.Hover => UiPixelPalette.GoldLight,
            UiControlState.Pressed => UiPixelPalette.CinnabarLight,
            UiControlState.Disabled => UiPixelPalette.Muted,
            _ => UiPixelPalette.Jade,
        };
        var canvas = NewCanvas(24, 20);
        DrawBevel(canvas, face, edge, state == UiControlState.Pressed);
        DrawButtonCorners(canvas, state == UiControlState.Disabled
            ? UiPixelPalette.Muted : UiPixelPalette.GoldDark);
        return canvas;
    }

    /// <summary>
    /// 生成只绘边缘的键盘焦点框，避免焦点状态覆盖悬停或按下底色。
    /// </summary>
    public PixelCanvas PaintButtonFocus()
    {
        var canvas = NewCanvas(24, 20);
        PixelDrawing.Line(canvas, 3, 0, 20, 0, UiPixelPalette.GoldLight);
        PixelDrawing.Line(canvas, 3, 19, 20, 19, UiPixelPalette.GoldLight);
        PixelDrawing.Line(canvas, 0, 3, 0, 16, UiPixelPalette.GoldLight);
        PixelDrawing.Line(canvas, 23, 3, 23, 16, UiPixelPalette.GoldLight);
        DrawButtonCorners(canvas, UiPixelPalette.CinnabarLight);
        return canvas;
    }

    /// <summary>
    /// 生成内凹输入框；聚焦状态用金色下沿和朱砂角点提供清晰但克制的反馈。
    /// </summary>
    public PixelCanvas PaintField(bool focused)
    {
        var canvas = NewCanvas(24, 20);
        PixelDrawing.FillRect(canvas, 1, 2, 22, 16, UiPixelPalette.Shadow);
        PixelDrawing.FillRect(canvas, 2, 3, 20, 14, UiPixelPalette.Ink);
        PixelDrawing.Line(canvas, 4, 3, 19, 3,
            focused ? UiPixelPalette.GoldLight : UiPixelPalette.PaperLight);
        PixelDrawing.Line(canvas, 4, 17, 19, 17,
            focused ? UiPixelPalette.CinnabarLight : UiPixelPalette.Jade);
        PixelDrawing.Line(canvas, 2, 6, 2, 14, UiPixelPalette.JadeDark);
        if (focused)
        {
            canvas.SetPixel(2, 3, UiPixelPalette.CinnabarLight);
            canvas.SetPixel(21, 16, UiPixelPalette.CinnabarLight);
        }

        return canvas;
    }

    /// <summary>
    /// 生成无盒页签，选中态只增加暗红墨面和完整金色下沿表达当前页。
    /// </summary>
    public PixelCanvas PaintTab(UiControlState state)
    {
        var canvas = NewCanvas(24, 16);
        Rgba32 face = state == UiControlState.Pressed
            ? new Rgba32(70, 24, 25, 244)
            : state == UiControlState.Hover
                ? new Rgba32(29, 34, 33, 210)
                : UiPixelPalette.Transparent;
        Rgba32 edge = state == UiControlState.Pressed
            ? UiPixelPalette.GoldLight
            : state == UiControlState.Hover ? UiPixelPalette.Gold : UiPixelPalette.JadeDark;
        PixelDrawing.FillRect(canvas, 2, 3, 20, 11, face);
        PixelDrawing.Line(canvas, 5, 2, 15, 2, edge);
        PixelDrawing.FillRect(canvas, 4, 14, 16, 2, state == UiControlState.Pressed
            ? UiPixelPalette.CinnabarLight : edge);
        canvas.SetPixel(2, 13, UiPixelPalette.CinnabarDark);
        canvas.SetPixel(21, 13, UiPixelPalette.JadeDark);
        return canvas;
    }

    /// <summary>
    /// 生成低层级列表墨面，只保留左侧压暗和上下短线，避免在外框内再次画完整方框。
    /// </summary>
    public PixelCanvas PaintListPanel()
    {
        var canvas = NewCanvas(24, 24);
        PixelDrawing.FillRect(canvas, 0, 0, 24, 24, new Rgba32(8, 12, 14, 244));
        PixelDrawing.FillRect(canvas, 0, 0, 2, 24, UiPixelPalette.Shadow);
        PixelDrawing.Line(canvas, 4, 1, 18, 1, UiPixelPalette.JadeDark);
        PixelDrawing.Line(canvas, 7, 22, 21, 22, UiPixelPalette.GoldDark);
        canvas.SetPixel(2, 2, UiPixelPalette.CinnabarDark);
        return canvas;
    }

    /// <summary>
    /// 生成列表选中签，以左侧朱砂折签、金色收边和暗红纸面同时传达当前条目。
    /// </summary>
    public PixelCanvas PaintListSelection()
    {
        var canvas = NewCanvas(24, 20);
        PixelDrawing.FillRect(canvas, 0, 0, 24, 20, new Rgba32(48, 17, 15, 255));
        PixelDrawing.FillRect(canvas, 0, 0, 3, 20, UiPixelPalette.Cinnabar);
        PixelDrawing.FillTriangle(canvas, (3, 0), (7, 0), (3, 4), UiPixelPalette.GoldLight);
        PixelDrawing.Line(canvas, 4, 1, 20, 1, UiPixelPalette.GoldDark);
        PixelDrawing.Line(canvas, 4, 18, 20, 18, UiPixelPalette.JadeDark);
        return canvas;
    }

    /// <summary>
    /// 生成方形符印式勾选框；启用时在朱砂底上绘制象牙色折笔而不是通用圆点。
    /// </summary>
    public PixelCanvas PaintCheck(bool selected, bool disabled)
    {
        var canvas = NewCanvas(12, 12);
        Rgba32 edge = disabled ? UiPixelPalette.Muted : UiPixelPalette.Gold;
        Rgba32 face = selected
            ? disabled ? new Rgba32(74, 47, 42, 255) : UiPixelPalette.CinnabarDark
            : UiPixelPalette.InkBlack;
        PixelDrawing.FillRect(canvas, 1, 1, 10, 10, edge);
        PixelDrawing.FillRect(canvas, 3, 3, 6, 6, face);
        canvas.SetPixel(0, 0, edge);
        canvas.SetPixel(11, 0, edge);
        canvas.SetPixel(0, 11, edge);
        canvas.SetPixel(11, 11, edge);
        if (selected)
        {
            Rgba32 mark = disabled ? UiPixelPalette.Muted : UiPixelPalette.Ivory;
            PixelDrawing.Line(canvas, 3, 6, 5, 8, mark);
            PixelDrawing.Line(canvas, 5, 8, 9, 3, mark);
        }

        return canvas;
    }

    /// <summary>
    /// 生成带中央朱砂结和对称云钩的横向分隔纹，可在宽页面中安全平铺拉伸。
    /// </summary>
    public PixelCanvas PaintHorizontalSeparator()
    {
        var canvas = NewCanvas(32, 5);
        PixelDrawing.Line(canvas, 0, 2, 31, 2, UiPixelPalette.GoldDark);
        for (int x = 1; x < 32; x += 6)
        {
            canvas.SetPixel(x, 1, UiPixelPalette.Jade);
            canvas.SetPixel(x + 1, 2, UiPixelPalette.GoldLight);
            canvas.SetPixel(x + 2, 3, UiPixelPalette.CinnabarDark);
        }

        return canvas;
    }

    /// <summary>
    /// 生成与横向分隔纹同源的竖向纹样，替代页面中的可拖动或现代感直线分界。
    /// </summary>
    public PixelCanvas PaintVerticalSeparator()
    {
        var canvas = NewCanvas(5, 32);
        PixelDrawing.Line(canvas, 2, 0, 2, 31, UiPixelPalette.GoldDark);
        for (int y = 1; y < 32; y += 6)
        {
            canvas.SetPixel(1, y, UiPixelPalette.Jade);
            canvas.SetPixel(2, y + 1, UiPixelPalette.GoldLight);
            canvas.SetPixel(3, y + 2, UiPixelPalette.CinnabarDark);
        }

        return canvas;
    }

    /// <summary>
    /// 生成滚动槽的深色内嵌面，保证长列表的滚动范围清晰但不过度抢眼。
    /// </summary>
    public PixelCanvas PaintScrollTrack()
    {
        var canvas = NewCanvas(8, 16);
        PixelDrawing.FillRect(canvas, 2, 0, 4, 16, UiPixelPalette.InkBlack);
        PixelDrawing.FillRect(canvas, 3, 1, 2, 14, UiPixelPalette.JadeDark);
        return canvas;
    }

    /// <summary>
    /// 生成竹节形滚动柄，悬停时切换亮金包边以提供明确的指针反馈。
    /// </summary>
    public PixelCanvas PaintScrollGrabber(bool hovered)
    {
        var canvas = NewCanvas(8, 16);
        Rgba32 edge = hovered ? UiPixelPalette.GoldLight : UiPixelPalette.GoldDark;
        PixelDrawing.FillRect(canvas, 1, 1, 6, 14, edge);
        PixelDrawing.FillRect(canvas, 2, 3, 4, 10, hovered
            ? UiPixelPalette.CinnabarDark : UiPixelPalette.JadeDark);
        PixelDrawing.Line(canvas, 2, 5, 5, 5, edge);
        PixelDrawing.Line(canvas, 2, 10, 5, 10, edge);
        return canvas;
    }

    /// <summary>
    /// 建立透明画布，让焦点框、分隔线和削角区域保持真实透明而非黑色方块。
    /// </summary>
    private static PixelCanvas NewCanvas(int width, int height)
    {
        var canvas = new PixelCanvas(width, height);
        canvas.Fill(UiPixelPalette.Transparent);
        return canvas;
    }

    /// <summary>
    /// 绘制按钮主体的外凸或内凹双层边缘，并保留四个削角像素。
    /// </summary>
    private static void DrawBevel(
        PixelCanvas canvas, Rgba32 face, Rgba32 edge, bool pressed)
    {
        PixelDrawing.FillRect(canvas, 2, 3, 21, 15, UiPixelPalette.Shadow);
        PixelDrawing.FillRect(canvas, 2, 2, 20, 15, face);
        PixelDrawing.FillRect(canvas, 1, 6, 2, 9,
            pressed ? UiPixelPalette.Gold : UiPixelPalette.CinnabarDark);
        PixelDrawing.Line(canvas, 5, 2, 16, 2,
            pressed ? UiPixelPalette.CinnabarLight : UiPixelPalette.JadeDark);
        PixelDrawing.Line(canvas, 5, 16, 20, 16, edge);
    }

    /// <summary>
    /// 在按钮削角上追加四枚工艺像素，使宽按钮拉伸后仍保留明确端点。
    /// </summary>
    private static void DrawButtonCorners(PixelCanvas canvas, Rgba32 color)
    {
        canvas.SetPixel(2, 1, color);
        canvas.SetPixel(21, 1, color);
        canvas.SetPixel(2, 18, color);
        canvas.SetPixel(21, 18, color);
    }
}
