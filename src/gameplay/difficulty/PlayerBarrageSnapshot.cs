namespace TouhouWuxiaSurvivor.Gameplay.Difficulty;

/// <summary>
/// 保存一次自动齐射的纯数据计划，同时记录理想弹数和受性能余量约束后的实际弹数。
/// </summary>
public readonly record struct PlayerBarrageSnapshot(
    double ElapsedMinutes,
    PlayerBarrageMode Mode,
    int RequestedProjectileCount,
    int ProjectileCount,
    double AngularStepRadians,
    double RotationRadians,
    bool RequiresTarget,
    double RetryIntervalSeconds,
    double VolleyDamageBudget);
