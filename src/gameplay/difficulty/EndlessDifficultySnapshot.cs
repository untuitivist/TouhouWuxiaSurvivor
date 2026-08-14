namespace TouhouWuxiaSurvivor.Gameplay.Difficulty;

/// <summary>
/// 保存指定生存时间的完整难度快照；无界倍率与有界实体参数明确分离，调用方无需重复推导曲线。
/// </summary>
public readonly record struct EndlessDifficultySnapshot(
    double ElapsedMinutes,
    double Intensity,
    double ScheduledSpawnsPerSecond,
    int SpawnBatchSize,
    double SpawnIntervalSeconds,
    int AliveLimit,
    double EnemyHealthMultiplier,
    double EnemyDamageMultiplier,
    double RewardMultiplier,
    double EnemySpeedMultiplier);
