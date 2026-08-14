using Godot;
using TouhouWuxiaSurvivor.World.Structures;

namespace TouhouWuxiaSurvivor.World.Rendering;

/// <summary>
/// 从原作结构场景提取色彩并生成透明俯视地标纹样，避免把横版背景截图直接覆盖在世界上。
/// </summary>
public static class InternalStructureTextureFactory
{
    /// <summary>
    /// 使用原图色彩绘制 128×128 透明俯视建筑部件，并以像素瓦纹打破大块纯色屋面。
    /// </summary>
    public static Texture2D CreateMarker(Texture2D sourceTexture, StructureId structure)
        => CreateMarker(sourceTexture, structure, 0, 0);

    /// <summary>
    /// 使用实例朝向和变体绘制语义一致的 128×128 纹样，供测试和非 Sprite 渲染入口直接复用。
    /// </summary>
    public static Texture2D CreateMarker(
        Texture2D sourceTexture,
        StructureId structure,
        int quarterTurns,
        int variant)
    {
        Image source = sourceTexture.GetImage();
        source.Convert(Image.Format.Rgba8);
        Color body = Sample(source, 0.5f).Darkened(0.24f);
        Color shadow = Sample(source, 0.15f).Darkened(0.18f);
        Color accent = Sample(source, 0.85f).Darkened(0.25f);
        Image marker = Image.CreateEmpty(128, 128, false, Image.Format.Rgba8);
        marker.Fill(Colors.Transparent);
        InternalStructureShape shape = InternalStructureShapeResolver.Resolve(structure);
        for (int y = 0; y < 128; y++)
        {
            for (int x = 0; x < 128; x++)
            {
                int layer = InternalStructurePatternRasterizer.GetLayer(
                    shape, x - 64, y - 64, quarterTurns, variant);
                if (layer == 0)
                {
                    continue;
                }

                Color color = layer switch
                {
                    3 => accent,
                    2 => shadow,
                    _ => PatternBody(body, shadow, accent, x, y, variant),
                };
                color.A = layer == 3 ? 0.94f : layer == 2 ? 0.9f : 0.86f;
                marker.SetPixel(x, y, color);
            }
        }

        return ImageTexture.CreateFromImage(marker);
    }

    /// <summary>
    /// 用交错横瓦、竖向接缝和零星亮边生成稳定像素纹理，使大型馆舍仍能读成建筑而非纯色面板。
    /// </summary>
    private static Color PatternBody(
        Color body,
        Color shadow,
        Color accent,
        int x,
        int y,
        int variant)
    {
        int row = (y + variant * 3) & 7;
        int shiftedX = x + (((y >> 3) & 1) * 8) + variant * 5;
        if (row <= 1)
        {
            return body.Lerp(shadow, 0.42f);
        }

        if ((shiftedX & 15) == 0)
        {
            return body.Lerp(shadow, 0.24f);
        }

        return row == 2 ? body.Lerp(accent, 0.14f) : body;
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
