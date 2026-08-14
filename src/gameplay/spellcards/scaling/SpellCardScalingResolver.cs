using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Contracts;

namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Scaling;

/// <summary>
/// 把内容中的相对系数投影为本次奥义最终数值，是全部作品共享的唯一缩放公式。
/// </summary>
public static class SpellCardScalingResolver
{
    /// <summary>
    /// 按角色和构筑实效属性解析伤害、范围、数量、护身、弹速与周期，并在整数边界饱和。
    /// </summary>
    public static ResolvedSpellCardCombat Resolve(
        SpellCardCombatProfile profile,
        SpellCardBaseAttributes attributes)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(attributes);
        float range = SaturatedPositive(
            (double)attributes.TargetRange * profile.RangeScale);
        float speed = SaturatedPositive(
            (double)attributes.ProjectileSpeed * profile.ProjectileSpeedScale);
        int damage = RoundPositive(
            (double)attributes.AttackPower * profile.DamageScale);
        int targets = profile.TargetScale <= 0.0f
            ? Math.Max(1, attributes.UltimateTargetCapacity)
            : RoundPositive(
                (double)attributes.UltimateTargetCapacity * profile.TargetScale);
        return new ResolvedSpellCardCombat(
            SaturatedPositive(
                (double)attributes.UltimateIntervalSeconds * profile.IntervalScale),
            range,
            damage,
            targets,
            RoundPositive((double)attributes.UltimateTargetCapacity *
                profile.ActivationThresholdScale),
            SaturatedNonNegative(
                (double)attributes.DefenseSeconds * profile.DefenseScale),
            speed,
            SaturatedNonNegative((double)range * profile.ImpactRangeScale),
            SaturatedPositive((double)range / speed * profile.TravelDurationScale),
            SaturatedPositive(
                (double)attributes.SpawnDistance * profile.SpawnDistanceScale),
            SaturatedPositive(attributes.FireIntervalSeconds));
    }

    /// <summary>把正浮点结果按远离零的规则转为至少一的整数，并避免无尽构筑溢出。</summary>
    private static int RoundPositive(double value) => (int)Math.Clamp(
        Math.Round(value, MidpointRounding.AwayFromZero), 1.0, int.MaxValue);

    /// <summary>将正结果饱和到单精度有限范围，避免无尽倍率把坐标与速度传播为 Infinity。</summary>
    private static float SaturatedPositive(double value) => (float)Math.Clamp(
        double.IsFinite(value) ? value : double.MaxValue,
        float.Epsilon,
        float.MaxValue);

    /// <summary>将允许为零的结果饱和到有限范围，并保留护身或范围维度的零哨兵语义。</summary>
    private static float SaturatedNonNegative(double value) => (float)Math.Clamp(
        double.IsFinite(value) ? value : double.MaxValue,
        0.0,
        float.MaxValue);
}
