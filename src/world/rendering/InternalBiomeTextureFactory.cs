using Godot;
using TouhouWuxiaSurvivor.World.Generation;

namespace TouhouWuxiaSurvivor.World.Rendering;

/// <summary>
/// 从原作场景的真实颜色与明暗采样生成四张可平铺地砖，避免把完整横版背景重复贴成壁纸。
/// </summary>
public static class InternalBiomeTextureFactory
{
    private const int TileSize = 16;

    /// <summary>
    /// 提取场景平均色、暗部与亮部，以确定性像素纹理生成 2×2 地砖图集。
    /// </summary>
    public static Texture2D CreateAtlas(Texture2D sourceTexture)
    {
        Image source = sourceTexture.GetImage();
        source.Convert(Image.Format.Rgba8);
        Color average = SampleAverage(source).Darkened(0.48f);
        Color shadow = SampleExtreme(source, false).Darkened(0.28f);
        Color accent = SampleExtreme(source, true).Darkened(0.46f);
        Image atlas = Image.CreateEmpty(TileSize * 2, TileSize * 2, false, Image.Format.Rgba8);

        for (int variant = 0; variant < 4; variant++)
        {
            PaintTile(atlas, variant, average, shadow, accent);
        }

        return ImageTexture.CreateFromImage(atlas);
    }

    /// <summary>
    /// 用低对比底色、暗点和少量高光构成单块像素地砖，边缘保持底色以弱化拼接线。
    /// </summary>
    private static void PaintTile(
        Image atlas,
        int variant,
        Color average,
        Color shadow,
        Color accent)
    {
        int offsetX = variant % 2 * TileSize;
        int offsetY = variant / 2 * TileSize;
        for (int y = 0; y < TileSize; y++)
        {
            for (int x = 0; x < TileSize; x++)
            {
                ulong hash = DeterministicHash.At(
                    (ulong)(variant + 1) * 7919UL, x, y, 0x574F524C44415254UL);
                Color color = average;
                if (x is > 0 and < TileSize - 1 && y is > 0 and < TileSize - 1)
                {
                    int choice = (int)(hash % 31UL);
                    color = choice == 0
                        ? accent
                        : choice is 1 or 2
                            ? average.Lerp(shadow, 0.55f)
                            : average.Lerp(accent, (float)((hash >> 8) & 3UL) * 0.025f);
                }

                color.A = 1.0f;
                atlas.SetPixel(offsetX + x, offsetY + y, color);
            }
        }
    }

    /// <summary>
    /// 稀疏遍历场景中可见像素并计算平均色，控制构建开销且保留原作地区主色倾向。
    /// </summary>
    private static Color SampleAverage(Image source)
    {
        Vector3 sum = Vector3.Zero;
        int count = 0;
        for (int y = 0; y < source.GetHeight(); y += 2)
        {
            for (int x = 0; x < source.GetWidth(); x += 2)
            {
                Color color = source.GetPixel(x, y);
                if (color.A < 0.1f)
                {
                    continue;
                }

                sum += new Vector3(color.R, color.G, color.B);
                count++;
            }
        }

        return count == 0
            ? new Color(0.12f, 0.16f, 0.12f)
            : new Color(sum.X / count, sum.Y / count, sum.Z / count);
    }

    /// <summary>
    /// 返回场景采样中的最亮或最暗可见颜色，为地砖提供来自原图的有限层次变化。
    /// </summary>
    private static Color SampleExtreme(Image source, bool brightest)
    {
        Color selected = new(0.12f, 0.16f, 0.12f);
        float selectedLuminance = brightest ? float.MinValue : float.MaxValue;
        for (int y = 0; y < source.GetHeight(); y += 4)
        {
            for (int x = 0; x < source.GetWidth(); x += 4)
            {
                Color color = source.GetPixel(x, y);
                float luminance = color.R * 0.2126f + color.G * 0.7152f + color.B * 0.0722f;
                bool replace = color.A >= 0.1f &&
                    (brightest ? luminance > selectedLuminance : luminance < selectedLuminance);
                if (replace)
                {
                    selected = color;
                    selectedLuminance = luminance;
                }
            }
        }

        return selected;
    }
}
