using TouhouWuxiaSurvivor.Tools.TileGenerator;

namespace TouhouWuxiaSurvivor.Tools.UiAssetGenerator;

/// <summary>
/// 绘制可九宫格拉伸的漆木、危险、内嵌、预览和 HUD 五类像素边框。
/// </summary>
internal sealed class UiFramePainter
{
    /// <summary>
    /// 生成带上下卷轴杆、左侧朱砂书脊和克制角饰的通用外层面板。
    /// </summary>
    public PixelCanvas PaintLacquerPanel() => PaintOuterFrame(false);

    /// <summary>
    /// 生成结算和危险确认专用的暗红漆框，结构与通用面板保持同一九宫格契约。
    /// </summary>
    public PixelCanvas PaintDangerPanel() => PaintOuterFrame(true);

    /// <summary>
    /// 生成供列表、正文和构筑详情使用的内嵌墨面，避免在外层卷轴内重复闭合描边。
    /// </summary>
    public PixelCanvas PaintInsetPanel()
    {
        var canvas = NewCanvas(24, 24, new Rgba32(11, 15, 17, 248));
        PixelDrawing.FillRect(canvas, 0, 0, 2, 24, UiPixelPalette.Shadow);
        PixelDrawing.Line(canvas, 3, 1, 17, 1, UiPixelPalette.JadeDark);
        PixelDrawing.Line(canvas, 7, 22, 21, 22, UiPixelPalette.GoldDark);
        PixelDrawing.FillRect(canvas, 2, 4, 1, 13, UiPixelPalette.CinnabarDark);
        canvas.SetPixel(3, 2, UiPixelPalette.Gold);
        canvas.SetPixel(21, 21, UiPixelPalette.Jade);
        return canvas;
    }

    /// <summary>
    /// 生成图鉴动态窗的双层金属框，并以如意云头包住四角以强化陈列感。
    /// </summary>
    public PixelCanvas PaintPreviewFrame()
    {
        var canvas = NewCanvas(32, 32, UiPixelPalette.InkBlack);
        BeveledRect(canvas, 0, 0, 32, 32, UiPixelPalette.Shadow);
        BeveledRect(canvas, 1, 1, 30, 30, UiPixelPalette.GoldDark);
        BeveledRect(canvas, 3, 3, 26, 26, UiPixelPalette.Ink);
        PixelDrawing.Line(canvas, 6, 4, 25, 4, UiPixelPalette.GoldLight);
        PixelDrawing.Line(canvas, 5, 27, 26, 27, UiPixelPalette.Jade);
        PixelDrawing.FillRect(canvas, 6, 6, 20, 20, UiPixelPalette.InkBlack);
        DrawRuyiCorner(canvas, 2, 2, 1, 1);
        DrawRuyiCorner(canvas, 29, 2, -1, 1);
        DrawRuyiCorner(canvas, 2, 29, 1, -1);
        DrawRuyiCorner(canvas, 29, 29, -1, -1);
        return canvas;
    }

    /// <summary>
    /// 生成比模态面板更薄的状态栏外框，保留金属转角但减少遮挡面积。
    /// </summary>
    public PixelCanvas PaintHudPanel()
    {
        var canvas = NewCanvas(24, 24, new Rgba32(7, 9, 10, 236));
        BeveledRect(canvas, 0, 0, 24, 24, UiPixelPalette.Shadow);
        BeveledRect(canvas, 1, 1, 22, 22, UiPixelPalette.GoldDark);
        BeveledRect(canvas, 2, 2, 20, 20, UiPixelPalette.PaperLight);
        PixelDrawing.FillRect(canvas, 4, 4, 16, 16, new Rgba32(9, 12, 13, 238));
        DrawCornerKnot(canvas, 2, 2, UiPixelPalette.Gold, UiPixelPalette.Cinnabar);
        DrawCornerKnot(canvas, 21, 2, UiPixelPalette.Gold, UiPixelPalette.Cinnabar);
        DrawCornerKnot(canvas, 2, 21, UiPixelPalette.Gold, UiPixelPalette.Cinnabar);
        DrawCornerKnot(canvas, 21, 21, UiPixelPalette.Gold, UiPixelPalette.Cinnabar);
        return canvas;
    }

    /// <summary>
    /// 生成透明中心的行旅地图包边，以双层墨金线、朱砂定位钉和如意云角围住地图内容。
    /// </summary>
    public PixelCanvas PaintMapFrame()
    {
        var canvas = NewCanvas(32, 32, UiPixelPalette.Transparent);
        PixelDrawing.Line(canvas, 1, 1, 30, 1, UiPixelPalette.GoldDark);
        PixelDrawing.Line(canvas, 1, 30, 30, 30, UiPixelPalette.GoldDark);
        PixelDrawing.Line(canvas, 1, 1, 1, 30, UiPixelPalette.GoldDark);
        PixelDrawing.Line(canvas, 30, 1, 30, 30, UiPixelPalette.GoldDark);
        PixelDrawing.Line(canvas, 4, 4, 27, 4, UiPixelPalette.Jade);
        PixelDrawing.Line(canvas, 4, 27, 27, 27, UiPixelPalette.Jade);
        PixelDrawing.Line(canvas, 4, 4, 4, 27, UiPixelPalette.Jade);
        PixelDrawing.Line(canvas, 27, 4, 27, 27, UiPixelPalette.Jade);
        DrawRuyiCorner(canvas, 2, 2, 1, 1);
        DrawRuyiCorner(canvas, 29, 2, -1, 1);
        DrawRuyiCorner(canvas, 2, 29, 1, -1);
        DrawRuyiCorner(canvas, 29, 29, -1, -1);
        return canvas;
    }

