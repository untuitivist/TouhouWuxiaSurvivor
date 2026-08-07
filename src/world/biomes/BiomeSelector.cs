using TouhouWuxiaSurvivor.Content;
using TouhouWuxiaSurvivor.World.Generation;
using TouhouWuxiaSurvivor.World.Official;

namespace TouhouWuxiaSurvivor.World.Biomes;

/// <summary>
/// 根据世界种子和绝对 Tile 坐标确定幻想乡生物群系，结果与区块加载顺序无关。
/// </summary>
public sealed class BiomeSelector
{
    private readonly ulong _seed;
    private readonly OfficialBiomeSelector _officialBiomes;

    /// <summary>
    /// 创建只启用幻想乡本体内容并绑定指定世界种子的群系选择器。
    /// </summary>
    public BiomeSelector(ulong seed) : this(seed, ContentPackSelection.BaseOnly)
    {
    }

    /// <summary>
    /// 创建绑定到指定世界种子和本局内容快照的群系选择器。
    /// </summary>
    public BiomeSelector(ulong seed, ContentPackSelection content)
    {
        _seed = seed;
        _officialBiomes = new OfficialBiomeSelector(seed, content);
    }

    /// <summary>
    /// 组合高度、湿度、魔力与聚落噪声选择本体群系，同时保证出生点周围为博丽神社安全区。
    /// </summary>
    public BiomeId Select(long tileX, long tileY)
    {
        if (IsInsideSpawnSanctuary(tileX, tileY))
        {
            return BiomeId.HakureiShrine;
        }

        if (_officialBiomes.TrySelect(tileX, tileY, out BiomeId officialBiome))
        {
            return officialBiome;
        }

        double elevation = ValueNoise2D.Fractal(_seed, tileX, tileY, 256, 0x1100);
        double moisture = ValueNoise2D.Fractal(_seed, tileX, tileY, 224, 0x2200);
        double magic = ValueNoise2D.Fractal(_seed, tileX, tileY, 192, 0x3300);
        double settlement = ValueNoise2D.Fractal(_seed, tileX, tileY, 320, 0x3380);

        if (elevation > 0.64)
        {
            return BiomeId.YoukaiMountain;
        }

        if (magic > 0.67 && elevation is > 0.32 and < 0.68)
        {
            return BiomeId.HakureiShrine;
        }

        if (settlement > 0.66 && elevation is > 0.34 and < 0.60 && magic < 0.55)
        {
            return BiomeId.HumanVillage;
        }

        return magic > 0.52 || moisture > 0.50
            ? BiomeId.MagicForest
            : BiomeId.Common;
    }

    /// <summary>
    /// 使用圆形边界判断绝对坐标是否位于出生点神社保护区。
    /// </summary>
    private static bool IsInsideSpawnSanctuary(long x, long y)
    {
        if (x is < -48 or > 48 || y is < -48 or > 48)
        {
            return false;
        }

        return x * x + y * y <= 48L * 48L;
    }
}
