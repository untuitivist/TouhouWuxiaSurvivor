namespace TouhouWuxiaSurvivor.Gameplay.Pacing;

/// <summary>
/// 保存动态阶段判定所需的最小战斗遥测；不携带实体、节点或内容包引用。
/// </summary>
public readonly record struct RunCombatTelemetry(
    int AliveEnemies,
    int DefeatedEnemies,
    int AliveLimit,
    double ScheduledSpawnsPerSecond = 0.0)
{
    /// <summary>把外部计数整理为非负值，并保证存活上限至少为一。</summary>
    public RunCombatTelemetry Normalize() => new(
        Math.Max(0, AliveEnemies),
        Math.Max(0, DefeatedEnemies),
        Math.Max(1, AliveLimit),
        double.IsFinite(ScheduledSpawnsPerSecond)
            ? Math.Max(0.0, ScheduledSpawnsPerSecond)
            : 0.0);
}
