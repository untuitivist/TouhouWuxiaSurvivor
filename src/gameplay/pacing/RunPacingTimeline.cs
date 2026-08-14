namespace TouhouWuxiaSurvivor.Gameplay.Pacing;

/// <summary>
/// 集中定义约五分钟本体流程及其五个有限阶段，禁止HUD、刷怪和弹幕各自维护里程碑。
/// </summary>
public static class RunPacingTimeline
{
    public const double RisingSeconds = 45.0;
    public const double SwarmingSeconds = 90.0;
    public const double BarrageSeconds = 150.0;
    public const double CrisisSeconds = 210.0;
    public const double FinalEncounterSeconds = 270.0;
    public const double TargetClearSeconds = 300.0;

    public static IReadOnlyList<double> MilestoneSeconds { get; } =
        [RisingSeconds, SwarmingSeconds, BarrageSeconds, CrisisSeconds];

    public static IReadOnlyList<RunPhaseDefinition> StructuredPhases { get; } =
    [
        new(RunPhaseId.Opening, "异变初兆", "追击敌群开始活动",
            0.0, RisingSeconds),
        new(RunPhaseId.Rising, "妖气初现", "突进敌群加入战场",
            RisingSeconds, SwarmingSeconds),
        new(RunPhaseId.Swarming, "百鬼渐行", "地区敌群开始混编",
            SwarmingSeconds, BarrageSeconds),
        new(RunPhaseId.Barrage, "弹幕成形", "远程弹幕形成交叉火力",
            BarrageSeconds, CrisisSeconds),
        new(RunPhaseId.Crisis, "结界震荡", "大妖怪与密集弹幕逼近",
            CrisisSeconds, FinalEncounterSeconds),
    ];

    /// <summary>
    /// 把任意运行时间投影为唯一阶段；四分半进入决战，目标在五分钟左右完成结算。
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
            false);
    }

    /// <summary>
    /// 为已经填满有限进度条的最终遭遇或无尽状态建立快照，避免用无穷结束时间污染UI。
    /// </summary>
    private static RunPacingSnapshot CreateTerminalSnapshot(
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
            isEndless);

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
