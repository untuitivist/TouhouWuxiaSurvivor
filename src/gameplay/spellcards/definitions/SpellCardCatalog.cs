namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;

using TouhouWuxiaSurvivor.Gameplay.Progression.Definitions;

/// <summary>
/// 集中声明灵梦当前可装备符卡及其原作出处，保持角色内容和执行逻辑分离。
/// </summary>
public static class SpellCardCatalog
{
    public static SpellCardDefinition FantasySeal { get; } = new(
        "reimu_fantasy_seal",
        "灵符「梦想封印」",
        "梦想封印",
        "博丽灵梦",
        "初出：东方红魔乡",
        "博丽奥义 · 追封",
        "放出八枚灵玉，各自追踪附近妖怪并施加封魔伤害。",
        SpellCardEffectKind.FantasySeal,
        RunUpgradeKind.FantasySeal,
        new SpellCardCombatProfile(100, 4.0f, 560.0f, 8, 8, 0.0f));

    public static SpellCardDefinition EvilSealingCircle { get; } = new(
        "reimu_evil_sealing_circle",
        "梦符「封魔阵」",
        "封魔阵",
        "博丽灵梦",
        "初出：东方红魔乡",
        "博丽阵法 · 护身",
        "展开近身封魔阵伤害群敌，并以结界护身一段时间。",
        SpellCardEffectKind.EvilSealingCircle,
        RunUpgradeKind.EvilSealingCircle,
        new SpellCardCombatProfile(70, 6.0f, 176.0f, 6, 0, 1.25f));

    public static IReadOnlyList<SpellCardDefinition> ReimuLoadout { get; } =
        [FantasySeal, EvilSealingCircle];
}
