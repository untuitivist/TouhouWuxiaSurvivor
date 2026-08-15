using Godot;
using TouhouWuxiaSurvivor.Gameplay.Pacing;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 验证动态阶段只响应持续割草能力，并以最短展示和最长兜底保护五分钟主流程。
/// </summary>
public partial class AdaptiveRunPacingTest : Node
{
    /// <summary>执行强势、爆发和弱势三种遥测轨迹，任一阶段误跳均以非零退出码报告。</summary>
    public override void _Ready()
    {
        try
        {
            VerifySustainedDominanceAdvancesEarly();
            VerifyBurstDoesNotSkipShowcase();
            VerifyCrowdPressureBlocksAdvance();
            VerifySpawnSupplyBlocksFalseMowing();
            VerifyClearedBattlefieldCountsAsDominance();
            VerifyPreBossMowingAdvancesBeforeTimeout();
            VerifyClockCannotMoveBackwards();
            VerifyTimeoutPreventsDeadlock();
            GD.Print("Adaptive run pacing test passed.");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    /// <summary>持续低存量与每秒击破应在最短展示后提前进阶，且难度时间保持单调。</summary>
    private static void VerifySustainedDominanceAdvancesEarly()
    {
        var state = new AdaptiveRunPacingState();
        int defeated = 0;
        double previousDifficulty = 0.0;
        for (int second = 0; second <= 40; second++)
        {
            if (second > 0) defeated += 2;
            state.Advance(second, new RunCombatTelemetry(6, defeated, 36));
            RunPacingSnapshot snapshot = state.CreateSnapshot(second);
            Require(snapshot.DifficultySeconds >= previousDifficulty,
                "Adaptive difficulty time moved backwards.");
            previousDifficulty = snapshot.DifficultySeconds;
            if (second < 30)
            {
                Require(snapshot.PhaseId == RunPhaseId.Opening,
                    "Opening phase skipped its minimum showcase duration.");
            }
        }

        Require(state.PhaseId == RunPhaseId.Rising,
            "Sustained mowing did not advance before the opening timeout.");
    }

    /// <summary>单次高击破后停止输出不得凑满持续压制时间，也不能越过固定兜底时点。</summary>
    private static void VerifyBurstDoesNotSkipShowcase()
    {
        var state = new AdaptiveRunPacingState();
        state.Advance(29.0, new RunCombatTelemetry(4, 0, 36));
        state.Advance(30.0, new RunCombatTelemetry(4, 30, 36));
        for (int second = 31; second < 45; second++)
        {
            state.Advance(second, new RunCombatTelemetry(4, 30, 36));
        }

        Require(state.PhaseId == RunPhaseId.Opening,
            "A single burst was mistaken for sustained mowing capability.");
    }

    /// <summary>持续击破但敌群仍接近上限时不算割草，防止只看击杀数字而忽略积压。</summary>
    private static void VerifyCrowdPressureBlocksAdvance()
    {
        var state = new AdaptiveRunPacingState();
        int defeated = 0;
        for (int second = 0; second < 45; second++)
        {
            if (second > 0) defeated += 2;
            state.Advance(second, new RunCombatTelemetry(34, defeated, 36));
        }

        Require(state.PhaseId == RunPhaseId.Opening,
            "High kills with a saturated crowd were mistaken for mowing dominance.");
    }

    /// <summary>击破率低于当前刷新供给时，即使敌群暂时较少也不能提前跳到下一阶段。</summary>
    private static void VerifySpawnSupplyBlocksFalseMowing()
    {
        var state = new AdaptiveRunPacingState();
        int defeated = 0;
        for (int second = 0; second < 45; second++)
        {
            if (second > 0 && second % 2 == 0) defeated++;
            state.Advance(second, new RunCombatTelemetry(6, defeated, 36, 0.80));
        }

        Require(state.PhaseId == RunPhaseId.Opening,
            "Kill throughput below spawn supply was mistaken for mowing dominance.");
    }

    /// <summary>场上只剩零星敌人时不强求持续击杀，避免强构筑因没有目标反而卡住阶段。</summary>
    private static void VerifyClearedBattlefieldCountsAsDominance()
    {
        var state = new AdaptiveRunPacingState();
        for (int second = 0; second <= 36; second++)
        {
            state.Advance(second, new RunCombatTelemetry(1, 0, 36, 0.80));
        }

        Require(state.PhaseId == RunPhaseId.Rising,
            "An already-cleared battlefield could not advance after the minimum showcase.");
    }

    /// <summary>前四阶段兜底后，持续清理残阵应在四分半硬时限前武装最终遭遇。</summary>
    private static void VerifyPreBossMowingAdvancesBeforeTimeout()
    {
        var state = new AdaptiveRunPacingState();
        state.Advance(RunPacingTimeline.CrisisSeconds,
            new RunCombatTelemetry(36, 0, 36));
        int defeated = 0;
        for (int second = 211; second < 270 && !state.IsFinalEncounter; second++)
        {
            defeated++;
            state.Advance(second, new RunCombatTelemetry(12, defeated, 76));
        }

        Require(state.IsFinalEncounter,
            "A sustained pre-Boss mowing window did not arm the encounter before timeout.");
    }

    /// <summary>异常倒退的外部时钟不得让阶段、真实显示时间或难度进度回退。</summary>
    private static void VerifyClockCannotMoveBackwards()
    {
        var state = new AdaptiveRunPacingState();
        state.Advance(20.0, new RunCombatTelemetry(8, 4, 36));
        RunPacingSnapshot before = state.CreateSnapshot(20.0);
        state.Advance(10.0, new RunCombatTelemetry(8, 4, 36));
        RunPacingSnapshot after = state.CreateSnapshot(10.0);
        Require(after.ElapsedSeconds >= before.ElapsedSeconds &&
            after.DifficultySeconds >= before.DifficultySeconds,
            "Adaptive pacing moved backwards with a regressed external clock.");
    }

    /// <summary>完全没有击破时仍应依次经过所有最长时限，并在五分钟武装最终遭遇。</summary>
    private static void VerifyTimeoutPreventsDeadlock()
    {
        var state = new AdaptiveRunPacingState();
        state.Advance(RunPacingTimeline.TargetClearSeconds,
            new RunCombatTelemetry(36, 0, 36));
        RunPacingSnapshot final = state.CreateSnapshot(
            RunPacingTimeline.TargetClearSeconds);
        Require(final.IsFinalEncounter && final.PhaseId == RunPhaseId.FinalEncounter &&
            final.DifficultySeconds == RunPacingTimeline.FinalEncounterSeconds,
            "Weak build did not reach the final encounter through the hard timeout.");
    }

    /// <summary>把任一契约失败转换为带明确原因的测试异常。</summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
