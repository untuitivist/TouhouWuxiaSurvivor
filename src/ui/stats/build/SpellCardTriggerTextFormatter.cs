using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;
using TouhouWuxiaSurvivor.Ui.SpellCards;

namespace TouhouWuxiaSurvivor.Ui.Stats.Build;

/// <summary>
/// 把奥义槽位、自动触发规则与实时属性系数转换为统一中文说明。
/// </summary>
public static class SpellCardTriggerTextFormatter
{
    /// <summary>
    /// 组合四攻二护持容量、角色周天与实效属性倍率，供节点详情和搜索索引使用同一说明。
    /// </summary>
    public static string Format(SpellCardDefinition card)
    {
        SpellCardSlotKind kind = SpellCardSlotPolicy.Classify(card);
        string slot = kind == SpellCardSlotKind.Support
            ? $"护持 {SpellCardSlotPolicy.MaximumSupportSlots}槽"
            : $"主攻 {SpellCardSlotPolicy.MaximumOffensiveSlots}槽";
        return $"{slot} · {SpellCardGeometryText.GetName(card.GeometryKind)} · " +
            $"{SpellCardActivationText.GetShortName(card.ActivationKind)}\n" +
            $"实时：周天×{card.Combat.IntervalScale:0.##} · " +
            $"攻势×{card.Combat.DamageScale:0.##} · " +
            $"索敌×{card.Combat.RangeScale:0.##}";
    }

    /// <summary>返回不依赖旧资源系统的完整自动触发条件，供图鉴详细说明复用。</summary>
    public static string DescribeAutomaticTrigger(SpellCardDefinition card) =>
        card.ActivationKind switch
        {
            SpellCardActivationKind.Periodic => "独立周天到时自动施展",
            SpellCardActivationKind.Crowd =>
                "独立周天就绪后，敌群达到角色承载门槛即自动施展",
            SpellCardActivationKind.OnDamaged =>
                "独立周天就绪后，角色受击即自动施展",
            _ => throw new ArgumentOutOfRangeException(nameof(card)),
        };
}
