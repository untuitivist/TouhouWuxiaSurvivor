namespace TouhouWuxiaSurvivor.Gameplay.Difficulty;

/// <summary>
/// 根据本局构筑和弹丸池余量规划预判弹组与成对定向弹幕；压力阶段绝不赠送火力。
/// </summary>
public static class PlayerBarrageCurve
{
    public const int MaximumAimedProjectilesPerVolley = 4;
    public const int MaximumBarrageProjectilesPerVolley = 10;
    public const int MaximumProjectilesPerVolley =
        MaximumAimedProjectilesPerVolley + MaximumBarrageProjectilesPerVolley;
    public const int ProjectileSoftLimit = 1600;
    public const double SaturatedRetryIntervalSeconds = 0.05;

    /// <summary>
    /// 生成一次构筑齐射：至少一发预判弹，额外弹幕成对加入，收束效果只改变有效形态。
    /// </summary>
    public static PlayerBarrageSnapshot Evaluate(
        bool convergingActive,
        int activeProjectileCount,
        int aimedProjectileBonus = 0,
        int barrageProjectileBonus = 0)
    {
        int requestedAimed = Math.Clamp(1 + aimedProjectileBonus,
            1, MaximumAimedProjectilesPerVolley);
        int requestedBarrage = NormalizeBarrageCount(
            barrageProjectileBonus, convergingActive);
        (int aimed, int barrage) = ApplyProjectileBudget(
            requestedAimed, requestedBarrage, activeProjectileCount);
        PlayerBarrageMode mode = GetMode(requestedBarrage, convergingActive);
        double angularStep = GetAngularStep(requestedBarrage, mode);
        const bool requiresTarget = true;
        double retry = aimed + barrage == 0 ? SaturatedRetryIntervalSeconds : 0.0;
        return new PlayerBarrageSnapshot(mode,
            requestedAimed, aimed, requestedBarrage, barrage,
            angularStep, requiresTarget, retry);
    }

    /// <summary>兼容旧纯函数调用形状；时间参数被明确忽略，测试可据此锁定阶段不会赠送火力。</summary>
    public static PlayerBarrageSnapshot EvaluateSeconds(
        double ignoredElapsedSeconds,
        bool convergingActive,
        long ignoredVolleySequence,
        int activeProjectileCount,
        int barrageProjectileBonus = 0,
        int aimedProjectileBonus = 0) => Evaluate(
            convergingActive, activeProjectileCount,
            aimedProjectileBonus, barrageProjectileBonus);

    /// <summary>
    /// 把构筑提供的弹幕整理为偶数；收束效果至少保留左右一对弹幕。
    /// </summary>
    private static int NormalizeBarrageCount(
        int barrageProjectileBonus,
        bool convergingActive)
    {
        int requested = Math.Clamp(barrageProjectileBonus, 0,
            MaximumBarrageProjectilesPerVolley);
        if (convergingActive)
        {
            requested = Math.Max(2, requested);
        }

        return requested - requested % 2;
    }

    /// <summary>
    /// 单发使用预测直射，多发使用目标扇形；明确取得的阵法效果改为两翼收束。
    /// </summary>
    private static PlayerBarrageMode GetMode(
        int requestedBarrage,
        bool convergingActive)
    {
        if (convergingActive && requestedBarrage > 0)
        {
            return PlayerBarrageMode.ConvergingFormation;
        }

        return requestedBarrage <= 0
            ? PlayerBarrageMode.TargetedSingle
            : PlayerBarrageMode.AlternatingFan;
    }

    /// <summary>
    /// 把单轮弹数压入软上限余量；优先保留预判弹，弹幕只保留完整的左右成对数量。
    /// </summary>
    private static (int Aimed, int Barrage) ApplyProjectileBudget(
        int requestedAimed,
        int requestedBarrage,
        int activeProjectileCount)
    {
        int activeCount = Math.Max(0, activeProjectileCount);
        int available = Math.Max(0, ProjectileSoftLimit - activeCount);
        int aimed = Math.Min(requestedAimed, available);
        int remaining = Math.Max(0, available - aimed);
        int barrage = Math.Min(requestedBarrage, remaining);
        barrage -= barrage % 2;
        return (aimed, barrage);
    }

    /// <summary>
    /// 为扇形提供随弹幕数量收紧的相邻夹角；收束阵使用平行出生线而不消费角度。
    /// </summary>
    private static double GetAngularStep(int requestedBarrage, PlayerBarrageMode mode)
    {
        if (mode == PlayerBarrageMode.ConvergingFormation)
        {
            return 0.0;
        }

        return requestedBarrage switch
        {
            2 => Math.PI / 18.0,
            4 => Math.PI / 24.0,
            6 => Math.PI / 30.0,
            8 => Math.PI / 34.0,
            10 => Math.PI / 38.0,
            _ => 0.0,
        };
    }
}
