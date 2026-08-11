using TouhouWuxiaSurvivor.Gameplay.Difficulty;

namespace TouhouWuxiaSurvivor.Gameplay.Spawning;

/// <summary>
/// 集中定义生存时间对应的刷怪批量、间隔和动态存活上限。
/// </summary>
public static class EnemySpawnPacing
{
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
    /// 返回不会封顶的每秒威胁预算，供生命、伤害、奖励或精英生成在实体数量封顶后继续提高强度。
    /// </summary>
    public static double GetSpawnBudgetPerSecond(double elapsedSeconds) =>
        EndlessDifficultyCurve.EvaluateSeconds(elapsedSeconds, int.MaxValue).SpawnBudgetPerSecond;
}
