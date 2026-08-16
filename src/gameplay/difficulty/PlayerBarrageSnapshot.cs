namespace TouhouWuxiaSurvivor.Gameplay.Difficulty;

/// <summary>
/// 保存普通弹与自机中心弹幕的一轮计划，同时记录性能预算前后的数量和独立阵形。
/// </summary>
public readonly record struct PlayerBarrageSnapshot(
    PlayerOrdinaryShotMode OrdinaryMode,
    PlayerBarrageMode BarrageMode,
    int RequestedOrdinaryProjectileCount,
    int OrdinaryProjectileCount,
    int RequestedBarrageProjectileCount,
    int BarrageProjectileCount,
    int BarrageSpiralArmCount,
    double OrdinaryAngularStepRadians,
    double BarrageRotationRadians,
    bool RequiresTarget,
    double RetryIntervalSeconds)
{
    public int RequestedProjectileCount =>
        RequestedOrdinaryProjectileCount + RequestedBarrageProjectileCount;
    public int ProjectileCount => OrdinaryProjectileCount + BarrageProjectileCount;

    /// <summary>
    /// 没有目标时移除普通弹而保留自机中心弹幕，使辐射和螺旋不会被索敌状态错误阻断。
    /// </summary>
    public PlayerBarrageSnapshot WithoutOrdinaryProjectiles() => this with
    {
        OrdinaryProjectileCount = 0,
        RequiresTarget = false,
    };
}
