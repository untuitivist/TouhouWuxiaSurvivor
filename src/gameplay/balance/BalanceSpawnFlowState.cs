using TouhouWuxiaSurvivor.Gameplay.Difficulty;
using TouhouWuxiaSurvivor.Gameplay.Spawning;

namespace TouhouWuxiaSurvivor.Gameplay.Balance;

/// <summary>
/// 以正式连续刷新率推进策划敌群；所有计划生成均被接纳，不虚构运行时不存在的存活软上限。
/// </summary>
public sealed class BalanceSpawnFlowState
{
    public double AliveCount { get; private set; }
    public double LastAcceptedSpawnsPerSecond { get; private set; }

    /// <summary>从正式场景默认首批数量建立敌群，允许测试传入其他非负初始值。</summary>
    public BalanceSpawnFlowState(
        int initialAlive = EnemySpawnPacing.DefaultInitialSpawnCount) =>
        AliveCount = Math.Max(0, initialAlive);

    /// <summary>
    /// 在有限时间窗内完整接纳计划生成量，并把击破限制在现有敌群与本窗新增敌人的总量内。
    /// </summary>
    public BalanceSpawnFlowSnapshot Advance(
        EndlessDifficultySnapshot difficulty,
        double defeatCapacityPerSecond,
        double durationSeconds = 1.0)
    {
        double duration = double.IsFinite(durationSeconds)
            ? Math.Max(0.001, durationSeconds)
            : 1.0;
        double capacity = double.IsFinite(defeatCapacityPerSecond)
            ? Math.Max(0.0, defeatCapacityPerSecond)
            : 0.0;
        double scheduledRate = difficulty.ScheduledSpawnsPerSecond;
        double acceptedRate = scheduledRate;
        double availableRate = AliveCount / duration + acceptedRate;
        double defeatRate = Math.Min(capacity, availableRate);
        AliveCount = Math.Max(0.0,
            AliveCount + (acceptedRate - defeatRate) * duration);
        LastAcceptedSpawnsPerSecond = acceptedRate;
        return new BalanceSpawnFlowSnapshot(scheduledRate, acceptedRate,
            defeatRate, AliveCount);
    }
}
