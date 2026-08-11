using System.Text.Json;

namespace TouhouWuxiaSurvivor.Tools.InternalAssets;

/// <summary>
/// 根据一份主清单或作品清单中实际存在的分区，把定义分派给场景、角色动画、立绘与二进制构建器。
/// </summary>
internal sealed class InternalAssetManifestCompiler
{
    private readonly InternalAssetBuildContext _context;
    private readonly InternalSceneAssetBuilder _sceneBuilder;
    private readonly InternalActorStripAssetBuilder _actorBuilder;
    private readonly InternalPortraitAssetBuilder _portraitBuilder;

    /// <summary>
    /// 为一次共享构建上下文建立所有专用构建器，确保它们登记到同一个来源审计集合。
    /// </summary>
    internal InternalAssetManifestCompiler(InternalAssetBuildContext context)
    {
        _context = context;
        _sceneBuilder = new InternalSceneAssetBuilder(context);
        _actorBuilder = new InternalActorStripAssetBuilder(context);
        _portraitBuilder = new InternalPortraitAssetBuilder(context);
    }

    /// <summary>
    /// 构建清单内所有可选分区；作品无需声明空数组，也不会因缺少某种资源类型而失败。
    /// </summary>
    internal void Build(JsonElement root)
    {
        if (root.TryGetProperty("scenes", out JsonElement scenes))
        {
            _sceneBuilder.BuildScenes(scenes);
        }
        if (root.TryGetProperty("gridStrips", out JsonElement grids))
        {
            _actorBuilder.BuildGridStrips(grids);
        }
        if (root.TryGetProperty("staticStrips", out JsonElement statics))
        {
            _actorBuilder.BuildStaticStrips(statics);
        }
        if (root.TryGetProperty("portraits", out JsonElement portraits))
        {
            _portraitBuilder.BuildPortraits(portraits);
        }
        if (root.TryGetProperty(
            "compositePortraits", out JsonElement composites))
        {
            _portraitBuilder.BuildCompositePortraits(composites);
        }
        if (root.TryGetProperty("copies", out JsonElement copies))
        {
            BuildCopies(copies);
        }
        if (root.TryGetProperty("audioCopies", out JsonElement audio))
        {
            InternalBinaryAssetBuilder.Build(
                _context.SourceRoot,
                _context.OutputRoot,
                audio,
                _context.UsedSources);
        }
    }

    /// <summary>
    /// 对只需合并独立 Alpha 的完整图集执行无损图像复制，保留弹幕图集的原始像素坐标。
    /// </summary>
    private void BuildCopies(JsonElement definitions)
    {
        foreach (JsonElement definition in definitions.EnumerateArray())
        {
            _context.Save(
                _context.LoadImage(definition),
                definition.GetProperty("output"));
        }
    }
}
