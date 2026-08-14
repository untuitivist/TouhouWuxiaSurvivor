namespace TouhouWuxiaSurvivor.Gameplay.Balance;

/// <summary>
/// 保存一次构筑投影产生的武器、奥义和综合预算，供时间轴模拟与契约测试共享。
/// </summary>
public readonly record struct BalanceCombatMetrics(
    double WeaponDps,
    double SpellDps,
    double TotalDps,
    double MoveSpeedMultiplier,
    double TargetRangeMultiplier,
    double SpiritAttractionMultiplier,
    double SpiritYieldMultiplier,
    double ReadinessScore,
    int OffensiveSpellCount,
    int SupportSpellCount,
    int EndlessRankCount);
