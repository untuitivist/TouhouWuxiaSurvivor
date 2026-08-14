namespace TouhouWuxiaSurvivor.Combat.Weapons;

/// <summary>
/// 集中处理角色普攻间隔与时间窗内齐射次数，使运行时和角色横向预算使用同一口径。
/// </summary>
public static class AutoAttackCadence
{
    public const double InitialDelaySeconds = 0.15;

    /// <summary>
    /// 合成武器基础间隔、角色节奏和构筑射速；异常输入会钳制为可推进的有限间隔。
    /// </summary>
    public static double CalculateInterval(
        double baseIntervalSeconds,
        double characterIntervalMultiplier,
        double fireRateMultiplier)
    {
        double baseInterval = double.IsFinite(baseIntervalSeconds)
            ? Math.Max(0.01, baseIntervalSeconds)
            : 0.28;
        double characterInterval = double.IsFinite(characterIntervalMultiplier)
            ? Math.Max(0.1, characterIntervalMultiplier)
            : 1.0;
        double fireRate = double.IsFinite(fireRateMultiplier)
            ? Math.Max(0.1, fireRateMultiplier)
            : 100.0;
        return Math.Max(0.01, baseInterval * characterInterval / fireRate);
    }

    /// <summary>
    /// 计算固定观察窗内实际可触发的齐射数，包含正式武器使用的首次短延迟。
    /// </summary>
    public static int CountVolleys(
        double durationSeconds,
        double intervalSeconds,
        double initialDelaySeconds = InitialDelaySeconds)
    {
        if (!double.IsFinite(durationSeconds) || durationSeconds < initialDelaySeconds ||
            !double.IsFinite(intervalSeconds) || intervalSeconds <= 0.0)
        {
            return 0;
        }

        double activeWindow = Math.Max(0.0, durationSeconds - initialDelaySeconds);
        return 1 + (int)Math.Floor(activeWindow / intervalSeconds + 0.0000001);
    }
}
