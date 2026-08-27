using TouhouWuxiaSurvivor.Tools.TileGenerator;

namespace TouhouWuxiaSurvivor.Tools.UiAssetGenerator;

/// <summary>
/// 绘制选项箭头与滑杆抓手等固定尺寸控件图标，避免依赖现代默认主题图形。
/// </summary>
internal sealed class UiIconPainter
{
    /// <summary>
    /// 生成由三层金色像素构成的向下箭头，在十二像素尺寸下保持完全对称。
    /// </summary>
    public PixelCanvas PaintOptionArrow()
    {
        var canvas = NewCanvas(12, 12);
        PixelDrawing.Line(canvas, 2, 4, 6, 8, UiPixelPalette.GoldDark);
        PixelDrawing.Line(canvas, 6, 8, 10, 4, UiPixelPalette.GoldDark);
        PixelDrawing.Line(canvas, 3, 4, 6, 7, UiPixelPalette.GoldLight);
        PixelDrawing.Line(canvas, 6, 7, 9, 4, UiPixelPalette.GoldLight);
        canvas.SetPixel(6, 9, UiPixelPalette.Cinnabar);
        return canvas;
    }

    /// <summary>
    /// 生成横向滑杆的结绳式抓手，悬停时用亮金与朱砂中心提升交互反馈。
    /// </summary>
    public PixelCanvas PaintSliderGrabber(bool hovered)
    {
        var canvas = NewCanvas(12, 14);
        Rgba32 edge = hovered ? UiPixelPalette.GoldLight : UiPixelPalette.Gold;
        Rgba32 center = hovered ? UiPixelPalette.Cinnabar : UiPixelPalette.JadeDark;
        PixelDrawing.FillRect(canvas, 3, 0, 6, 14, edge);
        PixelDrawing.FillRect(canvas, 1, 3, 10, 8, edge);
        PixelDrawing.FillRect(canvas, 3, 3, 6, 8, UiPixelPalette.InkBlack);
        PixelDrawing.FillRect(canvas, 4, 4, 4, 6, center);
        canvas.SetPixel(2, 2, UiPixelPalette.GoldDark);
        canvas.SetPixel(9, 2, UiPixelPalette.GoldDark);
        canvas.SetPixel(2, 11, UiPixelPalette.GoldDark);
        canvas.SetPixel(9, 11, UiPixelPalette.GoldDark);
        return canvas;
    }

    /// <summary>
    /// 建立透明图标画布，使不规则轮廓在最近邻缩放下不会出现方形底色。
    /// </summary>
    private static PixelCanvas NewCanvas(int width, int height)
    {
        var canvas = new PixelCanvas(width, height);
        canvas.Fill(UiPixelPalette.Transparent);
        return canvas;
    }
}
