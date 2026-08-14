namespace TouhouWuxiaSurvivor.Gameplay.Difficulty;

using TouhouWuxiaSurvivor.Gameplay.Pacing;

/// <summary>
/// 以无状态纯函数生成无尽难度；战斗倍率持续增长，刷新率与实体参数按性能边界允许封顶。
/// </summary>
public static class EndlessDifficultyCurve
{
    public const int MaximumSpawnBatchSize = 24;
    public const double MinimumSpawnIntervalSeconds = 0.10;
    public const double MaximumEnemySpeedMultiplier = 1.75;
    private const int OpeningAliveLimit = 36;
    private const double AliveGrowthPerMinute = 10.0;

    /// <summary>
    /// 把非负秒数转换为不可变难度快照；负数和非数按开局处理，正无穷按最大有限时间处理。
    /// </summary>
    public static EndlessDifficultySnapshot EvaluateSeconds(double elapsedSeconds, int hardAliveLimit)
    {
        double minutes = NormalizeMinutes(elapsedSeconds);
        double logarithm = Math.Log(1.0 + minutes);
        int batchSize = CalculateBatchSize(minutes);
        double interval = CalculateSpawnInterval(minutes);
        int aliveLimit = CalculateAliveLimit(minutes, hardAliveLimit);
        double scheduledSpawnsPerSecond = batchSize / interval;
        double intensity = 1.0 + 0.25 * minutes + 0.08 * logarithm * logarithm;
        double health = 1.0 + 0.12 * minutes + 0.04 * logarithm * logarithm;
        double damage = 1.0 + 0.055 * minutes + 0.02 * logarithm * logarithm;
        double reward = 1.0 + 0.018 * minutes + 0.01 * logarithm * logarithm;
        double speed = Math.Min(MaximumEnemySpeedMultiplier, 1.0 + 0.12 * logarithm);
        return new EndlessDifficultySnapshot(minutes, intensity, scheduledSpawnsPerSecond, batchSize,
            interval, aliveLimit, health, damage, reward, speed);
    }

    /// <summary>
    /// 将外部时间整理为有限非负分钟，避免暂停误差、非法存档或极端输入破坏曲线单调性。
    /// </summary>
    public static double NormalizeMinutes(double elapsedSeconds)
    {
        if (double.IsNaN(elapsedSeconds) || elapsedSeconds <= 0.0)
        {
            return 0.0;
        }

        return double.IsPositiveInfinity(elapsedSeconds)
            ? double.MaxValue / 60.0
            : elapsedSeconds / 60.0;
    }

    /// <summary>
    /// 有限流程在五个阶段逐步增加同批敌人；进入无尽后每三分钟增长并在二十四只封顶。
    /// </summary>
    private static int CalculateBatchSize(double minutes)
    {
        double seconds = minutes * 60.0;
        if (seconds < RunPacingTimeline.RisingSeconds) return 1;
        if (seconds < RunPacingTimeline.SwarmingSeconds) return 2;
        if (seconds < RunPacingTimeline.BarrageSeconds) return 3;
        if (seconds < RunPacingTimeline.CrisisSeconds) return 4;
        if (seconds < RunPacingTimeline.FinalEncounterSeconds) return 5;
        double endlessMinutes = minutes - RunPacingTimeline.FinalEncounterSeconds / 60.0;
        double tiersToMaximum = MaximumSpawnBatchSize - 6;
        return endlessMinutes / 3.0 >= tiersToMaximum
            ? MaximumSpawnBatchSize
            : 6 + (int)Math.Floor(endlessMinutes / 3.0);
    }

    /// <summary>
    /// 用连续双曲线缩短刷新间隔，并用固定下限阻止极长局触发同帧刷怪风暴。
    /// </summary>
    private static double CalculateSpawnInterval(double minutes) =>
        Math.Max(MinimumSpawnIntervalSeconds, 0.10 + 0.75 / (1.0 + minutes / 6.0));

    /// <summary>
    /// 每分钟提高期望存活数，但最终严格服从场景硬上限；非法硬上限被整理为至少一只。
    /// </summary>
    private static int CalculateAliveLimit(double minutes, int hardAliveLimit)
    {
        int hardLimit = Math.Max(1, hardAliveLimit);
        if (hardLimit <= OpeningAliveLimit)
        {
            return hardLimit;
        }

        double minutesToHardLimit = (hardLimit - OpeningAliveLimit) / AliveGrowthPerMinute;
        if (minutes >= minutesToHardLimit)
        {
            return hardLimit;
        }

        int desired = OpeningAliveLimit +
            (int)Math.Floor(minutes) * (int)AliveGrowthPerMinute;
        return Math.Min(hardLimit, desired);
    }
}
