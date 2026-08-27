using TouhouWuxiaSurvivor.Tools.TileGenerator;

namespace TouhouWuxiaSurvivor.Tools.UiAssetGenerator;

/// <summary>
/// 绘制紧凑 HUD 进度槽及四类语义填充纹样，保证低高度下仍能辨别资源类型。
/// </summary>
internal sealed class UiProgressPainter
{
    /// <summary>
    /// 生成带上下金属压条和墨色凹槽的通用进度背景。
    /// </summary>
    public PixelCanvas PaintTrack()
    {
        var canvas = NewCanvas();
        PixelDrawing.FillRect(canvas, 0, 1, 16, 6, UiPixelPalette.GoldDark);
        PixelDrawing.FillRect(canvas, 1, 2, 14, 4, UiPixelPalette.InkBlack);
        PixelDrawing.Line(canvas, 2, 3, 13, 3, UiPixelPalette.PaperDark);
        canvas.SetPixel(0, 0, UiPixelPalette.JadeDark);
        canvas.SetPixel(15, 7, UiPixelPalette.JadeDark);
        return canvas;
    }

    /// <summary>
    /// 按玩法语义选择主色、暗部和高光，并绘制不会被误认成纯色占位的织纹填充。
    /// </summary>
    public PixelCanvas PaintFill(UiProgressKind kind)
    {
        (Rgba32 dark, Rgba32 main, Rgba32 light) = kind switch
        {
            UiProgressKind.Health => (UiPixelPalette.CinnabarDark,
                UiPixelPalette.Cinnabar, UiPixelPalette.CinnabarLight),
            UiProgressKind.Experience => (new Rgba32(29, 77, 55, 255),
                new Rgba32(64, 142, 94, 255), new Rgba32(133, 202, 139, 255)),
            UiProgressKind.Pacing => (UiPixelPalette.GoldDark,
                UiPixelPalette.Gold, UiPixelPalette.GoldLight),
            _ => (new Rgba32(35, 74, 75, 255),
                new Rgba32(71, 137, 135, 255), new Rgba32(133, 195, 185, 255)),
        };
        var canvas = NewCanvas();
        PixelDrawing.FillRect(canvas, 0, 1, 16, 6, dark);
        PixelDrawing.FillRect(canvas, 1, 2, 14, 4, main);
        PixelDrawing.Line(canvas, 2, 2, 13, 2, light);
        for (int x = 3; x < 15; x += 4)
        {
            canvas.SetPixel(x, 4, light);
            canvas.SetPixel(x + 1, 5, dark);
        }

        return canvas;
    }

    /// <summary>
    /// 建立固定十六乘八的透明画布，与 Godot StyleBoxTexture 的三像素边距契约对应。
    /// </summary>
    private static PixelCanvas NewCanvas()
    {
        var canvas = new PixelCanvas(16, 8);
        canvas.Fill(UiPixelPalette.Transparent);
        return canvas;
    }
}
