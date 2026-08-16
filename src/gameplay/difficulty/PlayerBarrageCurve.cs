namespace TouhouWuxiaSurvivor.Gameplay.Difficulty;

/// <summary>
/// 根据构筑与弹丸池余量规划定向普通弹和自机中心弹幕；压力阶段绝不赠送火力。
/// </summary>
public static class PlayerBarrageCurve
{
    public const int MaximumOrdinaryProjectilesPerVolley = 6;
    public const int MaximumBarrageProjectilesPerVolley = 12;
    public const int MaximumProjectilesPerVolley =
        MaximumOrdinaryProjectilesPerVolley + MaximumBarrageProjectilesPerVolley;
    public const int ProjectileSoftLimit = 1600;
    public const double SaturatedRetryIntervalSeconds = 0.05;

    /// <summary>
    /// 生成一次构筑齐射；普通弹至少一发，弹幕数量、螺旋形态和旋转相位分别输入。
    /// </summary>
    public static PlayerBarrageSnapshot Evaluate(
        bool convergingOrdinaryActive,
        int barrageSpiralArmCount,
        long volleySequence,
        int activeProjectileCount,
        int ordinaryProjectileBonus = 0,
        int barrageProjectileBonus = 0)
    {
        int requestedOrdinary = Math.Clamp(1 + ordinaryProjectileBonus,
            1, MaximumOrdinaryProjectilesPerVolley);
        int requestedBarrage = Math.Clamp(barrageProjectileBonus,
            0, MaximumBarrageProjectilesPerVolley);
        (int ordinary, int barrage) = ApplyProjectileBudget(
            requestedOrdinary, requestedBarrage, activeProjectileCount);
        int spiralArms = requestedBarrage <= 0 || barrageSpiralArmCount < 2
            ? 0
            : Math.Clamp(barrageSpiralArmCount, 2, 4);
        PlayerBarrageMode barrageMode = requestedBarrage <= 0
            ? PlayerBarrageMode.None
            : spiralArms >= 2 ? PlayerBarrageMode.Spiral : PlayerBarrageMode.Radial;
        double retry = ordinary + barrage == 0 ? SaturatedRetryIntervalSeconds : 0.0;
        return new PlayerBarrageSnapshot(
            convergingOrdinaryActive
                ? PlayerOrdinaryShotMode.ConvergingFormation
                : PlayerOrdinaryShotMode.PredictiveFan,
            barrageMode,
            requestedOrdinary, ordinary, requestedBarrage, barrage,
            spiralArms, GetOrdinaryAngularStep(requestedOrdinary),
            GetBarrageRotation(volleySequence, barrageMode),
            ordinary > 0 && barrage <= 0, retry);
    }

    /// <summary>兼容旧纯函数调用形状；时间参数被明确忽略，测试可据此锁定阶段不会赠送火力。</summary>
    public static PlayerBarrageSnapshot EvaluateSeconds(
        double ignoredElapsedSeconds,
        bool convergingOrdinaryActive,
        long volleySequence,
        int activeProjectileCount,
        int barrageProjectileBonus = 0,
        int ordinaryProjectileBonus = 0,
        int barrageSpiralArmCount = 0) => Evaluate(
            convergingOrdinaryActive, barrageSpiralArmCount, volleySequence,
            activeProjectileCount, ordinaryProjectileBonus, barrageProjectileBonus);

    /// <summary>
    /// 把单轮弹数压入软上限余量；优先保留负责锁敌的普通弹，再保留中心弹幕。
    /// </summary>
    private static (int Ordinary, int Barrage) ApplyProjectileBudget(
        int requestedOrdinary,
        int requestedBarrage,
        int activeProjectileCount)
    {
        int activeCount = Math.Max(0, activeProjectileCount);
        int available = Math.Max(0, ProjectileSoftLimit - activeCount);
        int ordinary = Math.Min(requestedOrdinary, available);
        int remaining = Math.Max(0, available - ordinary);
        int barrage = Math.Min(requestedBarrage, remaining);
        return (ordinary, barrage);
    }

    /// <summary>为定向普通弹提供随数量收紧的相邻夹角，单发保持严格预判直射。</summary>
    private static double GetOrdinaryAngularStep(int requestedOrdinary) =>
        requestedOrdinary switch
        {
            2 => Math.PI / 18.0,
            3 => Math.PI / 22.0,
            4 => Math.PI / 26.0,
            5 => Math.PI / 30.0,
            6 => Math.PI / 34.0,
            _ => 0.0,
        };

    /// <summary>按齐射序号旋转中心弹幕；螺旋比辐射转得更慢以保持可读的连续臂线。</summary>
    private static double GetBarrageRotation(long volleySequence, PlayerBarrageMode mode) =>
        mode switch
        {
            PlayerBarrageMode.Spiral => volleySequence * Math.PI / 15.0,
            PlayerBarrageMode.Radial => volleySequence * Math.PI / 10.0,
            _ => 0.0,
        };
}
