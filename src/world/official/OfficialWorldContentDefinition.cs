using TouhouWuxiaSurvivor.World.Biomes;
using TouhouWuxiaSurvivor.World.Structures;
using TouhouWuxiaSurvivor.World.Tiles;

namespace TouhouWuxiaSurvivor.World.Official;

/// <summary>
/// 保存一个正作内容包在无限世界中的地区、结构、敌人和地表映射。
/// </summary>
public sealed class OfficialWorldContentDefinition
{
    public int Number { get; }
    public int RegionIndex { get; }
    public string PackId { get; }
    public BiomeId Biome { get; }
    public string BiomeName { get; }
    public StructureId Structure { get; }
    public string StructureName { get; }
    public string EnemyName { get; }
    public TileId BaseTile { get; }
    public TileId DetailTile { get; }

    /// <summary>
    /// 构造一份正作世界增量定义，供群系、结构、地图和敌人系统共享同一来源。
    /// </summary>
    public OfficialWorldContentDefinition(
        int number,
        int regionIndex,
        string packId,
        BiomeId biome,
        string biomeName,
        StructureId structure,
        string structureName,
        string enemyName,
        TileId baseTile,
        TileId detailTile)
    {
        Number = number;
        RegionIndex = regionIndex;
        PackId = packId;
        Biome = biome;
        BiomeName = biomeName;
        Structure = structure;
        StructureName = structureName;
        EnemyName = enemyName;
        BaseTile = baseTile;
        DetailTile = detailTile;
    }
}
