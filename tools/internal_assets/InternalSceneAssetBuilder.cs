using System.Text.Json;
using Godot;
using static TouhouWuxiaSurvivor.Tools.InternalAssets.InternalAssetBuildContext;
using static TouhouWuxiaSurvivor.Tools.InternalAssets.InternalImageTransformer;

namespace TouhouWuxiaSurvivor.Tools.InternalAssets;

/// <summary>
/// 把异构原作背景或透明贴图规范化为固定尺寸场景预览，不处理角色帧、立绘或二进制资源。
/// </summary>
internal sealed class InternalSceneAssetBuilder
{
    private readonly InternalAssetBuildContext _context;

    /// <summary>
    /// 绑定共享读写上下文，使场景构建结果参与同一次来源登记和路径校验。
    /// </summary>
    internal InternalSceneAssetBuilder(InternalAssetBuildContext context)
    {
        _context = context;
    }

    /// <summary>
    /// 按可选裁切和画布尺寸铺设场景底图，再以最近邻覆盖裁成统一的 128x80 预览。
    /// </summary>
    internal void BuildScenes(JsonElement definitions)
    {
        foreach (JsonElement definition in definitions.EnumerateArray())
        {
            Image image = _context.LoadImage(definition);
            if (definition.TryGetProperty("crop", out JsonElement crop))
            {
                image = image.GetRegion(ReadRect(crop));
            }

            image = CropOpaque(image);
            Color baseColor = ReadColor(definition.GetProperty("baseColor"));
            Vector2I canvasSize = definition.TryGetProperty(
                "canvas", out JsonElement canvas)
                ? ReadPoint(canvas)
                : image.GetSize();
            image = FitWithin(image, canvasSize);
            Image backdrop = Image.CreateEmpty(
                canvasSize.X, canvasSize.Y, false, Image.Format.Rgba8);
            backdrop.Fill(baseColor);
            backdrop.BlendRect(
                image,
                new Rect2I(0, 0, image.GetWidth(), image.GetHeight()),
                (canvasSize - image.GetSize()) / 2);
            _context.Save(
                Fit(backdrop, new Vector2I(128, 80)),
                definition.GetProperty("output"));
        }
    }
}
