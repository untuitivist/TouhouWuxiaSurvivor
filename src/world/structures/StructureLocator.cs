using TouhouWuxiaSurvivor.World.Biomes;
using TouhouWuxiaSurvivor.World.Coordinates;
using TouhouWuxiaSurvivor.World.Generation;
using TouhouWuxiaSurvivor.World.Official;

namespace TouhouWuxiaSurvivor.World.Structures;

/// <summary>
/// 根据世界种子和稀疏网格定位结构锚点，供地形生成与已探索地图共同查询。
/// </summary>
public sealed class StructureLocator
{
    public const int CellSize = 96;
    public const int Radius = 12;
    private readonly ulong _seed;
    private readonly BiomeSelector _biomes;

    /// <summary>
    /// 创建共享世界种子和群系规则的结构选址器。
    /// </summary>
    public StructureLocator(ulong seed, BiomeSelector biomes)
    {
        _seed = seed;
        _biomes = biomes;
    }

    /// <summary>
    /// 返回锚点落在指定绝对 Tile 矩形内的全部结构，并始终包含范围内的出生神社。
    /// </summary>
    public IReadOnlyList<StructurePlacement> FindInBounds(
        long minX,
        long minY,
        long maxX,
        long maxY)
    {
        var placements = new List<StructurePlacement>();
        if (minX <= 0 && maxX >= 0 && minY <= 0 && maxY >= 0)
        {
            placements.Add(new StructurePlacement(StructureId.HakureiShrine, 0, 0));
        }

        long firstCellX = GridMath.FloorDiv(minX, CellSize);
        long firstCellY = GridMath.FloorDiv(minY, CellSize);
        long lastCellX = GridMath.FloorDiv(maxX, CellSize);
        long lastCellY = GridMath.FloorDiv(maxY, CellSize);
        for (long cellY = firstCellY; cellY <= lastCellY; cellY++)
        {
            for (long cellX = firstCellX; cellX <= lastCellX; cellX++)
            {
                StructurePlacement? placement = LocateCell(cellX, cellY);
                if (placement is { } found &&
                    found.X >= minX && found.X <= maxX &&
                    found.Y >= minY && found.Y <= maxY)
                {
                    placements.Add(found);
                }
            }
        }

        return placements;
    }

    /// <summary>
    /// 确定性判断一个结构网格单元是否生成结构，并计算留有边距的随机锚点。
    /// </summary>
    private StructurePlacement? LocateCell(long cellX, long cellY)
    {
        if (DeterministicHash.Range(_seed, cellX, cellY, 100, 0x6600) >= 62)
        {
            return null;
        }

        long anchorX = cellX * CellSize + 16 +
            DeterministicHash.Range(_seed, cellX, cellY, 64, 0x6601);
        long anchorY = cellY * CellSize + 16 +
            DeterministicHash.Range(_seed, cellX, cellY, 64, 0x6602);
        if (anchorX is > -28 and < 28 && anchorY is > -28 and < 28)
        {
            return null;
        }

        return new StructurePlacement(
            GetStructureId(_biomes.Select(anchorX, anchorY), anchorX, anchorY),
            anchorX,
            anchorY);
    }

    /// <summary>
    /// 把锚点群系映射为对应结构；正作地区严格使用各自登记的地标。
    /// </summary>
    private StructureId GetStructureId(BiomeId biome, long x, long y)
    {
        if (OfficialWorldContentCatalog.TryGet(biome, out OfficialWorldContentDefinition definition))
        {
            return definition.Structure;
        }

        return biome switch
        {
            BiomeId.HakureiShrine => StructureId.ShrineCourt,
            BiomeId.HumanVillage => StructureId.HumanVillage,
            BiomeId.MagicForest => StructureId.MagicCircle,
            BiomeId.YoukaiMountain => StructureId.MountainTerrace,
            _ => StructureId.Crossroads,
        };
    }
}
