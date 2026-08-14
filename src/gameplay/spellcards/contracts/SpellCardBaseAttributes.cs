namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Contracts;

/// <summary>
/// 捕获一次施展所依赖的角色与构筑实效属性，奥义只消费这些通用维度而不读取孤立常数。
/// </summary>
public sealed class SpellCardBaseAttributes
{
    public float AttackPower { get; }
    public float FireIntervalSeconds { get; }
    public float TargetRange { get; }
    public float ProjectileSpeed { get; }
    public float DefenseSeconds { get; }
    public float UltimateIntervalSeconds { get; }
    public int UltimateTargetCapacity { get; }
    public float SpawnDistance { get; }

    /// <summary>
    /// 建立已在提供器边界整理的不可变快照，确保同一次奥义的全部投射物使用一致属性。
    /// </summary>
    public SpellCardBaseAttributes(
        float attackPower,
        float fireIntervalSeconds,
        float targetRange,
        float projectileSpeed,
        float defenseSeconds,
        float ultimateIntervalSeconds,
        int ultimateTargetCapacity,
        float spawnDistance)
    {
        AttackPower = RequirePositive(attackPower, nameof(attackPower));
        FireIntervalSeconds = RequirePositive(fireIntervalSeconds, nameof(fireIntervalSeconds));
        TargetRange = RequirePositive(targetRange, nameof(targetRange));
        ProjectileSpeed = RequirePositive(projectileSpeed, nameof(projectileSpeed));
        DefenseSeconds = RequirePositive(defenseSeconds, nameof(defenseSeconds));
        UltimateIntervalSeconds = RequirePositive(
            ultimateIntervalSeconds, nameof(ultimateIntervalSeconds));
        UltimateTargetCapacity = Math.Max(1, ultimateTargetCapacity);
        SpawnDistance = RequirePositive(spawnDistance, nameof(spawnDistance));
    }

    /// <summary>拒绝非有限或非正基础属性，让无尽倍率异常在施展前立即暴露。</summary>
    private static float RequirePositive(float value, string name) =>
        float.IsFinite(value) && value > 0.0f
            ? value
            : throw new ArgumentOutOfRangeException(name);
}
