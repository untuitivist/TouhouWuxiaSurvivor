using TouhouWuxiaSurvivor.World.Biomes;
using TouhouWuxiaSurvivor.World.Generation;
using TouhouWuxiaSurvivor.World.Official;

namespace TouhouWuxiaSurvivor.World.Tiles;

/// <summary>
/// 在群系基础地表中按绝对坐标选择可重复的 Tile 纹理变体，形成细粒度像素花纹。
/// </summary>
public sealed class BiomeTilePalette
{
    private readonly ulong _seed;

    /// <summary>
    /// 创建绑定到世界种子的地表调色板。
    /// </summary>
    public BiomeTilePalette(ulong seed) => _seed = seed;

    /// <summary>
    /// 根据群系和绝对坐标选择 TileId；相同输入始终返回相同结果。
    /// </summary>
    public TileId Pick(BiomeId biome, long x, long y)
    {
        int variation = DeterministicHash.Range(_seed, x, y, 100, 0x4400);
        return biome switch
        {
            BiomeId.HakureiShrine => variation < 18
                ? TileId.ShrineGrassPetals : TileId.ShrineGrassBase,
            BiomeId.HumanVillage => PickHumanVillage(variation),
            BiomeId.MagicForest => PickMagicForest(variation),
            BiomeId.MistyLake => PickMistyLake(x, y, variation),
            BiomeId.BambooForest => PickBambooForest(variation),
            BiomeId.YoukaiMountain => PickYoukaiMountain(variation),
            _ => PickOfficialOrCommon(biome, variation),
        };
    }

    /// <summary>
    /// 使用正作目录声明的两种地表砖块绘制细节；本体原野回退为普通草地。
    /// </summary>
    private static TileId PickOfficialOrCommon(BiomeId biome, int variation)
    {
        if (OfficialWorldContentCatalog.TryGet(biome, out OfficialWorldContentDefinition definition))
        {
            return variation < 24 ? definition.DetailTile : definition.BaseTile;
        }

        return variation < 13 ? TileId.GrassDots : TileId.GrassBase;
    }

    /// <summary>
    /// 混合草地、土路和少量石面，形成比原野更规整且适合聚落结构覆盖的基础地表。
    /// </summary>
    private static TileId PickHumanVillage(int variation) => variation switch
    {
        < 12 => TileId.StoneBase,
        < 34 => TileId.DirtPebbles,
        < 62 => TileId.DirtBase,
        < 74 => TileId.GrassDots,
        _ => TileId.GrassBase,
    };

    /// <summary>
    /// 按权重选择魔法森林的土壤、苔藓和落叶变体。
    /// </summary>
    private static TileId PickMagicForest(int variation) => variation switch
    {
        < 10 => TileId.MagicSoilSparkles,
        < 22 => TileId.MossDots,
        < 55 => TileId.MossBase,
        < 70 => TileId.ForestFloorLeaves,
        _ => TileId.ForestFloorBase
    };

    /// <summary>
    /// 用局部高频噪声划分雾之湖水面和湿地，并为两者选择细节变体。
    /// </summary>
    private TileId PickMistyLake(long x, long y, int variation)
    {
        double water = ValueNoise2D.Fractal(_seed, x, y, 48, 0x5500);
        if (water > 0.48)
        {
            return variation < 28 ? TileId.LakeWaterRipples : TileId.LakeWaterBase;
        }

        return variation < 18 ? TileId.WetGrassDroplets : TileId.WetGrassBase;
    }

    /// <summary>
    /// 按权重选择迷途竹林的竹叶地面与苔藓变体。
    /// </summary>
    private static TileId PickBambooForest(int variation) => variation switch
    {
        < 16 => TileId.BambooFloorLeaves,
        < 48 => TileId.BambooMossBase,
        < 61 => TileId.BambooMossDots,
        _ => TileId.BambooFloorBase
    };

    /// <summary>
    /// 按权重选择妖怪之山的草地、花草、岩石与裂纹变体。
    /// </summary>
    private static TileId PickYoukaiMountain(int variation) => variation switch
    {
        < 14 => TileId.MountainGrassFlowers,
        < 45 => TileId.MountainRockBase,
        < 58 => TileId.MountainRockCracks,
        _ => TileId.MountainGrassBase
    };
}
