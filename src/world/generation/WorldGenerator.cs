using TouhouWuxiaSurvivor.Content;
using TouhouWuxiaSurvivor.World.Biomes;
using TouhouWuxiaSurvivor.World.Coordinates;
using TouhouWuxiaSurvivor.World.Structures;
using TouhouWuxiaSurvivor.World.Tiles;

namespace TouhouWuxiaSurvivor.World.Generation;

/// <summary>
/// 协调群系、地表变体和结构三阶段生成，按绝对区块坐标产出确定性结果。
/// </summary>
public sealed class WorldGenerator
{
    private readonly BiomeTilePalette _palette;
    private readonly StructureGenerator _structures;

    /// <summary>
    /// 创建共享同一世界种子的纯本体群系、地表和结构生成组件。
    /// </summary>
    public WorldGenerator(ulong seed) : this(seed, ContentPackSelection.BaseOnly)
    {
    }

    /// <summary>
    /// 创建共享同一世界种子和不可变内容快照的群系、地表和结构生成组件。
    /// </summary>
    public WorldGenerator(ulong seed, ContentPackSelection content)
    {
        Seed = seed;
        Content = content;
        Biomes = new BiomeSelector(seed, content);
        StructureLocations = new StructureLocator(seed, Biomes);
        _palette = new BiomeTilePalette(seed);
        _structures = new StructureGenerator(StructureLocations);
    }

    public ulong Seed { get; }

    public ContentPackSelection Content { get; }

    public BiomeSelector Biomes { get; }

    public StructureLocator StructureLocations { get; }

    /// <summary>
    /// 生成一个完整区块：先逐 Tile 填充群系地表，再叠加可能跨越区块边界的结构。
    /// </summary>
    public GeneratedChunk Generate(ChunkCoordinate coordinate)
    {
        var chunk = new GeneratedChunk(coordinate);
        long originX = coordinate.X * WorldMetrics.ChunkTiles;
        long originY = coordinate.Y * WorldMetrics.ChunkTiles;

        for (int y = 0; y < WorldMetrics.ChunkTiles; y++)
        {
            for (int x = 0; x < WorldMetrics.ChunkTiles; x++)
            {
                long worldX = originX + x;
                long worldY = originY + y;
                BiomeId biome = Biomes.Select(worldX, worldY);
                chunk.SetBiome(x, y, biome);
                chunk.Set(x, y, _palette.Pick(biome, worldX, worldY));
            }
        }

        _structures.Apply(chunk);
        return chunk;
    }
}
