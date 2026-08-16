using Godot;
using TouhouWuxiaSurvivor.Content;
using TouhouWuxiaSurvivor.Gameplay.Balance;
using TouhouWuxiaSurvivor.Gameplay.Difficulty;
using TouhouWuxiaSurvivor.Gameplay.Pacing;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 验证零至一百二十分钟策划预算有限、持续成长、路线横向有界且内容包不增加战力容量。
/// </summary>
public partial class BalanceTimelineContractTest : Node
{
    /// <summary>
    /// 运行四路线与内容容量契约，打印全部里程碑，并以明确退出码报告任何数值回归。
    /// </summary>
    public override void _Ready()
    {
        try
        {
            var simulator = new BalanceTimelineSimulator();
            var allContent = new ContentPackSelection(
                ContentPackCatalog.All.Select(pack => pack.Id));
            IReadOnlyDictionary<BalanceBuildKind, IReadOnlyList<BalanceTimelineSnapshot>>
                timelines = simulator.SimulateAll(allContent);
            PrintTimelines(timelines);
            VerifyMilestonesAndFiniteValues(timelines);
            VerifyDeterminism(simulator, allContent, timelines);
            VerifyMonotonicProgression(timelines);
            VerifyPacingWindows(timelines);
            VerifyHorizontalBuildBand(timelines);
            VerifyRouteIdentities(timelines);
            VerifySpawnFlowUsesRuntimeLimits();
            VerifyContentCapacity(simulator, allContent);
            GD.Print("Balance timeline contract test passed.");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    /// <summary>
    /// 以宽区间守住三倍原始供给下的成长密度和长局无尽节奏，避免模拟继续沿用旧刷新量的假预期。
    /// </summary>
    private static void VerifyPacingWindows(
        IReadOnlyDictionary<BalanceBuildKind, IReadOnlyList<BalanceTimelineSnapshot>> timelines)
    {
        (int Minimum, int Maximum)[] levelBands =
        [
            (1, 1), (16, 21), (27, 35), (40, 48),
            (54, 63), (64, 72), (82, 91), (104, 115),
        ];
        foreach ((BalanceBuildKind kind, IReadOnlyList<BalanceTimelineSnapshot> values) in timelines)
        {
            for (int index = 0; index < values.Count; index++)
            {
                BalanceTimelineSnapshot item = values[index];
                (int minimum, int maximum) = levelBands[index];
                Require(item.RunLevel >= minimum && item.RunLevel <= maximum,
                    $"Level pacing left its target band: {kind}/{item.ElapsedMinutes}m " +
                    $"was {item.RunLevel}, expected {minimum}-{maximum}.");
            }

            int learnedSpells = values[2].OffensiveSpellCount + values[2].SupportSpellCount;
            int spellCapacity = values[2].OffensiveSlotCapacity + values[2].SupportSlotCapacity;
            Require(learnedSpells >= 5 && learnedSpells <= spellCapacity,
                $"The five-minute build did not form a nearly complete spell loadout: {kind}/" +
                $"{learnedSpells}/{spellCapacity}.");
            bool dominating = values[2].EffectiveKillsPerSecond >=
                values[2].ScheduledSpawnsPerSecond * RunPacingTimeline.RequiredClearRatio;
            Require(values[2].PressureGear == RunPacingTimeline.AdaptiveRules.Count &&
                values[2].ProjectedAliveEnemies <= 30.0 &&
                values[2].EffectiveKillsPerSecond >= 8.0 && dominating,
                $"The five-minute build did not clear the tripled supply into the boss gate: {kind}/" +
                $"gear {values[2].PressureGear}, " +
                $"{values[2].ProjectedAliveEnemies:F1} alive, " +
                $"{values[2].EffectiveKillsPerSecond:F3}/" +
                $"{values[2].ScheduledSpawnsPerSecond:F3} kills/supply.");
            Require(values[4].OffensiveSpellCount == values[4].OffensiveSlotCapacity &&
                values[4].SupportSpellCount == values[4].SupportSlotCapacity,
                $"The twenty-minute build did not fill shared spell slots: {kind}.");
            Require(values[^1].OffensiveSpellCount == values[^1].OffensiveSlotCapacity &&
                values[^1].SupportSpellCount == values[^1].SupportSlotCapacity,
                $"The long-run build did not eventually fill shared spell slots: {kind}.");
        }
    }

    /// <summary>
    /// 对相同内容和构筑再次运行完整模拟，确认快照逐项相等且结果不依赖隐藏随机状态或目录缓存时序。
    /// </summary>
    private static void VerifyDeterminism(
        BalanceTimelineSimulator simulator,
        ContentPackSelection content,
        IReadOnlyDictionary<BalanceBuildKind, IReadOnlyList<BalanceTimelineSnapshot>> expected)
    {
        foreach (BalanceBuildKind kind in Enum.GetValues<BalanceBuildKind>())
        {
            BalanceTimelineSnapshot[] repeated = simulator.Simulate(kind, content).ToArray();
            Require(repeated.SequenceEqual(expected[kind]),
                $"Balance timeline is not deterministic for {kind}.");
        }
    }

    /// <summary>按构筑枚举顺序打印所有时间点，使策划修改后的变化可以直接从测试输出审阅。</summary>
    private static void PrintTimelines(
        IReadOnlyDictionary<BalanceBuildKind, IReadOnlyList<BalanceTimelineSnapshot>> timelines)
    {
        foreach (BalanceBuildKind kind in Enum.GetValues<BalanceBuildKind>())
        {
            GD.Print($"--- Balance timeline: {kind} ---");
            foreach (BalanceTimelineSnapshot snapshot in timelines[kind])
            {
                GD.Print(snapshot.FormatReport());
            }
        }
    }

    /// <summary>
    /// 确认四条路线均覆盖约定八个里程碑，并拒绝非有限、负数或无意义的基础字段。
    /// </summary>
    private static void VerifyMilestonesAndFiniteValues(
        IReadOnlyDictionary<BalanceBuildKind, IReadOnlyList<BalanceTimelineSnapshot>> timelines)
    {
        foreach ((BalanceBuildKind kind, IReadOnlyList<BalanceTimelineSnapshot> values) in timelines)
        {
            Require(values.Select(item => item.ElapsedMinutes)
                    .SequenceEqual(BalanceTimelineSimulator.DefaultMilestones),
                $"Timeline milestones changed for {kind}.");
            foreach (BalanceTimelineSnapshot item in values)
            {
                double[] finite = [item.WeaponDps, item.SpellDps, item.TotalDps,
                    item.ReadinessScore, item.ScheduledSpawnsPerSecond,
                    item.ProjectedAliveEnemies,
                    item.EffectiveKillsPerSecond,
                    item.EnemyPressure, item.PowerToPressureRatio,
                    item.SpiritEconomyMultiplier, item.SpellCapacityBudget];
                Require(finite.All(value => double.IsFinite(value) && value >= 0.0),
                    $"Timeline contains an invalid value: {kind}/{item.ElapsedMinutes}m.");
                Require(item.RunLevel >= 1 && item.TotalExperience >= 0 &&
                    item.OffensiveSpellCount <= item.OffensiveSlotCapacity &&
                    item.SupportSpellCount <= item.SupportSlotCapacity,
                    $"Timeline contains an invalid level or slot count: {kind}/{item.ElapsedMinutes}m.");
            }
        }
    }

    /// <summary>
    /// 要求等级、累计经验、伤害和敌群供给随时间不下降，并确保长局已进入无尽修行。
    /// </summary>
    private static void VerifyMonotonicProgression(
        IReadOnlyDictionary<BalanceBuildKind, IReadOnlyList<BalanceTimelineSnapshot>> timelines)
    {
        foreach ((BalanceBuildKind kind, IReadOnlyList<BalanceTimelineSnapshot> values) in timelines)
        {
            for (int index = 1; index < values.Count; index++)
            {
                BalanceTimelineSnapshot before = values[index - 1];
                BalanceTimelineSnapshot after = values[index];
                Require(after.RunLevel >= before.RunLevel &&
                    after.TotalExperience >= before.TotalExperience &&
                    after.TotalDps + 0.0001 >= before.TotalDps &&
                    after.ScheduledSpawnsPerSecond >= before.ScheduledSpawnsPerSecond,
                    $"A required curve decreased: {kind}/{before.ElapsedMinutes}-{after.ElapsedMinutes}m.");
            }

            Require(values[^1].EndlessRankCount > values[^2].EndlessRankCount &&
                values[^1].TotalDps > values[^2].TotalDps,
                $"The 60-120 minute build stopped its endless growth: {kind}.");
        }
    }

    /// <summary>
    /// 在每个时间点比较综合准备度而非单一伤害，确保特色路线有差异但不形成跨级的纵向碾压。
    /// </summary>
    private static void VerifyHorizontalBuildBand(
        IReadOnlyDictionary<BalanceBuildKind, IReadOnlyList<BalanceTimelineSnapshot>> timelines)
    {
        int count = BalanceTimelineSimulator.DefaultMilestones.Count;
        for (int index = 0; index < count; index++)
        {
            double[] readiness = Enum.GetValues<BalanceBuildKind>()
                .Select(kind => timelines[kind][index].ReadinessScore)
                .ToArray();
            double spread = readiness.Max() / Math.Max(0.01, readiness.Min());
            Require(spread <= 1.90,
                $"Horizontal build readiness spread is too wide at " +
                $"{BalanceTimelineSimulator.DefaultMilestones[index]}m: {spread:F3}.");
        }
    }

    /// <summary>
    /// 检查路线标签确实改变构筑：强攻开局最高，速射后期普攻最高，效用保持更高经济成长。
    /// </summary>
    private static void VerifyRouteIdentities(
        IReadOnlyDictionary<BalanceBuildKind, IReadOnlyList<BalanceTimelineSnapshot>> timelines)
    {
        BalanceTimelineSnapshot assault = timelines[BalanceBuildKind.Assault][0];
        BalanceTimelineSnapshot rapid = timelines[BalanceBuildKind.Rapid][^1];
        BalanceTimelineSnapshot utility = timelines[BalanceBuildKind.Utility][^1];
        BalanceTimelineSnapshot baseline = timelines[BalanceBuildKind.Baseline][^1];
        Require(assault.TotalDps == timelines.Values.Max(values => values[0].TotalDps),
            "Assault route lost its opening burst identity.");
        Require(rapid.WeaponDps == timelines.Values.Max(values => values[^1].WeaponDps),
            "Rapid route lost its late-game normal-attack identity.");
        Require(utility.SpiritEconomyMultiplier == timelines.Values.Max(
                values => values[^1].SpiritEconomyMultiplier),
            "Utility route lost its per-drop economy identity.");
        double rapidWeaponShare = rapid.WeaponDps / rapid.TotalDps;
        double baselineWeaponShare = baseline.WeaponDps / baseline.TotalDps;
        Require(rapidWeaponShare > baselineWeaponShare &&
            rapid.WeaponDps >= baseline.WeaponDps * 1.05,
            "Rapid route collapsed into the baseline route.");
    }

    /// <summary>
    /// 独立推进正式刷怪投影，确认无击破时会顶住动态存活上限，有处理能力时接纳率由批次与间隔决定。
    /// </summary>
    private static void VerifySpawnFlowUsesRuntimeLimits()
    {
        var stalled = new BalanceSpawnFlowState(initialAlive: 36);
        EndlessDifficultySnapshot opening = EndlessDifficultyCurve.EvaluateSeconds(0.0);
        BalanceSpawnFlowSnapshot blocked = stalled.Advance(opening, 0.0);
        Require(blocked.AliveCount > 36.0 &&
            Math.Abs(blocked.AcceptedSpawnsPerSecond -
                opening.ScheduledSpawnsPerSecond) < 0.000001,
            "Spawn projection silently restored an alive soft cap when combat stalled.");

        var clearing = new BalanceSpawnFlowState(initialAlive: 0);
        BalanceSpawnFlowSnapshot supplied = clearing.Advance(opening, 1000.0);
        Require(Math.Abs(supplied.AcceptedSpawnsPerSecond -
                supplied.ScheduledSpawnsPerSecond) < 0.000001 &&
            Math.Abs(supplied.DefeatsPerSecond -
                supplied.AcceptedSpawnsPerSecond) < 0.000001,
            "Spawn projection diverged from runtime batch and interval throughput.");
    }

    /// <summary>
    /// 比较本体、单一正作与全正作：候选数可横向增加，但三者都必须独立填满同一 4+2 容量。
    /// </summary>
    private static void VerifyContentCapacity(
        BalanceTimelineSimulator simulator,
        ContentPackSelection allContent)
    {
        BalanceTimelineSnapshot baseOnly = simulator.Simulate(
            BalanceBuildKind.Baseline, ContentPackSelection.BaseOnly)[^1];
        ContentPackDefinition singlePack = ContentPackCatalog.All.First(pack =>
            SpellCardCatalog.All.Count(card => card.SourcePackId == pack.Id) > 0);
        var singleContent = new ContentPackSelection([singlePack.Id]);
        BalanceTimelineSnapshot single = simulator.Simulate(
            BalanceBuildKind.Baseline, singleContent)[^1];
        BalanceTimelineSnapshot all = simulator.Simulate(
            BalanceBuildKind.Baseline, allContent)[^1];
        Require(single.EnabledSpellCount > baseOnly.EnabledSpellCount &&
            all.EnabledSpellCount > single.EnabledSpellCount,
            "All-content selection did not increase horizontal spell candidates.");
        Require(all.OffensiveSlotCapacity == single.OffensiveSlotCapacity &&
            all.OffensiveSlotCapacity == baseOnly.OffensiveSlotCapacity &&
            all.SupportSlotCapacity == single.SupportSlotCapacity &&
            all.SupportSlotCapacity == baseOnly.SupportSlotCapacity &&
            Math.Abs(all.SpellCapacityBudget - single.SpellCapacityBudget) < 0.0001 &&
            Math.Abs(all.SpellCapacityBudget - baseOnly.SpellCapacityBudget) < 0.0001,
            "Content selection changed spell-card power capacity.");
        Require(baseOnly.OffensiveSpellCount == SpellCardSlotPolicy.MaximumOffensiveSlots &&
            baseOnly.SupportSpellCount == SpellCardSlotPolicy.MaximumSupportSlots &&
            single.OffensiveSpellCount == SpellCardSlotPolicy.MaximumOffensiveSlots &&
            single.SupportSpellCount == SpellCardSlotPolicy.MaximumSupportSlots &&
            all.OffensiveSpellCount == SpellCardSlotPolicy.MaximumOffensiveSlots &&
            all.SupportSpellCount == SpellCardSlotPolicy.MaximumSupportSlots,
            "Base, single-pack, and all-content timelines must all fill the shared 4+2 slots.");
    }

    /// <summary>将任一策划契约失败转换为包含具体时间或路线的测试异常。</summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
