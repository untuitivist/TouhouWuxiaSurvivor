namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;

/// <summary>
/// 保存一张奥义相对于角色实效属性的倍率，不保存任何最终战斗数值。
/// </summary>
public sealed class SpellCardCombatProfile
{
    public float IntervalScale { get; }
    public float RangeScale { get; }
    public float DamageScale { get; }
    public float TargetScale { get; }
    public float ActivationThresholdScale { get; }
    public float DefenseScale { get; }
    public float ProjectileSpeedScale { get; }
    public float ImpactRangeScale { get; }
    public float TravelDurationScale { get; }
    public float SpawnDistanceScale { get; }

    /// <summary>
    /// 建立经过边界约束的倍率档案；不使用的目标、护身与命中范围倍率允许为零。
    /// </summary>
    public SpellCardCombatProfile(
        float intervalScale,
        float rangeScale,
        float damageScale,
        float targetScale,
        float activationThresholdScale,
        float defenseScale,
        float projectileSpeedScale,
        float impactRangeScale,
        float travelDurationScale,
        float spawnDistanceScale)
    {
        IntervalScale = RequirePositive(intervalScale, nameof(intervalScale));
        RangeScale = RequirePositive(rangeScale, nameof(rangeScale));
        DamageScale = RequirePositive(damageScale, nameof(damageScale));
        TargetScale = RequireNonNegative(targetScale, nameof(targetScale));
        ActivationThresholdScale = RequirePositive(
            activationThresholdScale, nameof(activationThresholdScale));
        DefenseScale = RequireNonNegative(defenseScale, nameof(defenseScale));
        ProjectileSpeedScale = RequirePositive(
            projectileSpeedScale, nameof(projectileSpeedScale));
        ImpactRangeScale = RequireNonNegative(impactRangeScale, nameof(impactRangeScale));
        TravelDurationScale = RequirePositive(
            travelDurationScale, nameof(travelDurationScale));
        SpawnDistanceScale = RequirePositive(spawnDistanceScale, nameof(spawnDistanceScale));
    }

    /// <summary>拒绝非有限或非正倍率，避免内容错误污染运行时计时与战斗计算。</summary>
    private static float RequirePositive(float value, string name) =>
        float.IsFinite(value) && value > 0.0f
            ? value
            : throw new ArgumentOutOfRangeException(name);

    /// <summary>拒绝非有限负倍率，同时允许效果明确声明不使用对应战斗维度。</summary>
    private static float RequireNonNegative(float value, string name) =>
        float.IsFinite(value) && value >= 0.0f
            ? value
            : throw new ArgumentOutOfRangeException(name);
}
