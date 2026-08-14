using Godot;
using TouhouWuxiaSurvivor.Gameplay.Pacing;
using TouhouWuxiaSurvivor.Gameplay.Session;
using TouhouWuxiaSurvivor.Tests.Support;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 通过真实Boss事件和完成覆盖层验证首次通关选择、无尽恢复及成功原因转交的完整状态流。
/// </summary>
public partial class RunPacingFlowTest : Node
{
    /// <summary>依次执行无尽与结算两条互斥路径，并在退出前恢复暂停状态与释放测试节点。</summary>
    public override async void _Ready()
    {
        int exitCode = 0;
        try
        {
            await VerifyEndlessChoice();
            await VerifySettlementChoice();
            GD.Print("Run pacing flow test passed.");
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            exitCode = 1;
        }
        finally
        {
            GetTree().Paused = false;
            GetTree().Quit(exitCode);
        }
    }

    /// <summary>击破最终Boss后选择继续游历，确认解除暂停并让后续Boss不再重复打开完成层。</summary>
    private async Task VerifyEndlessChoice()
    {
        var fixture = new RunPacingRuntimeFixture(this);
        try
        {
            fixture.DefeatBoss(RunPacingTimeline.FinalEncounterSeconds);
            Require(fixture.Overlay.IsOpen && GetTree().Paused &&
                fixture.Pacing.IsCompletionPending && !fixture.Pacing.IsEndless,
                "Final boss defeat did not open the paused completion choice.");
            Require(fixture.Map.InputBlocked && fixture.Pause.InputBlocked &&
                fixture.Stats.InputBlocked &&
                fixture.Progression.IsChoicePresentationSuspended,
                "Completion choice did not own the competing modal inputs.");

            fixture.Overlay.GetNode<Button>(
                "Root/Panel/Padding/Layout/Buttons/Endless").EmitSignal(Button.SignalName.Pressed);
            Require(!fixture.Overlay.IsOpen && !GetTree().Paused &&
                fixture.Pacing.IsEndless && !fixture.Pacing.IsCompletionPending &&
                fixture.Pacing.CreateSnapshot().PhaseId == RunPhaseId.Endless,
                "Endless choice did not preserve the run and resume the world.");
            Require(!fixture.Map.InputBlocked && !fixture.Pause.InputBlocked &&
                !fixture.Stats.InputBlocked &&
                !fixture.Progression.IsChoicePresentationSuspended,
                "Endless choice did not release modal input ownership.");

            fixture.DefeatBoss(RunPacingTimeline.TargetClearSeconds + 60.0);
            Require(!fixture.Overlay.IsOpen,
                "A later endless boss reopened the one-time completion choice.");
        }
        finally
        {
            await fixture.FreeAsync(this);
        }
    }

    /// <summary>在全新流程中选择平定异变，确认只转交Cleared原因且保持暂停供总结页接管。</summary>
    private async Task VerifySettlementChoice()
    {
        var fixture = new RunPacingRuntimeFixture(this);
        try
        {
            fixture.DefeatBoss(RunPacingTimeline.FinalEncounterSeconds);
            fixture.Overlay.GetNode<Button>(
                "Root/Panel/Padding/Layout/Buttons/Settle").EmitSignal(Button.SignalName.Pressed);
            Require(fixture.FinalizedReason == RunEndReason.Cleared &&
                !fixture.Overlay.IsOpen && GetTree().Paused,
                "Settlement choice did not hand off a successful frozen run end.");
            Require(RunSummaryTextFormatter.FormatOutcomeTitle(RunEndReason.Cleared) ==
                "异变平定",
                "Successful settlement did not resolve to the dedicated outcome presentation.");
        }
        finally
        {
            await fixture.FreeAsync(this);
        }
    }

    /// <summary>将任一流程契约失败转换为带明确原因的测试异常。</summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

}
