namespace TouhouWuxiaSurvivor.Gameplay.Difficulty;

/// <summary>
/// 根据本局构筑、齐射序号和弹丸池余量规划有效弹幕；敌人压力阶段绝不自动强化玩家。
/// </summary>
public static class PlayerBarrageCurve
{
    public const int MaximumProjectilesPerVolley = 7;
    public const int ProjectileSoftLimit = 1600;
    public const double SaturatedRetryIntervalSeconds = 0.05;

    /// <summary>
    /// 生成一次构筑齐射：额外弹丸决定一、三、五、七发，螺旋效果才会启用目标收束阵。
    /// </summary>
    public static PlayerBarrageSnapshot Evaluate(
        bool spiralActive,
        long volleySequence,
        int activeProjectileCount,
        int bonusProjectiles = 0)
    {
        int normalizedBonus = Math.Clamp(bonusProjectiles, 0,
            MaximumProjectilesPerVolley - 1);
        int requestedCount = GetRequestedProjectileCount(spiralActive, normalizedBonus);
        PlayerBarrageMode mode = GetMode(requestedCount, spiralActive);
        int allowedCount = ApplyProjectileBudget(requestedCount, mode, activeProjectileCount);
        double angularStep = GetAngularStep(requestedCount, mode);
        double rotation = GetRotation(mode, volleySequence, angularStep);
        const bool requiresTarget = true;
        double retry = allowedCount == 0 ? SaturatedRetryIntervalSeconds : 0.0;
        double damageBudget = 1.0 + normalizedBonus / 2.0 * 0.25 +
            (spiralActive ? 0.20 : 0.0);
        return new PlayerBarrageSnapshot(0.0, mode, requestedCount, allowedCount,
            angularStep, rotation, requiresTarget, retry, damageBudget);
    }

    /// <summary>兼容旧纯函数调用形状；时间参数被明确忽略，测试可据此锁定阶段不会赠送火力。</summary>
    public static PlayerBarrageSnapshot EvaluateSeconds(
        double ignoredElapsedSeconds,
        bool spiralActive,
        long volleySequence,
        int activeProjectileCount,
        int bonusProjectiles = 0) => Evaluate(
            spiralActive, volleySequence, activeProjectileCount, bonusProjectiles);

    /// <summary>
    /// 把构筑提供的偶数额外弹叠加到中心弹；螺旋强化至少保留正反双发。
    /// </summary>
    private static int GetRequestedProjectileCount(
        bool spiralActive,
        int bonusProjectiles)
    {
        int builtCount = Math.Min(MaximumProjectilesPerVolley, 1 + bonusProjectiles);
        return spiralActive ? Math.Max(2, builtCount) : builtCount;
    }

    /// <summary>
    /// 单发使用预测直射，多发使用目标扇形；只有明确取得的螺旋效果使用收束阵。
    /// </summary>
    private static PlayerBarrageMode GetMode(
        int requestedCount,
        bool spiralActive)
    {
        if (spiralActive)
        {
            return PlayerBarrageMode.ConvergingOrbit;
        }

        return requestedCount <= 1
            ? PlayerBarrageMode.TargetedSingle
            : PlayerBarrageMode.AlternatingFan;
    }

    /// <summary>
    /// 把单轮弹数压入软上限余量；扇形只保留奇数以维持中心弹，临界余量仍允许退化为一发。
    /// </summary>
    private static int ApplyProjectileBudget(
        int requestedCount,
        PlayerBarrageMode mode,
        int activeProjectileCount)
    {
        int activeCount = Math.Max(0, activeProjectileCount);
        int available = Math.Max(0, ProjectileSoftLimit - activeCount);
        int allowed = Math.Min(requestedCount, available);
        if (mode == PlayerBarrageMode.AlternatingFan && allowed > 1 && allowed % 2 == 0)
        {
            allowed--;
        }

        return allowed;
    }

    /// <summary>
    /// 为扇形提供随弹数收紧的相邻夹角；环形的相邻角由实际生成阶段在武器节点中均分整圆。
    /// </summary>
    private static double GetAngularStep(int requestedCount, PlayerBarrageMode mode)
    {
        if (mode == PlayerBarrageMode.ConvergingOrbit)
        {
            return Math.Tau / requestedCount;
        }

        return requestedCount switch
        {
            3 => Math.PI / 18.0,
            5 => Math.PI / 24.0,
            7 => Math.PI / 30.0,
            _ => 0.0,
        };
    }

    /// <summary>
    /// 扇形按齐射奇偶轻微左右错位，环形每轮转十度，形成连续但不会改变弹数的可读弹幕运动。
    /// </summary>
    private static double GetRotation(
        PlayerBarrageMode mode,
        long volleySequence,
        double angularStep)
    {
        if (mode == PlayerBarrageMode.TargetedSingle)
        {
            return 0.0;
        }

        if (mode == PlayerBarrageMode.AlternatingFan)
        {
            double sign = (volleySequence & 1L) == 0L ? -1.0 : 1.0;
            return sign * angularStep * 0.25;
        }

        double phase = volleySequence % 36L * Math.PI / 18.0;
        return phase < 0.0 ? phase + Math.Tau : phase;
    }

}
