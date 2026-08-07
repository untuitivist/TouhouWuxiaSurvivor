using TouhouWuxiaSurvivor.World.Official;

namespace TouhouWuxiaSurvivor.World.Biomes;

/// <summary>
/// 集中维护生物群系在中文界面中的显示名称，避免 UI 层重复判断枚举。
/// </summary>
public static class BiomeNames
{
    /// <summary>
    /// 将群系枚举转换为稳定的中文名称，未知值回退为幻想乡原野。
    /// </summary>
    public static string GetChinese(BiomeId biome)
    {
        if (OfficialWorldContentCatalog.TryGet(biome, out OfficialWorldContentDefinition definition))
        {
            return definition.BiomeName;
        }

        return biome switch
        {
            BiomeId.HakureiShrine => "博丽神社",
            BiomeId.HumanVillage => "人间之里",
            BiomeId.MagicForest => "魔法森林",
            BiomeId.YoukaiMountain => "妖怪之山",
            _ => "幻想乡原野",
        };
    }
}
