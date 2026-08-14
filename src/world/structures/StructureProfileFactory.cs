using TouhouWuxiaSurvivor.World.StructureTemplates;

namespace TouhouWuxiaSurvivor.World.Structures;

/// <summary>
/// 按结构语义和地区层级生成差异化空间规则，避免所有地标共用一组概率。
/// </summary>
public static class StructureProfileFactory
{
    /// <summary>
    /// 为目录结构建立稳定 profile；外层地区更常见，核心地标更稀疏且相距更远。
    /// </summary>
    public static StructurePlacementProfile Create(
        StructureId id,
        StructureTemplateKind template,
        int regionIndex = -1)
    {
        StructureRarity rarity = regionIndex switch
        {
            0 => StructureRarity.Regional,
            1 => StructureRarity.Landmark,
            2 => StructureRarity.Mythic,
            _ => BaseRarity(template),
        };
        int spacing = rarity switch
        {
            StructureRarity.Common => 224,
            StructureRarity.Regional => 288,
            StructureRarity.Landmark => 320,
            _ => 384,
        };
        int separation = rarity switch
        {
            StructureRarity.Common => 80,
            StructureRarity.Regional => 112,
            StructureRarity.Landmark => 144,
            _ => 192,
        };
        double chance = rarity switch
        {
            StructureRarity.Common => 0.92,
            StructureRarity.Regional => 0.84,
            StructureRarity.Landmark => 0.76,
            _ => 0.68,
        };
        int footprint = Footprint(template);
        ulong salt = 0x7500UL + (ulong)(int)id * 0x9E37UL;
        return new StructurePlacementProfile(
            salt, spacing, separation, chance, 56, footprint,
            Math.Max(18, footprint + 8), rarity);
    }

    /// <summary>
    /// 将本体模板映射为空间稀有度；道路与小型法阵多见，大型宅邸与塔楼最远。
    /// </summary>
    private static StructureRarity BaseRarity(StructureTemplateKind template) => template switch
    {
        StructureTemplateKind.Crossroads or StructureTemplateKind.Circle => StructureRarity.Common,
        StructureTemplateKind.Settlement or StructureTemplateKind.Terrace => StructureRarity.Regional,
        StructureTemplateKind.Manor or StructureTemplateKind.Tower => StructureRarity.Mythic,
        _ => StructureRarity.Landmark,
    };

    /// <summary>
    /// 返回模板真实地表半径，生成器据此跨越任意数量的区块压印完整轮廓。
    /// </summary>
    private static int Footprint(StructureTemplateKind template) => template switch
    {
        StructureTemplateKind.Circle => 10,
        StructureTemplateKind.Ruin => 11,
        StructureTemplateKind.Shrine or StructureTemplateKind.Crossroads or
            StructureTemplateKind.Gate or StructureTemplateKind.Garden => 12,
        StructureTemplateKind.Stage or StructureTemplateKind.Market or
            StructureTemplateKind.Cave => 13,
        StructureTemplateKind.Terrace or StructureTemplateKind.Bridge or
            StructureTemplateKind.Tower or StructureTemplateKind.Outpost => 14,
        StructureTemplateKind.Settlement or StructureTemplateKind.Ship => 15,
        StructureTemplateKind.Manor => 16,
        _ => 12,
    };
}
