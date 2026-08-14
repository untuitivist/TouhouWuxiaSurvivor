using TouhouWuxiaSurvivor.World.Biomes;
using TouhouWuxiaSurvivor.World.Generation;
using TouhouWuxiaSurvivor.World.Official;
using TouhouWuxiaSurvivor.World.Regions;

namespace TouhouWuxiaSurvivor.World.Structures;

/// <summary>
/// 在每个正作 Voronoi 宏域中分别为外围、内部和核心寻找地标锚点，使三结构保持空间关联。
/// </summary>
public sealed class OfficialStructureSiteLocator
{
    private static readonly (int X, int Y)[] Directions =
    [
        (32, 0), (30, 12), (23, 23), (12, 30), (0, 32), (-12, 30), (-23, 23),
        (-30, 12), (-32, 0), (-30, -12), (-23, -23), (-12, -30), (0, -32),
        (12, -30), (23, -23), (30, -12),
    ];
    private readonly ulong _seed;
    private readonly BiomeSelector _biomes;

    /// <summary>
    /// 创建与群系规划共享世界种子的站点定位器。
    /// </summary>
    public OfficialStructureSiteLocator(ulong seed, BiomeSelector biomes) =>
        (_seed, _biomes) = (seed, biomes);

    /// <summary>
    /// 扫描覆盖查询范围的宏域单元，并为每个正作站点输出最多三个层级合法候选。
    /// </summary>
    public IReadOnlyList<StructurePlacement> FindCandidates(
        long minX,
        long minY,
        long maxX,
        long maxY)
    {
        var placements = new List<StructurePlacement>();
        int size = WorldRegionPlanner.CellSize;
        long firstX = Coordinates.GridMath.FloorDiv(minX, size) - 1;
        long firstY = Coordinates.GridMath.FloorDiv(minY, size) - 1;
        long lastX = Coordinates.GridMath.FloorDiv(maxX, size) + 1;
        long lastY = Coordinates.GridMath.FloorDiv(maxY, size) + 1;
        for (long cellY = firstY; cellY <= lastY; cellY++)
        {
            for (long cellX = firstX; cellX <= lastX; cellX++)
            {
                WorldRegionSample sample = _biomes.SampleRegion(
                    cellX * size + size / 2, cellY * size + size / 2);
                if (!sample.IsOfficial || sample.Site.CellX != cellX || sample.Site.CellY != cellY)
                {
                    continue;
                }

                foreach (OfficialWorldContentDefinition source in
                    OfficialWorldContentCatalog.GetByPack(sample.PackId))
                {
                    StructureDefinition definition = StructureCatalog.GetRequired(source.Structure);
                    if (TryCreate(definition, source.RegionIndex, sample.Site, out var placement))
                    {
                        placements.Add(placement);
                    }
                }
            }
        }

        return placements;
    }

    /// <summary>
    /// 按 profile 概率决定站点实例，再从该层多组半径和方向中选择首个合法群系坐标。
    /// </summary>
    private bool TryCreate(
        StructureDefinition definition,
        int regionIndex,
        WorldRegionSite site,
        out StructurePlacement placement)
    {
        StructurePlacementProfile profile = definition.Placement;
        if (DeterministicHash.Unit(_seed, site.CellX, site.CellY, profile.Salt + 20) >=
            profile.Chance)
        {
            placement = default;
            return false;
        }

        int[] radii = regionIndex switch
        {
            2 => [0, 16, 28, 40],
            1 => [72, 84, 96, 106],
            _ => [128, 144, 160, 176],
        };
        int start = DeterministicHash.Range(_seed, site.CellX, site.CellY,
            Directions.Length, profile.Salt + 21);
        foreach (int radius in radii)
        {
            for (int index = 0; index < Directions.Length; index++)
            {
                (int unitX, int unitY) = Directions[(start + index) % Directions.Length];
                long x = site.CenterX + (long)Math.Round(unitX * radius / 32.0);
                long y = site.CenterY + (long)Math.Round(unitY * radius / 32.0);
                WorldRegionSample sample = _biomes.SampleRegion(x, y);
                if (sample.Site.CellX != site.CellX || sample.Site.CellY != site.CellY ||
                    !definition.Allows(_biomes.Select(x, y)) || IsSpawnProtected(x, y, profile))
                {
                    continue;
                }

                ulong instanceId = DeterministicHash.At(_seed, site.CellX, site.CellY,
                    profile.Salt + 22);
                placement = new StructurePlacement(
                    definition.Id, x, y, instanceId, definition.DefinitionId,
                    DeterministicHash.Range(_seed, x, y, 4, profile.Salt + 23),
                    DeterministicHash.Range(_seed, x, y, 4, profile.Salt + 24),
                    profile.FootprintRadius);
                return true;
            }
        }

        placement = default;
        return false;
    }

    /// <summary>
    /// 拒绝覆盖固定出生神社的候选，平方比较避免额外开方。
    /// </summary>
    private static bool IsSpawnProtected(long x, long y, StructurePlacementProfile profile) =>
        (double)x * x + (double)y * y <
        (double)profile.SpawnProtectionRadius * profile.SpawnProtectionRadius;
}
