using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;

namespace TouhouWuxiaSurvivor.Ui.SpellCards;

/// <summary>
/// 把奥义自动运转类型投影为统一中文说明，供图鉴、构筑图和 HUD 共用且不复制规则文案。
/// </summary>
public static class SpellCardActivationText
{
    /// <summary>返回适合紧凑标题与节点标记的触发类型名称。</summary>
    public static string GetShortName(SpellCardActivationKind kind) => kind switch
    {
        SpellCardActivationKind.Periodic => "周期运转",
        SpellCardActivationKind.Crowd => "敌群响应",
        SpellCardActivationKind.OnDamaged => "受击响应",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    /// <summary>返回完整自动施展条件，并明确三类机制均不消耗灵力或要求主动按键。</summary>
    public static string Describe(SpellCardActivationKind kind) => kind switch
    {
        SpellCardActivationKind.Periodic =>
            "独立周天到时自动施展；无灵力消耗，无主动按键",
        SpellCardActivationKind.Crowd =>
            "独立周天就绪后，敌群达到角色承载门槛时自动施展；无灵力消耗",
        SpellCardActivationKind.OnDamaged =>
            "独立周天就绪后，受到伤害时自动施展；无灵力消耗",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };
}
