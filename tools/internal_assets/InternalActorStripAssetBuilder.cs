using System.Text.Json;
using Godot;
using static TouhouWuxiaSurvivor.Tools.InternalAssets.InternalAssetBuildContext;
using static TouhouWuxiaSurvivor.Tools.InternalAssets.InternalImageTransformer;

namespace TouhouWuxiaSurvivor.Tools.InternalAssets;

/// <summary>
/// 将规则图集帧或单个完整轮廓规范化成四帧横向动画条，并严格拒绝越界与透明角色帧。
/// </summary>
internal sealed class InternalActorStripAssetBuilder
{
    private static readonly int[] StaticVerticalOffsets = [2, 1, 0, 1];
    private readonly InternalAssetBuildContext _context;

    /// <summary>
    /// 绑定共享读写上下文，使动画条和其他内部资源使用同一来源集合及输出根目录。
    /// </summary>
    internal InternalActorStripAssetBuilder(InternalAssetBuildContext context)
    {
        _context = context;
    }

    /// <summary>
    /// 从规则网格或四个显式矩形裁帧，逐帧居中到 48x48，最终输出 192x48 横向动画条。
    /// </summary>
    internal void BuildGridStrips(JsonElement definitions)
    {
        foreach (JsonElement definition in definitions.EnumerateArray())
        {
            Image atlas = _context.LoadImage(definition);
            bool hasFrameRects = definition.TryGetProperty(
                "frameRects", out JsonElement frameRects);
            if (hasFrameRects && frameRects.GetArrayLength() != 4)
            {
                throw new InvalidDataException(
                    "frameRects must contain exactly four rectangles.");
            }

            int startX = hasFrameRects
                ? 0
                : definition.GetProperty("startX").GetInt32();
            int startY = hasFrameRects
                ? 0
                : definition.GetProperty("startY").GetInt32();
            int frameWidth = hasFrameRects
                ? 0
                : definition.GetProperty("frameWidth").GetInt32();
            int frameHeight = hasFrameRects
                ? 0
                : definition.GetProperty("frameHeight").GetInt32();
            Image strip = CreateTransparent(new Vector2I(192, 48));
            for (int frame = 0; frame < 4; frame++)
            {
                Rect2I region = hasFrameRects
                    ? ReadRect(frameRects[frame])
                    : new Rect2I(
                        startX + frame * frameWidth,
                        startY,
                        frameWidth,
                        frameHeight);
                string output = definition.GetProperty("output").GetString()!;
                RequireContainedRegion(atlas, region, output);
                Image sprite = atlas.GetRegion(region);
                if (!sprite.GetUsedRect().HasArea())
                {
                    throw new InvalidDataException(
                        $"Declared actor frame is transparent for {output}: {region}.");
                }

                PlaceSprite(strip, sprite, frame, frame % 2);
                RequireComposedFrame(strip, frame, output, region);
            }

            _context.Save(strip, definition.GetProperty("output"));
        }
    }

    /// <summary>
    /// 裁取一个完整原作轮廓并制成轻微上下浮动的四帧条，供没有规则动作网格的单位使用。
    /// </summary>
    internal void BuildStaticStrips(JsonElement definitions)
    {
        foreach (JsonElement definition in definitions.EnumerateArray())
        {
            Image sprite = _context.LoadImage(definition);
            if (definition.TryGetProperty("crop", out JsonElement crop))
            {
                sprite = sprite.GetRegion(ReadRect(crop));
            }

            Image strip = CreateTransparent(new Vector2I(192, 48));
            for (int frame = 0; frame < 4; frame++)
            {
                PlaceSprite(
                    strip, sprite, frame, StaticVerticalOffsets[frame]);
            }

            _context.Save(strip, definition.GetProperty("output"));
        }
    }

    /// <summary>
    /// 拒绝越过源图边界或无面积的声明帧，避免透明补边把错误坐标伪装成有效输出。
    /// </summary>
    private static void RequireContainedRegion(
        Image image,
        Rect2I region,
        string output)
    {
        var bounds = new Rect2I(Vector2I.Zero, image.GetSize());
        if (!region.HasArea() || !bounds.Encloses(region))
        {
            throw new InvalidDataException(
                $"Declared frame is outside source image for {output}: " +
                $"{region} / {bounds}.");
        }
    }

    /// <summary>
    /// 确认居中缩放后的目标帧仍有可见像素，及时暴露变换过程中意外丢失的角色轮廓。
    /// </summary>
    private static void RequireComposedFrame(
        Image strip,
        int frame,
        string output,
        Rect2I sourceRegion)
    {
        Image composed = strip.GetRegion(
            new Rect2I(frame * 48, 0, 48, 48));
        if (!composed.GetUsedRect().HasArea())
        {
            throw new InvalidDataException(
                $"Actor frame was lost while composing {output}: " +
                $"{sourceRegion}.");
        }
    }
}
