using Godot;
using TouhouWuxiaSurvivor.Actors.Enemies;
using TouhouWuxiaSurvivor.Gameplay.Difficulty;
using TouhouWuxiaSurvivor.Gameplay.Encounters;
using TouhouWuxiaSurvivor.Gameplay.Pacing;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 验证七档名义时间轴、敌人强度解锁和连续刷新率共享数据，同时拒绝阶段赠送玩家弹幕。
/// </summary>
public partial class RunPacingTimelineTest : Node
{
    /// <summary>执行全部纯数据契约，并以非零退出码报告任一跨系统时间漂移。</summary>
    public override void _Ready()
    {
        try
        {
            VerifyTimeline();
            VerifyCombatMilestones();
            VerifyEnemyUnlocks();
            VerifyBossMilestone();
            GD.Print("Run pacing timeline test passed.");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    /// <summary>逐个检查有限阶段边界、总进度、最终遭遇停留与显式无尽转换。</summary>
    private static void VerifyTimeline()
    {
        (double Seconds, RunPhaseId Phase)[] samples =
        [
            (0.0, RunPhaseId.Opening),
            (29.999, RunPhaseId.Opening),
            (30.0, RunPhaseId.Rising),
            (60.0, RunPhaseId.Swarming),
            (90.0, RunPhaseId.Barrage),
            (120.0, RunPhaseId.Crisis),
            (150.0, RunPhaseId.Dominance),
            (180.0, RunPhaseId.Breakthrough),
            (210.0, RunPhaseId.FinalEncounter),
        ];
        foreach ((double seconds, RunPhaseId expected) in samples)
        {
            RunPacingSnapshot snapshot = RunPacingTimeline.Evaluate(seconds);
            Require(snapshot.PhaseId == expected,
                $"Run phase at {seconds} seconds was {snapshot.PhaseId}, expected {expected}.");
        }

        RunPacingSnapshot midpoint = RunPacingTimeline.Evaluate(105.0);
        Require(Math.Abs(midpoint.TotalProgress - 0.5) < 0.000001 &&
            midpoint.SecondsToNextPhase == 15.0,
            "Structured run progress or next milestone countdown drifted.");
        RunPacingSnapshot final = RunPacingTimeline.Evaluate(360.0);
        RunPacingSnapshot endless = RunPacingTimeline.Evaluate(360.0, true);
        Require(final.IsFinalEncounter && !final.IsEndless && endless.IsEndless &&
            final.TotalProgress == 1.0 && endless.TotalProgress == 1.0 &&
            endless.DifficultySeconds == 360.0 &&
            EnemyPressureCurve.Evaluate(endless.DifficultySeconds)
                .SpawnRatePerSecond > RunPacingTimeline.FinalEncounterRule.SpawnRatePerSecond &&
            RunPacingTimeline.TargetClearSeconds == 300.0,
            "Final encounter or post-final endless pressure projection drifted.");
    }

    /// <summary>确认敌人总供给连续增长，而相同构筑在任意名义阶段都保持相同玩家弹数。</summary>
    private static void VerifyCombatMilestones()
    {
        double[] seconds = [0.0, 30.0, 60.0, 90.0, 120.0, 150.0, 180.0, 210.0];
        double previousRate = 0.0;
        for (int index = 0; index < seconds.Length; index++)
        {
            EndlessDifficultySnapshot difficulty = EndlessDifficultyCurve.EvaluateSeconds(
                seconds[index]);
            Require(difficulty.ScheduledSpawnsPerSecond > previousRate || index == 0,
                $"Spawn rate did not rise at {seconds[index]} seconds.");
            previousRate = difficulty.ScheduledSpawnsPerSecond;
        }

        Require(seconds.All(second => PlayerBarrageCurve.EvaluateSeconds(
                second, false, 0, 0).ProjectileCount == 1) &&
            PlayerBarrageCurve.EvaluateSeconds(210.0, false, 0, 0, 4)
                .ProjectileCount == 5,
            "Pressure milestones still grant player barrage power outside upgrade choices.");
    }

    /// <summary>锁定九类本体敌人的职责展开顺序，使前期可读追击自然过渡到后期混合弹幕。</summary>
    private static void VerifyEnemyUnlocks()
    {
        Require(Enemy("毛玉").UnlockTime == 0.0f && Enemy("野妖精").UnlockTime == 0.0f,
            "Opening enemy pair is no longer immediately available.");
        Require(Enemy("妖虫").UnlockTime == RunPacingTimeline.RisingSeconds &&
            Enemy("阴阳玉").UnlockTime == RunPacingTimeline.RisingSeconds &&
            Enemy("森林精怪").UnlockTime == RunPacingTimeline.RisingSeconds &&
            Enemy("山精").UnlockTime == RunPacingTimeline.BarrageSeconds &&
            Enemy("流窜妖怪").UnlockTime == RunPacingTimeline.RisingSeconds &&
            Enemy("夜行妖怪").UnlockTime == RunPacingTimeline.SwarmingSeconds &&
            Enemy("大妖怪").UnlockTime == RunPacingTimeline.CrisisSeconds,
            "Base enemy unlocks diverged from their four strength tiers.");
    }

    /// <summary>确认角色Boss导演在四分半进入决战，为五分钟目标保留约半分钟战斗窗口。</summary>
    private static void VerifyBossMilestone()
    {
        var director = new BossEncounterDirector();
        Require(!director.IsFirstEncounterArmed,
            "Boss director was armed before the adaptive final phase.");
        director.ArmFirstEncounter(240.0);
        Require(director.IsFirstEncounterArmed && director.NextEncounterSeconds == 240.0,
            "Adaptive final phase did not arm the first boss at its actual transition time.");
        director.Free();
    }

    /// <summary>按中文稳定名称取得本体敌人定义，缺失时直接暴露目录错误。</summary>
    private static EnemyDefinition Enemy(string name) => EnemyCatalog.All.Single(
        definition => definition.RequiredContentPack is null && definition.DisplayName == name);

    /// <summary>将任一节奏契约失败转换为带明确原因的测试异常。</summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
