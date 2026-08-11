using System.Text.Json;
using Godot;
using static TouhouWuxiaSurvivor.Tools.InternalAssets.InternalAssetBuildContext;
using static TouhouWuxiaSurvivor.Tools.InternalAssets.InternalImageTransformer;

namespace TouhouWuxiaSurvivor.Tools.InternalAssets;

/// <summary>
/// 负责单图裁切与多图层立绘合成，并将人物完整轮廓规范化到固定的 80x80 图鉴画布。
/// </summary>
internal sealed class InternalPortraitAssetBuilder
{
    private readonly InternalAssetBuildContext _context;

    /// <summary>
    /// 绑定共享读写上下文，使单图与合成立绘都纳入同一来源哈希和输出错误处理。
    /// </summary>
    internal InternalPortraitAssetBuilder(InternalAssetBuildContext context)
    {
        _context = context;
    }

    /// <summary>
    /// 默认使用完整原作立绘，仅在清单显式给出裁切时截取单帧，再规范化为 80x80 图鉴图。
    /// </summary>
    internal void BuildPortraits(JsonElement definitions)
    {
        foreach (JsonElement definition in definitions.EnumerateArray())
        {
            Image source = _context.LoadImage(definition);
            Rect2I crop = definition.TryGetProperty(
                "crop", out JsonElement cropElement)
                ? ReadRect(cropElement)
                : new Rect2I(
                    0, 0, source.GetWidth(), source.GetHeight());
            SavePortrait(
                source.GetRegion(crop),
                definition.GetProperty("output"));
        }
    }

    /// <summary>
    /// 按清单把半身、表情或多人图层合成到透明画布，并登记每一层来源供最终哈希审计。
    /// </summary>
    internal void BuildCompositePortraits(JsonElement definitions)
    {
        foreach (JsonElement definition in definitions.EnumerateArray())
        {
            Image composite = CreateTransparent(
                ReadPoint(definition.GetProperty("canvas")));
            foreach (JsonElement layer in definition
                .GetProperty("layers").EnumerateArray())
            {
                Image image = _context.LoadImage(layer);
                if (layer.TryGetProperty("crop", out JsonElement crop))
                {
                    image = image.GetRegion(ReadRect(crop));
                }
                if (layer.TryGetProperty(
                    "clearEdgeWhite", out JsonElement clearWhite) &&
                    clearWhite.GetBoolean())
                {
                    image = ClearEdgeConnectedNearWhite(image, 6);
                }

                composite.BlendRect(
                    image,
                    new Rect2I(
                        0, 0, image.GetWidth(), image.GetHeight()),
                    ReadPoint(layer.GetProperty("position")));
            }

            SavePortrait(composite, definition.GetProperty("output"));
        }
    }

    /// <summary>
    /// 去透明外边后以最近邻等比缩入 72x72，再居中写入稳定的 80x80 透明图鉴画布。
    /// </summary>
    private void SavePortrait(Image source, JsonElement output)
    {
        Image portrait = FitWithin(
            CropOpaque(source), new Vector2I(72, 72));
        Image canvas = CreateTransparent(new Vector2I(80, 80));
        canvas.BlendRect(
            portrait,
            new Rect2I(
                0, 0, portrait.GetWidth(), portrait.GetHeight()),
            new Vector2I(
                (80 - portrait.GetWidth()) / 2,
                (80 - portrait.GetHeight()) / 2));
        _context.Save(canvas, output);
    }
}
