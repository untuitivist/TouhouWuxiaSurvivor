using TouhouWuxiaSurvivor.Gameplay.Difficulty;

namespace TouhouWuxiaSurvivor.Gameplay.Spawning;

/// <summary>
/// 集中定义生存时间对应的刷怪批量、间隔和动态存活上限。
/// </summary>
public static class EnemySpawnPacing
{
    /// <summary>正式世界开局预置的敌人数，也是离线供给投影的唯一默认值。</summary>
    public const int DefaultInitialSpawnCount = 12;
    /// <summary>正式世界允许同时存活的敌人数上限，也是离线供给投影的唯一默认值。</summary>
    public const int DefaultAliveHardLimit = 140;

    /// <summary>
    /// 从无尽难度快照读取批量；早期仍在 120、240、420 秒跳档，长局最终受单批性能上限保护。
    /// </summary>
    public static int GetBatchSize(double elapsedSeconds) =>
        EndlessDifficultyCurve.EvaluateSeconds(elapsedSeconds, int.MaxValue).SpawnBatchSize;

    /// <summary>
    /// 从无尽难度快照读取连续下降的刷新间隔，并保留防止同帧刷怪风暴的性能下限。
    /// </summary>
    public static double GetSpawnInterval(double elapsedSeconds) =>
        EndlessDifficultyCurve.EvaluateSeconds(elapsedSeconds, int.MaxValue).SpawnIntervalSeconds;

    /// <summary>
    /// 从三十六只开始每分钟增加十只，最终严格服从调用场景提供的最大存活硬上限。
    /// </summary>
    public static int GetAliveLimit(double elapsedSeconds, int hardLimit) =>
        EndlessDifficultyCurve.EvaluateSeconds(elapsedSeconds, hardLimit).AliveLimit;

    /// <summary>
    /// 返回正式批次和刷新间隔对应的理论生成率；该值只描述生成器供给，不混入额外虚构压力。
    /// </summary>
    public static double GetScheduledSpawnsPerSecond(double elapsedSeconds)
    {
        EndlessDifficultySnapshot snapshot = EndlessDifficultyCurve.EvaluateSeconds(
            elapsedSeconds, int.MaxValue);
        return snapshot.SpawnBatchSize / snapshot.SpawnIntervalSeconds;
    }
}
