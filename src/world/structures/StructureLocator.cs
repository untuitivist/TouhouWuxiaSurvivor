using TouhouWuxiaSurvivor.World.Biomes;
using TouhouWuxiaSurvivor.World.Coordinates;
using TouhouWuxiaSurvivor.World.Generation;

namespace TouhouWuxiaSurvivor.World.Structures;

/// <summary>
/// 合并本体 spacing 网格和正作宏域地标，再以稳定优先级执行跨类型硬间距裁决。
/// </summary>
public sealed class StructureLocator
{
    public const int CellSize = 96;
    public static int Radius => StructureCatalog.MaximumFootprintRadius;
    public static int MaximumFootprintRadius => StructureCatalog.MaximumFootprintRadius;
    private readonly ulong _seed;
    private readonly BiomeSelector _biomes;
    private readonly OfficialStructureSiteLocator _officialSites;

    /// <summary>
    /// 创建共享世界种子与宏域规则的结构定位器，使三层地区与三处正作地标保持关联。
    /// </summary>
    public StructureLocator(ulong seed, BiomeSelector biomes)
    {
        _seed = seed;
        _biomes = biomes;
        _officialSites = new OfficialStructureSiteLocator(seed, biomes);
    }

    /// <summary>
    /// 查询指定绝对 Tile 矩形内锚点；内部扩大冲突边界后裁决，因此窗口与加载顺序不影响结果。
    /// </summary>
    public IReadOnlyList<StructurePlacement> FindInBounds(
        long minX,
        long minY,
        long maxX,
        long maxY)
    {
        int margin = StructurePlacementConflictResolver.QueryMargin;
        var candidates = new List<StructurePlacement>();
        candidates.AddRange(FindBaseCandidates(
            minX - margin, minY - margin, maxX + margin, maxY + margin));
        candidates.AddRange(_officialSites.FindCandidates(
            minX - margin, minY - margin, maxX + margin, maxY + margin));
        IReadOnlyList<StructurePlacement> resolved =
            StructurePlacementConflictResolver.Resolve(candidates);
        var placements = resolved.Where(item => item.X >= minX && item.X <= maxX &&
            item.Y >= minY && item.Y <= maxY).ToList();
        if (minX <= 0 && maxX >= 0 && minY <= 0 && maxY >= 0)
        {
            placements.Add(new StructurePlacement(StructureId.HakureiShrine, 0, 0));
        }

        return placements.OrderBy(item => item.Y).ThenBy(item => item.X)
            .ThenBy(item => item.Id).ToArray();
    }

    /// <summary>
    /// 返回中心圆形范围内的结构，矩形预筛选后再执行精确距离判断。
    /// </summary>
    public IReadOnlyList<StructurePlacement> FindNear(long x, long y, int radius) =>
        FindInBounds(x - radius, y - radius, x + radius, y + radius)
            .Where(item => DistanceSquared(item.X, item.Y, x, y) <= (double)radius * radius)
            .ToArray();

    /// <summary>
    /// 枚举每个本体定义自己的网格单元；正作结构由宏域站点定位器单独处理。
    /// </summary>
    private IReadOnlyList<StructurePlacement> FindBaseCandidates(
        long minX,
        long minY,
        long maxX,
        long maxY)
    {
        var candidates = new List<StructurePlacement>();
        foreach (StructureDefinition definition in StructureCatalog.All.Where(item =>
            !item.IsSpawnStructure && string.IsNullOrEmpty(item.SourcePackId)))
        {
            StructurePlacementProfile profile = definition.Placement;
            long firstX = GridMath.FloorDiv(minX, profile.Spacing);
            long firstY = GridMath.FloorDiv(minY, profile.Spacing);
            long lastX = GridMath.FloorDiv(maxX, profile.Spacing);
            long lastY = GridMath.FloorDiv(maxY, profile.Spacing);
            for (long cellY = firstY; cellY <= lastY; cellY++)
            {
                for (long cellX = firstX; cellX <= lastX; cellX++)
                {
                    if (TryCreateBase(definition, cellX, cellY, out var placement))
                    {
                        candidates.Add(placement);
                    }
                }
            }
        }

        return candidates;
    }

    /// <summary>
    /// 在单个定义网格中计算概率、随机偏移、出生保护和合法群系，并生成完整稳定实例。
    /// </summary>
    private bool TryCreateBase(
        StructureDefinition definition,
        long cellX,
        long cellY,
        out StructurePlacement placement)
    {
        StructurePlacementProfile profile = definition.Placement;
        int spread = profile.Spacing - profile.Separation;
        long x = cellX * profile.Spacing +
            DeterministicHash.Range(_seed, cellX, cellY, spread, profile.Salt + 1);
        long y = cellY * profile.Spacing +
            DeterministicHash.Range(_seed, cellX, cellY, spread, profile.Salt + 2);
        bool generated = DeterministicHash.Unit(_seed, cellX, cellY, profile.Salt + 3) < profile.Chance;
        bool spawnProtected = DistanceSquared(x, y, 0, 0) <
            (double)profile.SpawnProtectionRadius * profile.SpawnProtectionRadius;
        if (!generated || spawnProtected || !definition.Allows(_biomes.Select(x, y)))
        {
            placement = default;
            return false;
        }

        placement = new StructurePlacement(
            definition.Id, x, y,
            DeterministicHash.At(_seed, x, y, profile.Salt + 4), definition.DefinitionId,
            DeterministicHash.Range(_seed, x, y, 4, profile.Salt + 5),
            DeterministicHash.Range(_seed, x, y, 4, profile.Salt + 6),
            profile.FootprintRadius);
        return true;
    }

    /// <summary>
    /// 以双精度计算世界距离，避免远离原点后 long 平方溢出。
    /// </summary>
    private static double DistanceSquared(long ax, long ay, long bx, long by)
    {
        double dx = (double)ax - bx;
        double dy = (double)ay - by;
        return dx * dx + dy * dy;
    }
}
