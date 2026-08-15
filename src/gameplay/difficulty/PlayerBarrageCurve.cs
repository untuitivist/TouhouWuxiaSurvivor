namespace TouhouWuxiaSurvivor.Gameplay.Difficulty;

using TouhouWuxiaSurvivor.Gameplay.Pacing;

/// <summary>
/// 根据动态难度时间、齐射序号和弹丸池余量规划有效弹幕，保证所有弹型都围绕可命中目标展开。
/// </summary>
public static class PlayerBarrageCurve
{
    public const int MaximumProjectilesPerVolley = 7;
    public const int ProjectileSoftLimit = 1600;
    public const double SaturatedRetryIntervalSeconds = 0.05;

    /// <summary>
    /// 生成一次齐射计划：开局保持单发，随后进入三、五发并在后期交错扇形与目标收束阵。
    /// </summary>
    public static PlayerBarrageSnapshot EvaluateSeconds(
        double elapsedSeconds,
        bool spiralActive,
        long volleySequence,
        int activeProjectileCount,
        int bonusProjectiles = 0)
    {
        double minutes = EndlessDifficultyCurve.NormalizeMinutes(elapsedSeconds);
        int normalizedBonus = Math.Clamp(bonusProjectiles, 0, 2);
        int requestedCount = GetRequestedProjectileCount(
            minutes, spiralActive, normalizedBonus);
        PlayerBarrageMode mode = GetMode(minutes, spiralActive, volleySequence);
        int allowedCount = ApplyProjectileBudget(requestedCount, mode, activeProjectileCount);
        double angularStep = GetAngularStep(requestedCount, mode);
        double rotation = GetRotation(mode, volleySequence, angularStep);
        const bool requiresTarget = true;
        double retry = allowedCount == 0 ? SaturatedRetryIntervalSeconds : 0.0;
        double stageBudget = GetStageDamageBudget(minutes * 60.0);
        double damageBudget = stageBudget * (1.0 + normalizedBonus * 0.125 +
            (spiralActive ? 0.20 : 0.0));
        return new PlayerBarrageSnapshot(minutes, mode, requestedCount, allowedCount,
            angularStep, rotation, requiresTarget, retry, damageBudget);
    }

    /// <summary>
    /// 按阶段返回奇数扇形阶梯；额外弹特化可把五发扩展到七发，螺旋强化至少保留正反双发。
    /// </summary>
    private static int GetRequestedProjectileCount(
        double minutes,
        bool spiralActive,
        int bonusProjectiles)
    {
        double elapsedSeconds = minutes * 60.0;
        int timedCount = elapsedSeconds switch
        {
            < RunPacingTimeline.RisingSeconds => 1,
            < RunPacingTimeline.BarrageSeconds => 3,
            _ => 5,
        };
        int builtCount = Math.Min(MaximumProjectilesPerVolley, timedCount + bonusProjectiles);
        return spiralActive ? Math.Max(2, builtCount) : builtCount;
    }

    /// <summary>
    /// 普通攻击由单发过渡到交错扇形，危机阶段每隔一轮插入收束阵；螺旋强化始终使用收束阵。
    /// </summary>
    private static PlayerBarrageMode GetMode(
        double minutes,
        bool spiralActive,
        long volleySequence)
    {
        if (spiralActive)
        {
            return PlayerBarrageMode.ConvergingOrbit;
        }

        double elapsedSeconds = minutes * 60.0;
        if (elapsedSeconds < RunPacingTimeline.RisingSeconds)
        {
            return PlayerBarrageMode.TargetedSingle;
        }

        return elapsedSeconds >= RunPacingTimeline.CrisisSeconds &&
            (volleySequence & 1L) == 1L
            ? PlayerBarrageMode.ConvergingOrbit
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

    /// <summary>
    /// 阶段进阶同步提高整轮有效输出，使最终战前形成割草能力；构筑倍率仍决定路线间差异。
    /// </summary>
    private static double GetStageDamageBudget(double difficultySeconds) => difficultySeconds switch
    {
        < RunPacingTimeline.RisingSeconds => 1.00,
        < RunPacingTimeline.SwarmingSeconds => 1.20,
        < RunPacingTimeline.BarrageSeconds => 1.50,
        < RunPacingTimeline.CrisisSeconds => 1.85,
        < RunPacingTimeline.FinalEncounterSeconds => 2.25,
        _ => 2.50,
    };
}
