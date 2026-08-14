namespace TouhouWuxiaSurvivor.World.Regions;

/// <summary>
/// 保存一个确定性 Voronoi 宏域站点的位置与内容来源，站点不依赖区块加载顺序。
/// </summary>
public readonly record struct WorldRegionSite(
    long CellX,
    long CellY,
    long CenterX,
    long CenterY,
    int SourceIndex);
