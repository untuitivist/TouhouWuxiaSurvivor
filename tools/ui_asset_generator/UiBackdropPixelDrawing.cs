using TouhouWuxiaSurvivor.Tools.TileGenerator;

namespace TouhouWuxiaSurvivor.Tools.UiAssetGenerator;

/// <summary>
/// 提供 640×360 原生背景所需的分段渐变与规则抖色，避免低分辨率放大造成粗糙块面。
/// </summary>
internal static class UiBackdropPixelDrawing
{
    /// <summary>在指定纵向范围内逐行插值不透明色，形成稳定的像素天空或水面。</summary>
    public static void FillVerticalGradient(
        PixelCanvas canvas, int top, int bottom, Rgba32 start, Rgba32 end)
    {
        int height = Math.Max(1, bottom - top);
        for (int y = top; y < bottom; y++)
        {
            int step = y - top;
            var color = new Rgba32(
                Lerp(start.R, end.R, step, height),
                Lerp(start.G, end.G, step, height),
                Lerp(start.B, end.B, step, height),
                Lerp(start.A, end.A, step, height));
            PixelDrawing.FillRect(canvas, 0, y, canvas.Width, 1, color);
        }
    }

    /// <summary>按固定棋盘相位稀疏覆盖像素，给大色块增加手工抖色而不引入随机噪点。</summary>
    public static void Dither(
        PixelCanvas canvas, int left, int top, int width, int height,
        int spacing, Rgba32 color)
    {
        for (int y = top; y < top + height; y++)
        {
            for (int x = left; x < left + width; x++)
            {
                if ((x + y * 3) % spacing == 0)
                {
                    canvas.SetPixel(x, y, color);
                }
            }
        }
    }

    /// <summary>使用整数比例插值单个颜色通道，保证跨平台生成结果完全一致。</summary>
    private static byte Lerp(byte start, byte end, int step, int length) =>
        (byte)(start + (end - start) * step / length);
}
