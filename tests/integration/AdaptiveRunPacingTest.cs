using Godot;
using TouhouWuxiaSurvivor.Gameplay.Pacing;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 验证动态阶段只比较最近三十秒滑动窗口的实际生成与击破数，不读取存活量或时间兜底。
/// </summary>
public partial class AdaptiveRunPacingTest : Node
{
    /// <summary>执行合格、失败、空供给与大跨度窗口，任一换挡误判均以非零退出码报告。</summary>
    public override void _Ready()
    {
        try
        {
            VerifyQualifiedWindowAdvancesOnce();
            VerifySlidingWindowRechecksContinuously();
            VerifyEmptyWindowCannotAdvance();
            VerifySevenQualifiedWindowsReachBoss();
            VerifyClockCannotMoveBackwards();
            GD.Print("Adaptive run pacing test passed.");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    /// <summary>三十秒内击破数等于实际生成数时提高一档，提前一帧与单次巨量击破都不能跳多档。</summary>
    private static void VerifyQualifiedWindowAdvancesOnce()
    {
        var state = new AdaptiveRunPacingState();
        state.Advance(0.0, new RunCombatTelemetry(0, 0));
        state.Advance(29.9, new RunCombatTelemetry(23, 23));
        Require(state.PhaseId == RunPhaseId.Opening,
            "A window advanced before thirty seconds elapsed.");
        state.Advance(30.0, new RunCombatTelemetry(23, 230));
        Require(state.PhaseId == RunPhaseId.Rising,
            "A qualified window did not advance exactly one gear.");
    }

    /// <summary>整三十秒未达标后，下一秒滑动出的新窗口可以立即重新判定，不等待下一个分箱。</summary>
    private static void VerifySlidingWindowRechecksContinuously()
    {
        var state = new AdaptiveRunPacingState();
        state.Advance(0.0, new RunCombatTelemetry(0, 0));
        state.Advance(1.0, new RunCombatTelemetry(1, 0));
        state.Advance(30.0, new RunCombatTelemetry(30, 29));
        Require(state.PhaseId == RunPhaseId.Opening,
            "An underperforming rolling window advanced the pressure gear.");
        state.Advance(31.0, new RunCombatTelemetry(31, 31));
        Require(state.PhaseId == RunPhaseId.Rising,
            "The rolling window waited for a second fixed thirty-second bucket.");
    }

    /// <summary>没有实际生成敌人的空窗口不能用零等于零误判为割草能力。</summary>
    private static void VerifyEmptyWindowCannotAdvance()
    {
        var state = new AdaptiveRunPacingState();
        state.Advance(0.0, new RunCombatTelemetry(0, 0));
        state.Advance(30.0, new RunCombatTelemetry(0, 0));
        Require(state.PhaseId == RunPhaseId.Opening,
            "An empty telemetry window advanced the pressure gear.");
    }

    /// <summary>连续七个合格窗口在最快三分半进入Boss，且每窗快照记录真实S与K。</summary>
    private static void VerifySevenQualifiedWindowsReachBoss()
    {
        var state = new AdaptiveRunPacingState();
        state.Advance(0.0, new RunCombatTelemetry(0, 0));
        int total = 0;
        for (int window = 1; window <= 7; window++)
        {
            total += 24 + window;
            state.Advance(window * 30.0, new RunCombatTelemetry(total, total));
        }

        RunPacingSnapshot final = state.CreateSnapshot(210.0);
        Require(final.IsFinalEncounter && final.PressureGear == 7,
            "Seven qualified windows did not reach the final encounter.");
    }

    /// <summary>异常倒退的外部时钟不得让阶段、真实显示时间或难度进度回退。</summary>
    private static void VerifyClockCannotMoveBackwards()
    {
        var state = new AdaptiveRunPacingState();
        state.Advance(0.0, new RunCombatTelemetry(0, 0));
        state.Advance(20.0, new RunCombatTelemetry(8, 4));
        RunPacingSnapshot before = state.CreateSnapshot(20.0);
        state.Advance(10.0, new RunCombatTelemetry(8, 4));
        RunPacingSnapshot after = state.CreateSnapshot(10.0);
        Require(after.ElapsedSeconds >= before.ElapsedSeconds &&
            after.DifficultySeconds >= before.DifficultySeconds,
            "Adaptive pacing moved backwards with a regressed external clock.");
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
