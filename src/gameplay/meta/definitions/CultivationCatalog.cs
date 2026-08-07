namespace TouhouWuxiaSurvivor.Gameplay.Meta.Definitions;

/// <summary>
/// 集中保存博丽灵梦的四项神社整备，并为存档和界面提供稳定目录顺序。
/// </summary>
public static class CultivationCatalog
{
    public static IReadOnlyList<CultivationDefinition> All { get; } =
    [
        new("hakurei_barrier", "博丽护身结界", "每重增加 1 点初始生命。",
            CultivationKind.MaxHealth, 3, 16, 12, 0),
        new("floating_practice", "空中飘浮", "每重提高 2% 基础移动速度。",
            CultivationKind.MoveSpeed, 5, 12, 8, 30),
        new("yin_yang_resonance", "阴阳玉共鸣", "每重提高 8% 灵息吸附范围。",
            CultivationKind.SpiritAttraction, 5, 10, 6, 0),
        new("persuasion_needle_tuning", "封魔针调律", "每重令自机弹幕伤害增加 1。",
            CultivationKind.Damage, 2, 40, 45, 100),
    ];

    /// <summary>
    /// 按稳定存档 ID 查找定义，未知或空 ID 返回空值而不抛出异常。
    /// </summary>
    public static CultivationDefinition? Find(string id) =>
        All.FirstOrDefault(definition => definition.Id == id);
}
