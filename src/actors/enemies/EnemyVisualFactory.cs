using Godot;

namespace TouhouWuxiaSurvivor.Actors.Enemies;

/// <summary>
/// 按敌人原型配置中文名称文字的颜色，并提供统一的受伤与死亡显示状态。
/// </summary>
public static class EnemyVisualFactory
{
    /// <summary>
    /// 把目录中的完整中文名和原型颜色写入敌人标签，不加载任何示例图集资源。
    /// </summary>
    public static void Configure(Label label, EnemyDefinition definition)
    {
        label.Text = definition.DisplayName;
        label.Modulate = GetBaseColor(definition);
    }

    /// <summary>
    /// 将死亡敌人的文字替换为中文结果词，并用红色区分会自爆的敌人。
    /// </summary>
    public static void ConfigureDefeated(Label label, bool exploded)
    {
        label.Text = exploded ? "爆散" : "消散";
        label.Modulate = exploded ? new Color("ff5d52") : new Color("c5c5bf");
    }

    /// <summary>
    /// 返回各原型的基础识别色，使受伤闪烁结束后能够准确恢复而不是统一变白。
    /// </summary>
    public static Color GetBaseColor(EnemyDefinition definition) => definition.Archetype switch
    {
        EnemyArchetype.Fairy => new Color("c8e7ff"),
        EnemyArchetype.Kedama => new Color("f0eee5"),
        EnemyArchetype.Insect => new Color("b9d477"),
        EnemyArchetype.YinYangOrb => new Color("e0b47d"),
        EnemyArchetype.ForestSpirit => new Color("8fcf9b"),
        EnemyArchetype.BambooSpirit => new Color("c5d982"),
        EnemyArchetype.MountainSpirit => new Color("aeb9bd"),
        EnemyArchetype.VillageOutlaw => new Color("d6ad91"),
        EnemyArchetype.GreatYoukai => new Color("e5a3df"),
        EnemyArchetype.OfficialSpirit => new Color("b7aed9"),
        EnemyArchetype.ScarletMistInsect => new Color("f08b8b"),
        _ => new Color("b7aed9"),
    };
}
