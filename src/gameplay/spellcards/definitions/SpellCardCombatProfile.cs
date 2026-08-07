namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;

/// <summary>
/// 保存一张符卡的可平衡战斗参数，避免定义构造函数堆积零散数值。
/// </summary>
public sealed class SpellCardCombatProfile
{
    public int PowerCost { get; }
    public float CooldownSeconds { get; }
    public float EffectRange { get; }
    public int Damage { get; }
    public int TargetCount { get; }
    public float DefenseSeconds { get; }

    /// <summary>
    /// 建立经过下限约束的符卡参数；不适用的目标数或防御时间允许为零。
    /// </summary>
    public SpellCardCombatProfile(
        int powerCost,
        float cooldownSeconds,
        float effectRange,
        int damage,
        int targetCount,
        float defenseSeconds)
    {
        PowerCost = Math.Max(1, powerCost);
        CooldownSeconds = Math.Max(0.0f, cooldownSeconds);
        EffectRange = Math.Max(1.0f, effectRange);
        Damage = Math.Max(1, damage);
        TargetCount = Math.Max(0, targetCount);
        DefenseSeconds = Math.Max(0.0f, defenseSeconds);
    }
}
