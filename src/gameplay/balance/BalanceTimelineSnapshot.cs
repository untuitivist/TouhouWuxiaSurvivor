namespace TouhouWuxiaSurvivor.Gameplay.Balance;

/// <summary>
/// 保存一个指定分钟节点的完整策划快照，使测试、调试输出与后续表格导出使用同一结果。
/// </summary>
public sealed record BalanceTimelineSnapshot(
    int ElapsedMinutes,
    BalanceBuildKind BuildKind,
    int RunLevel,
    long TotalExperience,
    double WeaponDps,
    double SpellDps,
    double TotalDps,
    double ReadinessScore,
    double EnemyHealthMultiplier,
    double EnemyDamageMultiplier,
    double RewardMultiplier,
    double ScheduledSpawnsPerSecond,
    double EffectiveKillsPerSecond,
    double EnemyPressure,
    double PowerToPressureRatio,
    double SpiritEconomyMultiplier,
    int OffensiveSpellCount,
    int SupportSpellCount,
    int EndlessRankCount,
    int EnabledSpellCount,
    int OffensiveSlotCapacity,
    int SupportSlotCapacity,
    double SpellCapacityBudget)
{
    /// <summary>
    /// 生成紧凑而稳定的中文单行报告，方便在持续测试输出中直接比较不同时间与路线。
    /// </summary>
    public string FormatReport() =>
        $"{ElapsedMinutes,3}分 {GetBuildName(),-4} 等级{RunLevel,4} " +
        $"普攻{WeaponDps,8:F1} 奥义{SpellDps,8:F1} 总伤{TotalDps,8:F1}/秒 " +
        $"击破{EffectiveKillsPerSecond,6:F2}/秒 " +
        $"压力{EnemyPressure,8:F1} 准备度{ReadinessScore,6:F2} " +
        $"奥义{OffensiveSpellCount}+{SupportSpellCount} 无尽{EndlessRankCount}";

    /// <summary>把内部构筑枚举转换为策划日志使用的中文短名。</summary>
    private string GetBuildName() => BuildKind switch
    {
        BalanceBuildKind.Baseline => "基础",
        BalanceBuildKind.Assault => "强攻",
        BalanceBuildKind.Rapid => "速射",
        BalanceBuildKind.Utility => "效用",
        _ => BuildKind.ToString(),
    };
}
