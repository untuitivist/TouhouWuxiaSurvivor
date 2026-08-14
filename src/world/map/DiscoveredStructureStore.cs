using TouhouWuxiaSurvivor.World.Structures;

namespace TouhouWuxiaSurvivor.World.Map;

/// <summary>
/// 保存玩家亲自接近过的结构实例；稳定实例号使跨区块重载不会重复发现同一地标。
/// </summary>
public sealed class DiscoveredStructureStore
{
    private readonly Dictionary<ulong, StructurePlacement> _placements = [];

    public int Count => _placements.Count;

    /// <summary>
    /// 登记一个结构实例；首次发现返回 true，重复接近保持原记录并返回 false。
    /// </summary>
    public bool Discover(StructurePlacement placement) =>
        _placements.TryAdd(placement.InstanceId, placement);

    /// <summary>
    /// 判断稳定结构实例是否已经由玩家亲自发现。
    /// </summary>
    public bool Contains(ulong instanceId) => _placements.ContainsKey(instanceId);

    /// <summary>
    /// 返回指定绝对 Tile 矩形内已发现的结构，供旅行地图绘制名称。
    /// </summary>
    public IReadOnlyList<StructurePlacement> FindInBounds(
        long left,
        long top,
        long right,
        long bottom) => _placements.Values
        .Where(placement => placement.X >= left && placement.X <= right &&
            placement.Y >= top && placement.Y <= bottom)
        .OrderBy(placement => placement.Y)
        .ThenBy(placement => placement.X)
        .ToArray();
}
