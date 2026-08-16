namespace TouhouWuxiaSurvivor.Gameplay.Pacing;

/// <summary>
/// 将动态阶段提供的名义难度秒数投影为连续上升的总刷新率和四档敌人占比。
/// </summary>
public static class EnemyPressureCurve
{
    /// <summary>
    /// 在相邻三十秒策划档之间线性插值；决战后仅总刷新率缓慢无界增长，配比保持最终档。
    /// </summary>
    public static EnemyPressureSnapshot Evaluate(double difficultySeconds)
    {
        double seconds = NormalizeSeconds(difficultySeconds);
        double position = seconds / RunPacingTimeline.EvaluationWindowSeconds;
        int finiteCount = RunPacingTimeline.AdaptiveRules.Count;
        if (position >= finiteCount)
        {
            double endlessGears = position - finiteCount;
            RunPhaseRule final = RunPacingTimeline.FinalEncounterRule;
            return new EnemyPressureSnapshot(
                finiteCount,
                final.SpawnRatePerSecond + endlessGears * 0.12,
                final.TierMix);
        }

        int lowerIndex = Math.Clamp((int)Math.Floor(position), 0, finiteCount - 1);
        int upperIndex = Math.Min(finiteCount, lowerIndex + 1);
        RunPhaseRule lower = RunPacingTimeline.AdaptiveRules[lowerIndex];
        RunPhaseRule upper = upperIndex == finiteCount
            ? RunPacingTimeline.FinalEncounterRule
            : RunPacingTimeline.AdaptiveRules[upperIndex];
        double t = Math.Clamp(position - lowerIndex, 0.0, 1.0);
        return new EnemyPressureSnapshot(
            lowerIndex,
            lower.SpawnRatePerSecond +
                (upper.SpawnRatePerSecond - lower.SpawnRatePerSecond) * t,
            EnemyTierMix.Lerp(lower.TierMix, upper.TierMix, t));
    }

    /// <summary>将非法、负数与正无穷输入整理成可计算的有限非负秒数。</summary>
    private static double NormalizeSeconds(double seconds)
    {
        if (double.IsNaN(seconds) || seconds <= 0.0)
        {
            return 0.0;
        }

        return double.IsPositiveInfinity(seconds)
            ? double.MaxValue / 1_000_000.0
            : seconds;
    }
}
