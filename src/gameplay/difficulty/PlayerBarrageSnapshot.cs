namespace TouhouWuxiaSurvivor.Gameplay.Difficulty;

/// <summary>
/// 保存一次双通道自动齐射的形态计划，同时记录理想弹数和性能预算后的实际弹数。
/// </summary>
public readonly record struct PlayerBarrageSnapshot(
    PlayerBarrageMode Mode,
    int RequestedAimedProjectileCount,
    int AimedProjectileCount,
    int RequestedBarrageProjectileCount,
    int BarrageProjectileCount,
    double AngularStepRadians,
    bool RequiresTarget,
    double RetryIntervalSeconds)
{
    public int RequestedProjectileCount =>
        RequestedAimedProjectileCount + RequestedBarrageProjectileCount;
    public int ProjectileCount => AimedProjectileCount + BarrageProjectileCount;
}
