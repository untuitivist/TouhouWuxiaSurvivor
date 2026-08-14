using TouhouWuxiaSurvivor.Content;
using TouhouWuxiaSurvivor.World.Biomes;
using TouhouWuxiaSurvivor.World.Regions;

namespace TouhouWuxiaSurvivor.World.Official;

/// <summary>
/// 从连续宏观地域规划中选择正作地区，保留原有查询接口供地形与结构系统使用。
/// </summary>
public sealed class OfficialBiomeSelector
{
    private readonly WorldRegionPlanner _regions;

    /// <summary>
    /// 从世界种子和本局不可变内容快照创建确定性宏域规划器。
    /// </summary>
    public OfficialBiomeSelector(ulong seed, ContentPackSelection content)
    {
        _regions = new WorldRegionPlanner(seed, content);
    }

    /// <summary>
    /// 查询坐标所属宏域；本体宏域返回 false，正作宏域返回同一作品内相邻三层之一。
    /// </summary>
    public bool TrySelect(long tileX, long tileY, out BiomeId biome)
    {
        WorldRegionSample sample = Sample(tileX, tileY);
        biome = sample.Biome;
        return sample.IsOfficial;
    }

    /// <summary>
    /// 返回完整宏域采样，供结构定位共享站点、作品和层级，不再独立猜测地区归属。
    /// </summary>
    public WorldRegionSample Sample(long tileX, long tileY) =>
        _regions.Sample(tileX, tileY);
}
