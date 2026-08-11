using Godot;
using TouhouWuxiaSurvivor.World.Structures;

namespace TouhouWuxiaSurvivor.World.Rendering;

/// <summary>
/// 从原作结构场景提取色彩并生成透明俯视地标纹样，避免把横版背景截图直接覆盖在世界上。
/// </summary>
public static class InternalStructureTextureFactory
{
    /// <summary>
    /// 使用原图平均色、暗部和亮部绘制 128×128 俯视结构，并按结构类型改变占地图形。
    /// </summary>
    public static Texture2D CreateMarker(Texture2D sourceTexture, StructureId structure)
    {
        Image source = sourceTexture.GetImage();
        source.Convert(Image.Format.Rgba8);
        Color average = Sample(source, 0.5f).Darkened(0.3f);
        Color shadow = Sample(source, 0.15f).Darkened(0.18f);
        Color accent = Sample(source, 0.85f).Darkened(0.25f);
        Image marker = Image.CreateEmpty(128, 128, false, Image.Format.Rgba8);
        marker.Fill(Colors.Transparent);
        InternalStructureShape shape = InternalStructureShapeResolver.Resolve(structure);
        for (int y = 0; y < 128; y++)
        {
            for (int x = 0; x < 128; x++)
            {
                int layer = InternalStructurePatternRasterizer.GetLayer(shape, x - 64, y - 64);
                if (layer == 0)
                {
                    continue;
                }

                Color color = layer == 3 ? accent : layer == 2 ? shadow : average;
                color.A = layer == 1 ? 0.78f : 0.94f;
                marker.SetPixel(x, y, color);
            }
        }

        return ImageTexture.CreateFromImage(marker);
    }

    /// <summary>
    /// 按目标亮度分位附近选取场景颜色，使结构纹样保留原图色相而不携带横版画面构图。
    /// </summary>
    private static Color Sample(Image source, float targetLuminance)
    {
        Color selected = new(0.2f, 0.2f, 0.2f);
        float bestDistance = float.MaxValue;
        for (int y = 0; y < source.GetHeight(); y += 3)
        {
            for (int x = 0; x < source.GetWidth(); x += 3)
            {
                Color color = source.GetPixel(x, y);
                float luminance = color.R * 0.2126f + color.G * 0.7152f + color.B * 0.0722f;
                float distance = Math.Abs(luminance - targetLuminance);
                if (color.A >= 0.1f && distance < bestDistance)
                {
                    selected = color;
                    bestDistance = distance;
                }
            }
        }

        return selected;
    }
}
