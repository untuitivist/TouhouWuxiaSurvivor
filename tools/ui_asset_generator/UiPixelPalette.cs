using TouhouWuxiaSurvivor.Tools.TileGenerator;

namespace TouhouWuxiaSurvivor.Tools.UiAssetGenerator;

/// <summary>
/// 集中定义武侠 UI 的离散像素色阶，保证不同绘制器不会产生互相冲突的近似颜色。
/// </summary>
internal static class UiPixelPalette
{
    public static readonly Rgba32 Transparent = new(0, 0, 0, 0);
    public static readonly Rgba32 Shadow = new(3, 4, 6, 224);
    public static readonly Rgba32 InkBlack = new(8, 10, 12, 255);
    public static readonly Rgba32 Ink = new(15, 18, 19, 255);
    public static readonly Rgba32 PaperDark = new(23, 25, 24, 255);
    public static readonly Rgba32 Paper = new(34, 36, 33, 255);
    public static readonly Rgba32 PaperLight = new(51, 53, 47, 255);
    public static readonly Rgba32 JadeDark = new(24, 48, 46, 255);
    public static readonly Rgba32 Jade = new(52, 91, 82, 255);
    public static readonly Rgba32 JadeLight = new(101, 136, 116, 255);
    public static readonly Rgba32 GoldDark = new(83, 63, 32, 255);
    public static readonly Rgba32 Gold = new(157, 126, 61, 255);
    public static readonly Rgba32 GoldLight = new(218, 181, 96, 255);
    public static readonly Rgba32 CinnabarDark = new(76, 20, 23, 255);
    public static readonly Rgba32 Cinnabar = new(161, 42, 42, 255);
    public static readonly Rgba32 CinnabarLight = new(220, 82, 61, 255);
    public static readonly Rgba32 Ivory = new(229, 220, 188, 255);
    public static readonly Rgba32 Muted = new(82, 87, 82, 255);
}
