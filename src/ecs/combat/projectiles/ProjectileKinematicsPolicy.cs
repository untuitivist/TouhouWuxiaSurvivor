namespace TouhouWuxiaSurvivor.Ecs.Combat.Projectiles;

/// <summary>
/// 定义玩家弹在正式运行时可兑现的运动边界，供战斗、面板、模拟与奥义统一钳制显示值。
/// </summary>
public static class ProjectileKinematicsPolicy
{
    public const float SoftCapStartSpeed = 1800.0f;
    public const float MaximumEffectiveSpeed = 2400.0f;

    /// <summary>
    /// 将任意原始弹速整理为零至安全软上限；常规数值保持原样，极高无尽重数仍有递减收益。
    /// </summary>
    public static float NormalizeSpeed(float rawSpeed)
    {
        if (float.IsNaN(rawSpeed) || float.IsNegativeInfinity(rawSpeed))
        {
            return 0.0f;
        }

        if (float.IsPositiveInfinity(rawSpeed))
        {
            return MaximumEffectiveSpeed;
        }

        float nonNegative = Math.Max(0.0f, rawSpeed);
        if (nonNegative <= SoftCapStartSpeed)
        {
            return nonNegative;
        }

        float span = MaximumEffectiveSpeed - SoftCapStartSpeed;
        float overflow = nonNegative - SoftCapStartSpeed;
        return SoftCapStartSpeed + span * overflow / (span + overflow);
    }
}
