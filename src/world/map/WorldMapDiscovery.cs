using TouhouWuxiaSurvivor.World.Structures;

namespace TouhouWuxiaSurvivor.World.Map;

/// <summary>
/// 协调玩家视野揭图与结构发现，避免区块加载窗口直接泄露未到达区域。
/// </summary>
public sealed class WorldMapDiscovery
{
    private readonly ExploredMapStore _explored;
    private readonly DiscoveredStructureStore _structures;
    private readonly StructureLocator _locator;
    private long _lastX = long.MinValue;
    private long _lastY = long.MinValue;

    /// <summary>
    /// 创建发现服务，并钳制地图视野与地标发现半径。
    /// </summary>
    public WorldMapDiscovery(
        ExploredMapStore explored,
        DiscoveredStructureStore structures,
        StructureLocator locator,
        int revealRadius = 22,
        int structureRadius = 16)
    {
        _explored = explored;
        _structures = structures;
        _locator = locator;
        RevealRadius = Math.Max(1, revealRadius);
        StructureRadius = Math.Max(1, structureRadius);
    }

    public int RevealRadius { get; }

    public int StructureRadius { get; }

    /// <summary>
    /// 玩家跨越至少一格时更新圆形探索视野，并发现实际进入半径的所有结构。
    /// </summary>
    public void Update(long tileX, long tileY)
    {
        if (tileX == _lastX && tileY == _lastY)
        {
            return;
        }

        _lastX = tileX;
        _lastY = tileY;
        _explored.RevealAround(tileX, tileY, RevealRadius);
        foreach (StructurePlacement placement in _locator.FindInBounds(
            tileX - StructureRadius,
            tileY - StructureRadius,
            tileX + StructureRadius,
            tileY + StructureRadius))
        {
            long dx = placement.X - tileX;
            long dy = placement.Y - tileY;
            if (dx * dx + dy * dy <= (long)StructureRadius * StructureRadius)
            {
                _structures.Discover(placement);
            }
        }
    }
}
