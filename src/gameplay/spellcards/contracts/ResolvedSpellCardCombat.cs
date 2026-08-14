namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Contracts;

/// <summary>
/// 保存一张奥义在当前施展瞬间换算出的最终数值，效果执行器不再接触内容倍率。
/// </summary>
public sealed class ResolvedSpellCardCombat
{
    public float IntervalSeconds { get; }
    public float EffectRange { get; }
    public int Damage { get; }
    public int TargetCount { get; }
    public int ActivationThreshold { get; }
    public float DefenseSeconds { get; }
    public float ProjectileSpeed { get; }
    public float ImpactRange { get; }
    public float TravelDurationSeconds { get; }
    public float SpawnDistance { get; }
    public float CastLockSeconds { get; }

    /// <summary>保存解析器已校验的最终值，形成协调器与效果执行器之间的稳定边界。</summary>
    public ResolvedSpellCardCombat(
        float intervalSeconds,
        float effectRange,
        int damage,
        int targetCount,
        int activationThreshold,
        float defenseSeconds,
        float projectileSpeed,
        float impactRange,
        float travelDurationSeconds,
        float spawnDistance,
        float castLockSeconds)
    {
        IntervalSeconds = intervalSeconds;
        EffectRange = effectRange;
        Damage = damage;
        TargetCount = targetCount;
        ActivationThreshold = activationThreshold;
        DefenseSeconds = defenseSeconds;
        ProjectileSpeed = projectileSpeed;
        ImpactRange = impactRange;
        TravelDurationSeconds = travelDurationSeconds;
        SpawnDistance = spawnDistance;
        CastLockSeconds = castLockSeconds;
    }
}
