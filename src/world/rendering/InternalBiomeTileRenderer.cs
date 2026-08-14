using Godot;
using TouhouWuxiaSurvivor.Visuals.Internal;
using TouhouWuxiaSurvivor.World.Biomes;
using TouhouWuxiaSurvivor.World.Coordinates;
using TouhouWuxiaSurvivor.World.Generation;

namespace TouhouWuxiaSurvivor.World.Rendering;

/// <summary>
/// 从原作场景派生低对比像素地砖，并按真实群系逐格铺入无限世界的地区视觉层。
/// </summary>
public sealed class InternalBiomeTileRenderer : IChunkRenderer
{
    private readonly TileMapLayer _layer;
    private readonly Dictionary<BiomeId, Vector3I> _bindings = [];
    private readonly InternalVisualCatalog _catalog = new();

    public bool UsesInternalArt => _bindings.Count > 0;

    /// <summary>
    /// 绑定独立地区图层，并为清单中存在的每个群系建立自己的原作场景图集来源。
    /// </summary>
    public InternalBiomeTileRenderer(TileMapLayer layer)
    {
        _layer = layer;
        _layer.TileSet = CreateTileSet();
    }

    /// <summary>
    /// 保留既有构造签名；群系选择器不再参与绘制，所有语义直接读取已生成区块。
    /// </summary>
    public InternalBiomeTileRenderer(TileMapLayer layer, BiomeSelector biomes) : this(layer)
    {
    }

    /// <summary>
    /// 按每个绝对 Tile 的真实群系选择素材，使用绝对坐标保持跨区块与重定位后的纹理连续。
    /// </summary>
    public void Draw(GeneratedChunk chunk, ChunkCoordinate originChunk)
    {
        ChunkCoordinate localChunk = chunk.Coordinate - originChunk;
        int baseX = checked((int)localChunk.X) * WorldMetrics.ChunkTiles;
        int baseY = checked((int)localChunk.Y) * WorldMetrics.ChunkTiles;
        long worldBaseX = chunk.Coordinate.X * WorldMetrics.ChunkTiles;
        long worldBaseY = chunk.Coordinate.Y * WorldMetrics.ChunkTiles;

        for (int y = 0; y < WorldMetrics.ChunkTiles; y++)
        {
            for (int x = 0; x < WorldMetrics.ChunkTiles; x++)
            {
                long worldX = worldBaseX + x;
                long worldY = worldBaseY + y;
                BiomeId biome = chunk.GetBiome(x, y);
                if (!_bindings.TryGetValue(biome, out Vector3I binding))
                {
                    continue;
                }

                int variant = DeterministicHash.Range(
                    (ulong)biome + 1UL, worldX, worldY, binding.Y * binding.Z, 0x42494F4D45UL);
                var atlas = new Vector2I(variant % binding.Y, variant / binding.Y);
                _layer.SetCell(new Vector2I(baseX + x, baseY + y), binding.X, atlas);
            }
        }
    }

    /// <summary>
    /// 擦除指定区块的地区格，未映射地区本来为空，因此无需区分群系逐项处理。
    /// </summary>
    public void Erase(ChunkCoordinate absoluteChunk, ChunkCoordinate originChunk)
    {
        ChunkCoordinate localChunk = absoluteChunk - originChunk;
        int baseX = checked((int)localChunk.X) * WorldMetrics.ChunkTiles;
        int baseY = checked((int)localChunk.Y) * WorldMetrics.ChunkTiles;
        for (int y = 0; y < WorldMetrics.ChunkTiles; y++)
        {
            for (int x = 0; x < WorldMetrics.ChunkTiles; x++)
            {
                _layer.EraseCell(new Vector2I(baseX + x, baseY + y));
            }
        }
    }

    /// <summary>
    /// 清空地区图层，供世界原点重定位后由流送器重新绘制活动窗口。
    /// </summary>
    public void Clear() => _layer.Clear();

    /// <summary>
    /// 从每张场景图片生成四种可平铺地砖，并记录群系到来源和图集尺寸的映射。
    /// </summary>
    private TileSet CreateTileSet()
    {
        var tileSet = new TileSet { TileSize = Vector2I.One * WorldMetrics.TilePixels };
        int sourceId = 0;
        foreach (BiomeId biome in Enum.GetValues<BiomeId>())
        {
            string name = BiomeNames.GetChinese(biome);
            string packId = InternalContentSourceResolver.GetSourceId(biome);
            if (!_catalog.TryGet(packId, InternalVisualCategory.Biome, name, out var definition) ||
                definition.Kind != InternalVisualKind.Scene ||
                !_catalog.TryGetTexture(definition, out Texture2D texture))
            {
                continue;
            }

            const int columns = 2;
            const int rows = 2;
            var source = new TileSetAtlasSource
            {
                Texture = InternalBiomeTextureFactory.CreateAtlas(texture),
                TextureRegionSize = Vector2I.One * WorldMetrics.TilePixels,
                UseTexturePadding = true
            };
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    source.CreateTile(new Vector2I(x, y));
                }
            }

            tileSet.AddSource(source, sourceId);
            _bindings.Add(biome, new Vector3I(sourceId, columns, rows));
            sourceId++;
        }

        return tileSet;
    }
}
