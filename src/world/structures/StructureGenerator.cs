using TouhouWuxiaSurvivor.World.Coordinates;
using TouhouWuxiaSurvivor.World.Generation;
using TouhouWuxiaSurvivor.World.StructureTemplates;
using TouhouWuxiaSurvivor.World.Tiles;

namespace TouhouWuxiaSurvivor.World.Structures;

/// <summary>
/// 使用定义驱动的分层模板压印结构，并按照每个实例的真实 footprint 跨区块连续生成。
/// </summary>
public sealed class StructureGenerator
{
    private readonly StructureLocator _locator;

    /// <summary>
    /// 创建使用共享结构定位器的生成器，使地表与地图读取完全相同的实例集合。
    /// </summary>
    public StructureGenerator(StructureLocator locator) => _locator = locator;

    /// <summary>
    /// 扩大锚点查询到目录最大占地半径，并只把与当前区块相交的模板格写入地表。
    /// </summary>
    public void Apply(GeneratedChunk chunk)
    {
        long minX = chunk.Coordinate.X * WorldMetrics.ChunkTiles;
        long minY = chunk.Coordinate.Y * WorldMetrics.ChunkTiles;
        long maxX = minX + WorldMetrics.ChunkTiles - 1;
        long maxY = minY + WorldMetrics.ChunkTiles - 1;
        int margin = StructureCatalog.MaximumFootprintRadius;
        foreach (StructurePlacement placement in _locator.FindInBounds(
            minX - margin, minY - margin, maxX + margin, maxY + margin))
        {
            Stamp(chunk, placement);
        }
    }

    /// <summary>
    /// 遍历实例真实占地并把模板角色映射为 Tile；旋转与变体由 placement 稳定提供。
    /// </summary>
    private static void Stamp(GeneratedChunk chunk, StructurePlacement placement)
    {
        StructureDefinition definition = StructureCatalog.GetRequired(placement.Id);
        int radius = placement.FootprintRadius;
        for (int dy = -radius; dy <= radius; dy++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                StructureTileRole role = StructureTemplateSampler.Sample(
                    definition.Template, dx, dy, radius,
                    placement.QuarterTurns, placement.Variant);
                if (StructureTilePalette.TryResolve(definition, role, out TileId tile))
                {
                    chunk.TrySetAbsolute(placement.X + dx, placement.Y + dy, tile);
                }
            }
        }
    }
}
