using Godot;
using TouhouWuxiaSurvivor.Visuals.Internal;
using TouhouWuxiaSurvivor.World.Coordinates;
using TouhouWuxiaSurvivor.World.Generation;
using TouhouWuxiaSurvivor.World.Structures;

namespace TouhouWuxiaSurvivor.World.Rendering;

/// <summary>
/// 在结构生成器给出的真实世界坐标上放置原作场景精灵，并随区块流送和原点重定位回收。
/// </summary>
public sealed class InternalStructureRenderer : IChunkRenderer
{
    private readonly Node2D _container;
    private readonly StructureLocator _structures;
    private readonly InternalVisualCatalog _catalog = new();
    private readonly Dictionary<StructurePlacement, Sprite2D> _sprites = [];
    private readonly Dictionary<string, Texture2D> _markerTextures = new(StringComparer.Ordinal);

    public int VisibleStructureCount => _sprites.Count;

    /// <summary>
    /// 绑定实际世界结构容器与共享选址器，使视觉锚点与生成压印使用同一份确定性数据。
    /// </summary>
    public InternalStructureRenderer(Node2D container, StructureLocator structures)
    {
        _container = container;
        _structures = structures;
    }

    /// <summary>
    /// 为锚点落在当前区块内且拥有内部素材的结构创建世界精灵，重复绘制保持幂等。
    /// </summary>
    public void Draw(GeneratedChunk chunk, ChunkCoordinate originChunk)
    {
        long minX = chunk.Coordinate.X * WorldMetrics.ChunkTiles;
        long minY = chunk.Coordinate.Y * WorldMetrics.ChunkTiles;
        long maxX = minX + WorldMetrics.ChunkTiles - 1;
        long maxY = minY + WorldMetrics.ChunkTiles - 1;
        foreach (StructurePlacement placement in _structures.FindInBounds(minX, minY, maxX, maxY))
        {
            if (!_sprites.ContainsKey(placement))
            {
                TryCreateSprite(placement, originChunk);
            }
        }
    }

    /// <summary>
    /// 回收锚点属于离开区块的结构精灵，避免无限行走持续累积场景节点。
    /// </summary>
    public void Erase(ChunkCoordinate absoluteChunk, ChunkCoordinate originChunk)
    {
        foreach (StructurePlacement placement in _sprites.Keys.ToArray())
        {
            var coordinate = new ChunkCoordinate(
                GridMath.FloorDiv(placement.X, WorldMetrics.ChunkTiles),
                GridMath.FloorDiv(placement.Y, WorldMetrics.ChunkTiles));
            if (coordinate != absoluteChunk)
            {
                continue;
            }

            _sprites[placement].QueueFree();
            _sprites.Remove(placement);
        }
    }

    /// <summary>
    /// 回收全部结构精灵，重定位后将由流送器按新本地坐标重新创建。
    /// </summary>
    public void Clear()
    {
        foreach (Sprite2D sprite in _sprites.Values)
        {
            sprite.QueueFree();
        }

        _sprites.Clear();
    }

    /// <summary>
    /// 按内容包和结构中文名加载素材，并把绝对 Tile 锚点换算为当前本地像素位置。
    /// </summary>
    private void TryCreateSprite(StructurePlacement placement, ChunkCoordinate originChunk)
    {
        string name = StructureNames.GetChinese(placement.Id);
        string packId = InternalContentSourceResolver.GetSourceId(placement.Id);
        if (!_catalog.TryGet(packId, InternalVisualCategory.Structure, name, out var definition) ||
            definition.Kind != InternalVisualKind.Scene ||
            !_catalog.TryGetTexture(definition, out Texture2D texture))
        {
            return;
        }

        long localTileX = placement.X - originChunk.X * WorldMetrics.ChunkTiles;
        long localTileY = placement.Y - originChunk.Y * WorldMetrics.ChunkTiles;
        string markerKey = $"{definition.AssetPath}:{(int)placement.Id}:" +
            $"{placement.QuarterTurns}:{placement.Variant & 1}";
        if (!_markerTextures.TryGetValue(markerKey, out Texture2D? markerTexture))
        {
            markerTexture = InternalStructureTextureFactory.CreateMarker(
                texture, placement.Id, placement.QuarterTurns, placement.Variant);
            _markerTextures.Add(markerKey, markerTexture);
        }

        float footprintPixels = (placement.FootprintRadius * 2 + 1) * WorldMetrics.TilePixels;
        float markerScale = footprintPixels / markerTexture.GetWidth();
        var sprite = new Sprite2D
        {
            Name = $"{placement.Id}_{placement.InstanceId:X16}",
            Texture = markerTexture,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            Position = new Vector2(
                (float)(localTileX * WorldMetrics.TilePixels + WorldMetrics.TilePixels / 2),
                (float)(localTileY * WorldMetrics.TilePixels + WorldMetrics.TilePixels / 2)),
            Scale = Vector2.One * markerScale,
            ZIndex = 1,
        };
        _container.AddChild(sprite);
        _sprites.Add(placement, sprite);
    }
}
