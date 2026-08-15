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
        double interval = CalculateSpawnInterval(minutes, batchSize);
        int aliveLimit = CalculateAliveLimit(minutes, hardAliveLimit);
        double scheduledSpawnsPerSecond = batchSize / interval;
        double intensity = 1.0 + 0.25 * minutes + 0.08 * logarithm * logarithm;
        double health = CalculateOrdinaryHealthMultiplier(minutes);
        double bossHealth = 1.0 + 0.12 * minutes + 0.04 * logarithm * logarithm;
        double damage = 1.0 + 0.055 * minutes + 0.02 * logarithm * logarithm;
        double reward = 1.0 + 0.018 * minutes + 0.01 * logarithm * logarithm;
        double speed = Math.Min(MaximumEnemySpeedMultiplier, 1.0 + 0.12 * logarithm);
        return new EndlessDifficultySnapshot(minutes, intensity, scheduledSpawnsPerSecond, batchSize,
            interval, aliveLimit, health, bossHealth, damage, reward, speed);
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
    /// 把大批次转换成可读波次；决战前留出割草验收窗，Boss期间维持低补充，无尽后再连续提高密度。
    /// </summary>
    private static double CalculateSpawnInterval(double minutes, int batchSize)
    {
        double seconds = minutes * 60.0;
        double targetRate = seconds switch
        {
            < RunPacingTimeline.RisingSeconds => 0.80,
            < RunPacingTimeline.SwarmingSeconds => 1.00,
            < RunPacingTimeline.BarrageSeconds => 1.20,
            < RunPacingTimeline.CrisisSeconds => 1.40,
            < RunPacingTimeline.FinalEncounterSeconds => 0.25,
            _ => 0.25 + Math.Max(0.0,
                minutes - RunPacingTimeline.FinalEncounterSeconds / 60.0) * 0.20,
        };
        return Math.Max(MinimumSpawnIntervalSeconds, batchSize / targetRate);
    }

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

    /// <summary>
    /// 有限流程让普通怪耐久缓慢增长，使构筑能在决战前形成割草优势；进入无尽后恢复无界增长。
    /// Boss 使用独立原曲线，不消费这个清场倍率。
    /// </summary>
    private static double CalculateOrdinaryHealthMultiplier(double minutes)
    {
        double finiteEnd = RunPacingTimeline.FinalEncounterSeconds / 60.0;
        double finiteMinutes = Math.Min(minutes, finiteEnd);
        double finiteLogarithm = Math.Log(1.0 + finiteMinutes);
        double multiplier = 1.0 + finiteMinutes * 0.03 +
            finiteLogarithm * finiteLogarithm * 0.015;
        double endlessMinutes = Math.Max(0.0, minutes - finiteEnd);
        if (endlessMinutes <= 0.0)
        {
            return multiplier;
        }

        double totalLogarithm = Math.Log(1.0 + minutes);
        return multiplier + endlessMinutes * 0.12 +
            Math.Max(0.0, totalLogarithm * totalLogarithm -
                finiteLogarithm * finiteLogarithm) * 0.04;
    }
}
