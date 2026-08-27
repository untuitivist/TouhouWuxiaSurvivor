using TouhouWuxiaSurvivor.Tools.TileGenerator;

namespace TouhouWuxiaSurvivor.Tools.UiAssetGenerator;

/// <summary>
/// 集中定义武侠 UI 的离散像素色阶，保证不同绘制器不会产生互相冲突的近似颜色。
/// </summary>
internal static class UiPixelPalette
{
    public static readonly Rgba32 Transparent = new(0, 0, 0, 0);
    public static readonly Rgba32 Shadow = new(3, 7, 5, 220);
    public static readonly Rgba32 InkBlack = new(6, 11, 8, 255);
    public static readonly Rgba32 Ink = new(10, 18, 13, 255);
    public static readonly Rgba32 PaperDark = new(13, 24, 17, 255);
    public static readonly Rgba32 Paper = new(19, 31, 23, 255);
    public static readonly Rgba32 PaperLight = new(27, 42, 30, 255);
    public static readonly Rgba32 JadeDark = new(37, 58, 42, 255);
    public static readonly Rgba32 Jade = new(73, 98, 72, 255);
    public static readonly Rgba32 JadeLight = new(116, 139, 103, 255);
    public static readonly Rgba32 GoldDark = new(91, 70, 34, 255);
    public static readonly Rgba32 Gold = new(166, 137, 72, 255);
    public static readonly Rgba32 GoldLight = new(220, 188, 111, 255);
    public static readonly Rgba32 CinnabarDark = new(91, 24, 22, 255);
    public static readonly Rgba32 Cinnabar = new(178, 49, 41, 255);
    public static readonly Rgba32 CinnabarLight = new(224, 100, 73, 255);
    public static readonly Rgba32 Ivory = new(235, 226, 195, 255);
    public static readonly Rgba32 Muted = new(79, 89, 76, 255);
}
