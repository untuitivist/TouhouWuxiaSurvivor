using TouhouWuxiaSurvivor.Gameplay.Pacing;

namespace TouhouWuxiaSurvivor.Gameplay.Difficulty;

/// <summary>
/// 保存指定压力时间的刷新快照；同种怪物的基础属性永远不随该快照变化。
/// </summary>
public readonly record struct EndlessDifficultySnapshot(
    double ElapsedMinutes,
    double Intensity,
    double ScheduledSpawnsPerSecond,
    EnemyTierMix TierMix);
