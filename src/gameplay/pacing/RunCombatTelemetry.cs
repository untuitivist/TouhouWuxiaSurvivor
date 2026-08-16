namespace TouhouWuxiaSurvivor.Gameplay.Pacing;

/// <summary>
/// 保存三十秒验证窗所需的累计普通敌人生成与击破数；不读取场上存量或理论刷新率。
/// </summary>
public readonly record struct RunCombatTelemetry(
    int SpawnedEnemies,
    int DefeatedEnemies)
{
    /// <summary>把外部累计计数整理为非负值，倒退计数由阶段状态按窗口基线再行保护。</summary>
    public RunCombatTelemetry Normalize() => new(
        Math.Max(0, SpawnedEnemies),
        Math.Max(0, DefeatedEnemies));
}
