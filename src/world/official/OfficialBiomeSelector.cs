using TouhouWuxiaSurvivor.Content;
using TouhouWuxiaSurvivor.World.Biomes;
using TouhouWuxiaSurvivor.World.Coordinates;
using TouhouWuxiaSurvivor.World.Generation;

namespace TouhouWuxiaSurvivor.World.Official;

/// <summary>
/// 在稀疏宏区块中确定性选择一个已启用正作地区，避免多内容包造成逐 Tile 线性开销。
/// </summary>
public sealed class OfficialBiomeSelector
{
    private const int RegionCellSize = 192;
    private readonly ulong _seed;
    private readonly OfficialWorldContentDefinition[] _enabled;

    /// <summary>
    /// 从本局不可变内容快照缓存已启用定义，后续地形生成不再读取菜单状态。
    /// </summary>
    public OfficialBiomeSelector(ulong seed, ContentPackSelection content)
    {
        _seed = seed;
        _enabled = OfficialWorldContentCatalog.All
            .Where(definition => content.IsEnabled(definition.PackId))
            .ToArray();
    }

    /// <summary>
    /// 查询坐标是否落入当前宏区块的圆形正作地区；没有启用包或位于区域外时返回 false。
    /// </summary>
    public bool TrySelect(long tileX, long tileY, out BiomeId biome)
    {
        biome = BiomeId.Common;
        if (_enabled.Length == 0)
        {
            return false;
        }

        long cellX = GridMath.FloorDiv(tileX, RegionCellSize);
        long cellY = GridMath.FloorDiv(tileY, RegionCellSize);
        if (DeterministicHash.Range(_seed, cellX, cellY, 100, 0x7710) >= 58)
        {
            return false;
        }

        int index = DeterministicHash.Range(_seed, cellX, cellY, _enabled.Length, 0x7711);
        long centerX = cellX * RegionCellSize + 48 +
            DeterministicHash.Range(_seed, cellX, cellY, 96, 0x7712);
        long centerY = cellY * RegionCellSize + 48 +
            DeterministicHash.Range(_seed, cellX, cellY, 96, 0x7713);
        int radius = 54 + DeterministicHash.Range(_seed, cellX, cellY, 24, 0x7714);
        long dx = tileX - centerX;
        long dy = tileY - centerY;
        if (dx * dx + dy * dy > (long)radius * radius)
        {
            return false;
        }

        biome = _enabled[index].Biome;
        return true;
    }
}
