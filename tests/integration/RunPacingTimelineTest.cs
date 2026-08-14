using Godot;
using TouhouWuxiaSurvivor.Actors.Enemies;
using TouhouWuxiaSurvivor.Gameplay.Difficulty;
using TouhouWuxiaSurvivor.Gameplay.Encounters;
using TouhouWuxiaSurvivor.Gameplay.Pacing;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 验证五分钟目标阶段、敌人解锁、玩家弹幕与刷怪批量共享同一组稳定里程碑。
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
            (44.999, RunPhaseId.Opening),
            (45.0, RunPhaseId.Rising),
            (90.0, RunPhaseId.Swarming),
            (150.0, RunPhaseId.Barrage),
            (210.0, RunPhaseId.Crisis),
            (270.0, RunPhaseId.FinalEncounter),
        ];
        foreach ((double seconds, RunPhaseId expected) in samples)
        {
            RunPacingSnapshot snapshot = RunPacingTimeline.Evaluate(seconds);
            Require(snapshot.PhaseId == expected,
                $"Run phase at {seconds} seconds was {snapshot.PhaseId}, expected {expected}.");
        }

        RunPacingSnapshot midpoint = RunPacingTimeline.Evaluate(135.0);
        Require(Math.Abs(midpoint.TotalProgress - 0.5) < 0.000001 &&
            midpoint.SecondsToNextPhase == 15.0,
            "Structured run progress or next milestone countdown drifted.");
        RunPacingSnapshot final = RunPacingTimeline.Evaluate(360.0);
        RunPacingSnapshot endless = RunPacingTimeline.Evaluate(360.0, true);
        Require(final.IsFinalEncounter && !final.IsEndless && endless.IsEndless &&
            final.TotalProgress == 1.0 && endless.TotalProgress == 1.0 &&
            RunPacingTimeline.TargetClearSeconds == 300.0,
            "Final encounter did not remain gated until the explicit endless choice.");
    }

    /// <summary>确认玩家弹数和敌人生成批量在阶段边界增长，而不是继续使用旧的独立分钟表。</summary>
    private static void VerifyCombatMilestones()
    {
        double[] seconds = [0.0, 45.0, 90.0, 150.0, 210.0, 270.0];
        int[] expectedBatches = [1, 2, 3, 4, 5, 6];
        for (int index = 0; index < seconds.Length; index++)
        {
            EndlessDifficultySnapshot difficulty = EndlessDifficultyCurve.EvaluateSeconds(
                seconds[index], 140);
            Require(difficulty.SpawnBatchSize == expectedBatches[index],
                $"Spawn batch drifted at {seconds[index]} seconds.");
        }

        Require(PlayerBarrageCurve.EvaluateSeconds(44.0, false, 0, 0).ProjectileCount == 1 &&
            PlayerBarrageCurve.EvaluateSeconds(45.0, false, 0, 0).ProjectileCount == 3 &&
            PlayerBarrageCurve.EvaluateSeconds(150.0, false, 0, 0).ProjectileCount == 5 &&
            PlayerBarrageCurve.EvaluateSeconds(210.0, false, 1, 0).Mode ==
                PlayerBarrageMode.RotatingRing,
            "Player barrage did not follow the shared phase milestones.");
    }

    /// <summary>锁定九类本体敌人的职责展开顺序，使前期可读追击自然过渡到后期混合弹幕。</summary>
    private static void VerifyEnemyUnlocks()
    {
        Require(Enemy("毛玉").UnlockTime == 0.0f && Enemy("野妖精").UnlockTime == 0.0f,
            "Opening enemy pair is no longer immediately available.");
        Require(Enemy("妖虫").UnlockTime == RunPacingTimeline.RisingSeconds &&
            Enemy("阴阳玉").UnlockTime == RunPacingTimeline.SwarmingSeconds &&
            Enemy("森林精怪").UnlockTime == RunPacingTimeline.SwarmingSeconds &&
            Enemy("山精").UnlockTime == RunPacingTimeline.BarrageSeconds &&
            Enemy("流窜妖怪").UnlockTime == RunPacingTimeline.BarrageSeconds &&
            Enemy("夜行妖怪").UnlockTime == RunPacingTimeline.CrisisSeconds &&
            Enemy("大妖怪").UnlockTime == RunPacingTimeline.CrisisSeconds,
            "Base enemy unlocks diverged from the five structured phases.");
    }

    /// <summary>确认角色Boss导演在四分半进入决战，为五分钟目标保留约半分钟战斗窗口。</summary>
    private static void VerifyBossMilestone()
    {
        var director = new BossEncounterDirector();
        Require(director.FirstEncounterSeconds == RunPacingTimeline.FinalEncounterSeconds,
            "First character boss is not aligned to the fifteen-minute finale.");
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
