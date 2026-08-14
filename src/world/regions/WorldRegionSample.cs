using TouhouWuxiaSurvivor.World.Biomes;

namespace TouhouWuxiaSurvivor.World.Regions;

/// <summary>
/// 描述绝对坐标所属宏域及其内部层级，供群系、结构和地图共享同一空间语义。
/// </summary>
public readonly record struct WorldRegionSample(
    bool IsOfficial,
    string PackId,
    BiomeId Biome,
    WorldRegionLayer Layer,
    WorldRegionSite Site,
    double RadialDistance);