    /// <summary>
    /// 按危险语义切换卷轴书脊与中心色阶，拉伸区保持平坦以免产生重复纹样。
    /// </summary>
    private static PixelCanvas PaintOuterFrame(bool danger)
    {
        Rgba32 rim = danger ? UiPixelPalette.Cinnabar : UiPixelPalette.GoldDark;
        Rgba32 joint = danger ? UiPixelPalette.GoldLight : UiPixelPalette.Cinnabar;
        Rgba32 face = danger ? new Rgba32(34, 14, 17, 252) : new Rgba32(18, 22, 23, 250);
        var canvas = NewCanvas(32, 32, face);
        PixelDrawing.FillRect(canvas, 0, 0, 32, 3, UiPixelPalette.Shadow);
        PixelDrawing.FillRect(canvas, 0, 29, 32, 3, UiPixelPalette.Shadow);
        PixelDrawing.FillRect(canvas, 2, 2, 28, 2, rim);
        PixelDrawing.FillRect(canvas, 2, 28, 28, 2,
            danger ? UiPixelPalette.CinnabarLight : UiPixelPalette.JadeDark);
        PixelDrawing.FillRect(canvas, 1, 5, 3, 22, joint);
        PixelDrawing.FillRect(canvas, 4, 6, 1, 18, UiPixelPalette.GoldDark);
        PixelDrawing.Line(canvas, 8, 4, 24, 4, UiPixelPalette.Gold);
        PixelDrawing.Line(canvas, 9, 27, 23, 27, UiPixelPalette.Jade);
        DrawWoodGrain(canvas, danger ? UiPixelPalette.CinnabarDark : UiPixelPalette.GoldDark);
        DrawCornerKnot(canvas, 4, 3, rim, joint);
        DrawCornerKnot(canvas, 27, 3, rim, joint);
        DrawCornerKnot(canvas, 4, 28, rim, joint);
        DrawCornerKnot(canvas, 27, 28, rim, joint);
        return canvas;
    }

    /// <summary>
    /// 建立已填底色的画布，保证所有九宫格中心像素在完全不透明状态下稳定拉伸。
    /// </summary>
    private static PixelCanvas NewCanvas(int width, int height, Rgba32 fill)
    {
        var canvas = new PixelCanvas(width, height);
        canvas.Fill(fill);
        return canvas;
    }

    /// <summary>
    /// 以削角矩形模拟木框榫接，避免现代圆角卡片的平滑轮廓。
    /// </summary>
    private static void BeveledRect(
        PixelCanvas canvas, int x, int y, int width, int height, Rgba32 color)
    {
        PixelDrawing.FillRect(canvas, x + 1, y, width - 2, height, color);
        PixelDrawing.FillRect(canvas, x, y + 1, width, height - 2, color);
    }

    /// <summary>
    /// 在边缘固定区绘制断续木纹，拉伸区仅保留连续色面以避免比例失真。
    /// </summary>
    private static void DrawWoodGrain(PixelCanvas canvas, Rgba32 color)
    {
        PixelDrawing.Line(canvas, 8, 5, 12, 5, color);
        PixelDrawing.Line(canvas, 19, 5, 23, 5, color);
        PixelDrawing.Line(canvas, 8, 26, 12, 26, color);
        PixelDrawing.Line(canvas, 19, 26, 23, 26, color);
        PixelDrawing.Line(canvas, 5, 8, 5, 12, color);
        PixelDrawing.Line(canvas, 26, 19, 26, 23, color);
    }

    /// <summary>
    /// 在指定角点画出三像素回纹与中心榫钉，形成能在小尺寸辨认的工艺细节。
    /// </summary>
    private static void DrawCornerKnot(
        PixelCanvas canvas, int x, int y, Rgba32 line, Rgba32 pin)
    {
        int sx = x < canvas.Width / 2 ? 1 : -1;
        int sy = y < canvas.Height / 2 ? 1 : -1;
        PixelDrawing.Line(canvas, x, y, x + sx * 4, y, line);
        PixelDrawing.Line(canvas, x, y, x, y + sy * 4, line);
        PixelDrawing.Line(canvas, x + sx * 2, y + sy * 2, x + sx * 4, y + sy * 2, line);
        canvas.SetPixel(x + sx, y + sy, pin);
    }

    /// <summary>
    /// 绘制向内卷曲的如意云头，用方向参数镜像而不复制四套像素坐标。
    /// </summary>
    private static void DrawRuyiCorner(PixelCanvas canvas, int x, int y, int sx, int sy)
    {
        Rgba32 gold = UiPixelPalette.GoldLight;
        PixelDrawing.Line(canvas, x, y, x + sx * 6, y, gold);
        PixelDrawing.Line(canvas, x, y, x, y + sy * 6, gold);
        PixelDrawing.Line(canvas, x + sx * 3, y + sy * 2, x + sx * 6, y + sy * 2, gold);
        PixelDrawing.Line(canvas, x + sx * 2, y + sy * 3, x + sx * 2, y + sy * 6, gold);
        canvas.SetPixel(x + sx * 4, y + sy * 4, UiPixelPalette.CinnabarLight);
    }
}
