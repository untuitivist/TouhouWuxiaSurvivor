using TouhouWuxiaSurvivor.Gameplay.Difficulty;
using TouhouWuxiaSurvivor.Gameplay.Spawning;

namespace TouhouWuxiaSurvivor.Gameplay.Balance;

/// <summary>
/// 以正式批次、间隔和动态存活上限推进策划敌群，不引入运行时不存在的刷怪利用率或额外压力。
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
    /// 在一个有限时间窗内先用现有敌人释放容量，再接纳正式计划生成量，并把击破限制在可用敌人数内。
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
        double initialDefeatRate = Math.Min(capacity, AliveCount / duration);
        double freeRoomRate = Math.Max(0.0,
            (difficulty.AliveLimit - AliveCount) / duration);
        double acceptedRate = Math.Min(scheduledRate,
            freeRoomRate + initialDefeatRate);
        double availableRate = AliveCount / duration + acceptedRate;
        double defeatRate = Math.Min(capacity, availableRate);
        AliveCount = Math.Clamp(
            AliveCount + (acceptedRate - defeatRate) * duration,
            0.0,
            difficulty.AliveLimit);
        LastAcceptedSpawnsPerSecond = acceptedRate;
        return new BalanceSpawnFlowSnapshot(scheduledRate, acceptedRate,
            defeatRate, AliveCount, difficulty.AliveLimit);
    }
}
