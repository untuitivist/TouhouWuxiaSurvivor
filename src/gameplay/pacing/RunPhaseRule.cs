namespace TouhouWuxiaSurvivor.Gameplay.Pacing;

/// <summary>
/// 定义一个有限阶段的最短展示、最长兜底与持续压制门槛，使动态进阶可审计且不会瞬时跳档。
/// </summary>
public sealed class RunPhaseRule
{
    public RunPhaseId PhaseId { get; }
    public double MinimumDurationSeconds { get; }
    public double MaximumDurationSeconds { get; }
    public double RequiredKillsPerSecond { get; }
    public double MaximumAliveRatio { get; }
    public double RequiredDominanceSeconds { get; }

    /// <summary>
    /// 建立完整规则；所有比率、持续时间和击破要求必须有限且位于可解释范围内。
    /// </summary>
    public RunPhaseRule(
        RunPhaseId phaseId,
        double minimumDurationSeconds,
        double maximumDurationSeconds,
        double requiredKillsPerSecond,
        double maximumAliveRatio,
        double requiredDominanceSeconds)
    {
        if (!double.IsFinite(minimumDurationSeconds) || minimumDurationSeconds <= 0.0 ||
            !double.IsFinite(maximumDurationSeconds) ||
            maximumDurationSeconds < minimumDurationSeconds ||
            !double.IsFinite(requiredKillsPerSecond) || requiredKillsPerSecond <= 0.0 ||
            !double.IsFinite(maximumAliveRatio) || maximumAliveRatio <= 0.0 ||
            maximumAliveRatio >= 1.0 ||
            !double.IsFinite(requiredDominanceSeconds) || requiredDominanceSeconds <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumDurationSeconds),
                "Adaptive phase rule values must form finite positive bounds.");
        }

        PhaseId = phaseId;
        MinimumDurationSeconds = minimumDurationSeconds;
        MaximumDurationSeconds = maximumDurationSeconds;
        RequiredKillsPerSecond = requiredKillsPerSecond;
        MaximumAliveRatio = maximumAliveRatio;
        RequiredDominanceSeconds = requiredDominanceSeconds;
    }
}
