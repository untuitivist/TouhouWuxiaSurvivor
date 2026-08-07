using TouhouWuxiaSurvivor.World.Official;

namespace TouhouWuxiaSurvivor.World.Structures;

/// <summary>
/// 集中维护结构在地图界面中的中文显示名称。
/// </summary>
public static class StructureNames
{
    /// <summary>
    /// 将结构枚举转换为稳定中文名称，未知值回退为无名地标。
    /// </summary>
    public static string GetChinese(StructureId structure)
    {
        if (OfficialWorldContentCatalog.TryGet(structure, out OfficialWorldContentDefinition definition))
        {
            return definition.StructureName;
        }

        return structure switch
        {
            StructureId.HakureiShrine => "博丽神社",
            StructureId.ShrineCourt => "结界庭院",
            StructureId.HumanVillage => "人间之里",
            StructureId.MagicCircle => "魔法阵遗迹",
            StructureId.LakeIsland => "雾湖小岛",
            StructureId.MountainTerrace => "山间梯田",
            StructureId.Crossroads => "荒野十字路",
            _ => "无名地标",
        };
    }
}
