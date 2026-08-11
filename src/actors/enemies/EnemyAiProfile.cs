namespace TouhouWuxiaSurvivor.Actors.Enemies;

/// <summary>
/// 保存一种敌人移动决策的可调参数，使同一批量系统能够执行不同 AI，而非依赖原型硬编码。
/// </summary>
public sealed class EnemyAiProfile
{
    public static EnemyAiProfile Chase { get; } = new(EnemyAiKind.Chase, 0.0f, 1.0f, 0.0f, 0.0f);
    public static EnemyAiProfile OrbitShooter { get; } = new(EnemyAiKind.OrbitShooter, 150.0f, 0.72f, 0.0f, 0.0f);
    public static EnemyAiProfile Charger { get; } = new(EnemyAiKind.Charger, 0.0f, 0.45f, 1.65f, 0.42f);
    public static EnemyAiProfile BossPhased { get; } = new(EnemyAiKind.BossPhased, 190.0f, 0.58f, 0.0f, 0.0f);

    public EnemyAiKind Kind { get; }
    public float PreferredRange { get; }
    public float TangentialWeight { get; }
    public float ChargeInterval { get; }
    public float ChargeDuration { get; }

    /// <summary>
    /// 构造经过非负钳制的移动档案；偏好距离只对绕行类 AI 生效，突进计时只对突进类生效。
    /// </summary>
    public EnemyAiProfile(
        EnemyAiKind kind,
        float preferredRange,
        float tangentialWeight,
        float chargeInterval,
        float chargeDuration)
    {
        Kind = kind;
        PreferredRange = Math.Max(0.0f, preferredRange);
        TangentialWeight = Math.Max(0.0f, tangentialWeight);
        ChargeInterval = Math.Max(0.0f, chargeInterval);
        ChargeDuration = Math.Max(0.0f, chargeDuration);
    }
}
