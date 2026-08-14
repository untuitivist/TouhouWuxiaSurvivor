using TouhouWuxiaSurvivor.Gameplay.SpellCards.Contracts;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;

namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Balance;

/// <summary>
/// 统一奥义的命中兑现、自动触发、范围衰减与护身折算口径，供运行模拟、契约测试和策划工具对照。
/// </summary>
public static class SpellCardContributionModel
{
    public const double AreaEdgeDamageMultiplier = 0.45;
    public const double GuardDefenseCreditWeight = 0.17;

    /// <summary>
    /// 返回指定效果把理论伤害送达目标的可靠度；该权重不包含范围距离衰减或触发条件。
    /// </summary>
    public static double DeliveryWeight(SpellCardEffectKind effectKind) => effectKind switch
    {
        SpellCardEffectKind.HomingVolley => 0.90,
        SpellCardEffectKind.FocusedVolley => 1.00,
        SpellCardEffectKind.AreaBurst => 0.85,
        SpellCardEffectKind.GuardField => 0.72,
        _ => throw new ArgumentOutOfRangeException(nameof(effectKind)),
    };

    /// <summary>
    /// 返回自动触发在典型战况中的长期可用率；受击与敌群触发因此不会被当作无条件定时施放。
    /// </summary>
    public static double ActivationAvailability(
        SpellCardActivationKind activationKind) => activationKind switch
    {
        SpellCardActivationKind.Periodic => 1.00,
        SpellCardActivationKind.Crowd => 0.90,
        SpellCardActivationKind.OnDamaged => 0.58,
        _ => throw new ArgumentOutOfRangeException(nameof(activationKind)),
    };

    /// <summary>
    /// 把圆内均匀分布目标的线性距离衰减积分为期望伤害；面积密度随半径成正比，结果为 (1+2m)/3。
    /// </summary>
    public static double ExpectedTargetDamageMultiplier(SpellCardEffectKind effectKind) =>
        effectKind is SpellCardEffectKind.AreaBurst or SpellCardEffectKind.GuardField
            ? (1.0 + 2.0 * AreaEdgeDamageMultiplier) / 3.0
            : 1.0;

    /// <summary>
    /// 计算不含基础属性的攻势预算，统一组合伤害、目标承载、周期、投射可靠度、范围衰减和触发可用率。
    /// </summary>
    public static double CalculateOffenseBudget(SpellCardDefinition card)
    {
        ArgumentNullException.ThrowIfNull(card);
        return card.Combat.DamageScale * card.Combat.TargetScale /
            card.Combat.IntervalScale * DeliveryWeight(card.EffectKind) *
            ExpectedTargetDamageMultiplier(card.EffectKind) *
            ActivationAvailability(card.ActivationKind);
    }

    /// <summary>
    /// 将护身持续时间折算为伤害等价预算，并同样计入自动触发可用率；非护身效果固定返回零。
    /// </summary>
    public static double CalculateDefenseCredit(SpellCardDefinition card)
    {
        ArgumentNullException.ThrowIfNull(card);
        return card.EffectKind == SpellCardEffectKind.GuardField
            ? card.Combat.DefenseScale / card.Combat.IntervalScale *
                GuardDefenseCreditWeight * ActivationAvailability(card.ActivationKind)
            : 0.0;
    }

    /// <summary>合并攻势与护身折算，得到跨效果可比较且不依赖角色基础值的单卡契约预算。</summary>
    public static double CalculateBudget(SpellCardDefinition card) =>
        CalculateOffenseBudget(card) + CalculateDefenseCredit(card);

    /// <summary>
    /// 将单卡契约预算投影为每秒伤害等价贡献；角色强度只通过攻势、目标容量和奥义基础周期进入。
    /// </summary>
    public static double ProjectPerSecond(
        SpellCardDefinition card,
        SpellCardBaseAttributes attributes)
    {
        ArgumentNullException.ThrowIfNull(attributes);
        double baseContribution = attributes.AttackPower *
            attributes.UltimateTargetCapacity / attributes.UltimateIntervalSeconds;
        return baseContribution * CalculateBudget(card);
    }
}
