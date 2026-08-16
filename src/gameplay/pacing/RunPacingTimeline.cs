namespace TouhouWuxiaSurvivor.Gameplay.Pacing;

/// <summary>
/// 集中定义七个三十秒战力验证档位；只有击破本窗实际刷新量才会进入下一档。
/// </summary>
public static class RunPacingTimeline
{
    public const double EvaluationWindowSeconds = 30.0;
    public const double RisingSeconds = 30.0;
    public const double SwarmingSeconds = 60.0;
    public const double BarrageSeconds = 90.0;
    public const double CrisisSeconds = 120.0;
    public const double DominanceSeconds = 150.0;
    public const double BreakthroughSeconds = 180.0;
    public const double FinalEncounterSeconds = 210.0;
    public const double TargetClearSeconds = 300.0;

    public static IReadOnlyList<double> MilestoneSeconds { get; } =
        [RisingSeconds, SwarmingSeconds, BarrageSeconds, CrisisSeconds,
            DominanceSeconds, BreakthroughSeconds];

    public static IReadOnlyList<RunPhaseDefinition> StructuredPhases { get; } =
    [
        new(RunPhaseId.Opening, "异变初兆", "普通敌群开始活动",
            0.0, RisingSeconds),
        new(RunPhaseId.Rising, "妖气初现", "强敌开始混入敌群",
            RisingSeconds, SwarmingSeconds),
        new(RunPhaseId.Swarming, "百鬼渐行", "精锐敌人开始出现",
            SwarmingSeconds, BarrageSeconds),
        new(RunPhaseId.Barrage, "弹幕成形", "敌群职责形成组合",
            BarrageSeconds, CrisisSeconds),
        new(RunPhaseId.Crisis, "结界震荡", "头目级敌人开始混入",
            CrisisSeconds, DominanceSeconds),
        new(RunPhaseId.Dominance, "百鬼压境", "高强度敌群持续增援",
            DominanceSeconds, BreakthroughSeconds),
        new(RunPhaseId.Breakthrough, "异变临界", "击穿最后敌阵以引出核心",
            BreakthroughSeconds, FinalEncounterSeconds),
    ];

    public static IReadOnlyList<RunPhaseRule> AdaptiveRules { get; } =
    [
        new(RunPhaseId.Opening, 2.40, new EnemyTierMix(100, 0, 0, 0)),
        new(RunPhaseId.Rising, 3.15, new EnemyTierMix(92, 8, 0, 0)),
        new(RunPhaseId.Swarming, 4.05, new EnemyTierMix(84, 14, 2, 0)),
        new(RunPhaseId.Barrage, 5.10, new EnemyTierMix(75, 20, 5, 0)),
        new(RunPhaseId.Crisis, 6.15, new EnemyTierMix(66, 23, 9, 2)),
        new(RunPhaseId.Dominance, 7.20, new EnemyTierMix(57, 26, 13, 4)),
        new(RunPhaseId.Breakthrough, 8.25, new EnemyTierMix(48, 28, 18, 6)),
    ];

    public static RunPhaseRule FinalEncounterRule { get; } = new(
        RunPhaseId.FinalEncounter, 9.30, new EnemyTierMix(40, 30, 22, 8));

    /// <summary>
    /// 把名义难度时间投影为唯一档位；正式运行时由 AdaptiveRunPacingState 决定是否前进。
    /// </summary>
    public static RunPacingSnapshot Evaluate(double elapsedSeconds, bool isEndless = false)
    {
        double elapsed = NormalizeElapsed(elapsedSeconds);
        if (isEndless)
        {
            return CreateTerminalSnapshot(RunPhaseId.Endless, "无尽游历",
                "敌群与角色Boss将持续增强", elapsed, true);
        }

        if (elapsed >= FinalEncounterSeconds)
        {
            return CreateTerminalSnapshot(RunPhaseId.FinalEncounter, "异变核心",
                "击破角色Boss以平定异变", elapsed, false);
        }

        RunPhaseDefinition phase = StructuredPhases.Last(item => elapsed >= item.StartSeconds);
        int phaseIndex = StructuredPhases.TakeWhile(item => item.Id != phase.Id).Count();
        string nextName = phaseIndex + 1 < StructuredPhases.Count
            ? StructuredPhases[phaseIndex + 1].DisplayName
            : "异变核心";
        double duration = phase.EndSeconds - phase.StartSeconds;
        return new RunPacingSnapshot(
            phase.Id,
            phase.DisplayName,
            phase.CueText,
            nextName,
            elapsed,
            phase.StartSeconds,
            phase.EndSeconds,
            Math.Clamp(elapsed / FinalEncounterSeconds, 0.0, 1.0),
            Math.Clamp((elapsed - phase.StartSeconds) / duration, 0.0, 1.0),
            Math.Max(0.0, phase.EndSeconds - elapsed),
            false,
            false,
            elapsed,
            Math.Clamp((elapsed - phase.StartSeconds) / duration, 0.0, 1.0),
            true);
    }

    /// <summary>
    /// 为已经填满有限进度条的最终遭遇或无尽状态建立快照，避免用无穷结束时间污染UI。
    /// </summary>
    internal static RunPacingSnapshot CreateTerminalSnapshot(
        RunPhaseId id,
        string name,
        string cue,
        double elapsed,
        bool isEndless) => new(
            id,
            name,
            cue,
            isEndless ? "无尽" : "击破Boss",
            elapsed,
            FinalEncounterSeconds,
            FinalEncounterSeconds,
            1.0,
            1.0,
            0.0,
            !isEndless,
            isEndless,
            FinalEncounterSeconds,
            1.0,
            false,
            AdaptiveRules.Count);

    /// <summary>
    /// 将非法、负数与正无穷运行时间整理为可显示的有限非负秒数。
    /// </summary>
    private static double NormalizeElapsed(double elapsedSeconds)
    {
        if (double.IsNaN(elapsedSeconds) || elapsedSeconds <= 0.0)
        {
            return 0.0;
        }

        return double.IsPositiveInfinity(elapsedSeconds)
            ? double.MaxValue
            : elapsedSeconds;
    }
}
