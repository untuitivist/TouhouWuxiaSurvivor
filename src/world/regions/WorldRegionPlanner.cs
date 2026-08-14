using TouhouWuxiaSurvivor.Content;
using TouhouWuxiaSurvivor.World.Biomes;
using TouhouWuxiaSurvivor.World.Coordinates;
using TouhouWuxiaSurvivor.World.Generation;
using TouhouWuxiaSurvivor.World.Official;

namespace TouhouWuxiaSurvivor.World.Regions;

/// <summary>
/// 以抖动 Voronoi 站点建立无限宏域，并在单个正作宏域内生成外围、内部和核心三层地区。
/// </summary>
public sealed class WorldRegionPlanner
{
    public const int CellSize = 384;
    private const int SiteMargin = 144;
    private const double CoreRadius = 56.0;
    private const double InnerRadius = 112.0;
    private const int SiteCacheCapacity = 2048;
    private readonly ulong _seed;
    private readonly OfficialWorldContentDefinition[][] _packs;
    private readonly Dictionary<(long X, long Y), WorldRegionSite> _siteCache = [];
    private readonly Queue<(long X, long Y)> _siteCacheOrder = [];

    /// <summary>
    /// 缓存所有已启用作品的三地区定义；本体与每个作品各占一个等权来源。
    /// </summary>
    public WorldRegionPlanner(ulong seed, ContentPackSelection content)
    {
        _seed = seed;
        _packs = OfficialWorldContentCatalog.All
            .Where(definition => content.IsEnabled(definition.PackId))
            .GroupBy(definition => definition.PackId, StringComparer.Ordinal)
            .Select(group => group.OrderBy(definition => definition.RegionIndex).ToArray())
            .OrderBy(definitions => definitions[0].Number)
            .ToArray();
    }

    /// <summary>
    /// 采样绝对 Tile 坐标：先扭曲查询位置，再找九宫格内最近站点，最后按扰动径向距离选择三层地区。
    /// </summary>
    public WorldRegionSample Sample(long tileX, long tileY)
    {
        (long warpedX, long warpedY) = Warp(tileX, tileY);
        long cellX = GridMath.FloorDiv(warpedX, CellSize);
        long cellY = GridMath.FloorDiv(warpedY, CellSize);
        WorldRegionSite nearest = default;
        double nearestSquared = double.MaxValue;

        for (long y = cellY - 1; y <= cellY + 1; y++)
        {
            for (long x = cellX - 1; x <= cellX + 1; x++)
            {
                WorldRegionSite site = CreateSite(x, y);
                double dx = warpedX - site.CenterX;
                double dy = warpedY - site.CenterY;
                double squared = dx * dx + dy * dy;
                if (squared < nearestSquared ||
                    squared == nearestSquared && ComesFirst(site, nearest))
                {
                    nearest = site;
                    nearestSquared = squared;
                }
            }
        }

        if (nearest.SourceIndex == 0)
        {
            return new(false, string.Empty, BiomeId.Common, WorldRegionLayer.Outer,
                nearest, Math.Sqrt(nearestSquared));
        }

        double radialDistance = Math.Sqrt(nearestSquared) + RadialDistortion(tileX, tileY);
        WorldRegionLayer layer = radialDistance switch
        {
            < CoreRadius => WorldRegionLayer.Core,
            < InnerRadius => WorldRegionLayer.Inner,
            _ => WorldRegionLayer.Outer,
        };
        OfficialWorldContentDefinition[] definitions = _packs[nearest.SourceIndex - 1];
        int regionIndex = layer switch
        {
            WorldRegionLayer.Core => 2,
            WorldRegionLayer.Inner => 1,
            _ => 0,
        };
        OfficialWorldContentDefinition definition = definitions[regionIndex];
        return new(true, definition.PackId, definition.Biome, layer, nearest, radialDistance);
    }

    /// <summary>
    /// 为宏域网格单元创建中心区域内的抖动站点，并等权选择本体或任一已启用作品。
    /// </summary>
    public WorldRegionSite CreateSite(long cellX, long cellY)
    {
        if (_siteCache.TryGetValue((cellX, cellY), out WorldRegionSite cached))
        {
            return cached;
        }

        int jitterRange = CellSize - SiteMargin * 2 + 1;
        long centerX = cellX * CellSize + SiteMargin +
            DeterministicHash.Range(_seed, cellX, cellY, jitterRange, 0x52454710);
        long centerY = cellY * CellSize + SiteMargin +
            DeterministicHash.Range(_seed, cellX, cellY, jitterRange, 0x52454711);
        int source = DeterministicHash.Range(
            _seed, cellX, cellY, _packs.Length + 1, 0x52454712);
        var site = new WorldRegionSite(cellX, cellY, centerX, centerY, source);
        TrimSiteCache();
        _siteCache.Add((cellX, cellY), site);
        _siteCacheOrder.Enqueue((cellX, cellY));
        return site;
    }

    /// <summary>
    /// 在插入新站点前淘汰最早访问的坐标，使玩家无限旅行时规划器内存仍保持常量上限。
    /// </summary>
    private void TrimSiteCache()
    {
        while (_siteCache.Count >= SiteCacheCapacity && _siteCacheOrder.TryDequeue(out var oldest))
        {
            _siteCache.Remove(oldest);
        }
    }

    /// <summary>
    /// 用低频连续噪声轻微偏移查询坐标，使相邻宏域边界弯曲且不会沿网格硬切。
    /// </summary>
    private (long X, long Y) Warp(long tileX, long tileY)
    {
        double xNoise = ValueNoise2D.Sample(_seed, tileX, tileY, 256, 0x52454720) - 0.5;
        double yNoise = ValueNoise2D.Sample(_seed, tileX, tileY, 256, 0x52454721) - 0.5;
        return (
            tileX + (long)Math.Round(xNoise * 112.0),
            tileY + (long)Math.Round(yNoise * 112.0));
    }

    /// <summary>
    /// 对内部同心层追加小尺度连续扰动，消除规则圆环但保留外围到核心的可读路线。
    /// </summary>
    private double RadialDistortion(long tileX, long tileY) =>
        (ValueNoise2D.Sample(_seed, tileX, tileY, 96, 0x52454730) - 0.5) * 36.0;

    /// <summary>
    /// 在极少数距离完全相等时按单元坐标确定顺序，避免遍历次序影响归属。
    /// </summary>
    private static bool ComesFirst(WorldRegionSite candidate, WorldRegionSite current) =>
        candidate.CellY < current.CellY ||
        candidate.CellY == current.CellY && candidate.CellX < current.CellX;
}
