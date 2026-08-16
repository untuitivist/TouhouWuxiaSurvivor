using TouhouWuxiaSurvivor.Gameplay.Pacing;

namespace TouhouWuxiaSurvivor.Gameplay.Spawning;

/// <summary>
/// 集中暴露动态难度对应的连续刷新率与四档强度配比，不再提供批次或存活软上限。
/// </summary>
public static class EnemySpawnPacing
{
    /// <summary>正式世界开局预置的敌人数，也是离线供给投影的唯一默认值。</summary>
    public const int DefaultInitialSpawnCount = 12;
    /// <summary>
    /// 返回正式批次和刷新间隔对应的理论生成率；该值只描述生成器供给，不混入额外虚构压力。
    /// </summary>
    public static double GetScheduledSpawnsPerSecond(double elapsedSeconds) =>
        EnemyPressureCurve.Evaluate(elapsedSeconds).SpawnRatePerSecond;

    /// <summary>返回当前连续压力快照，刷怪器据此调度强度档而不重新推导比例。</summary>
    public static EnemyPressureSnapshot GetPressure(double difficultySeconds) =>
        EnemyPressureCurve.Evaluate(difficultySeconds);
}
