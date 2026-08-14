using Godot;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Runtime;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 验证奥义独立周期在推进、施展重置和构筑缩放时保持公平、单调且不依赖充能资源。
/// </summary>
public partial class SpellCardTimingTest : Node
{
    /// <summary>执行纯计时与纯缩放契约，失败时返回非零退出码供完整回归识别。</summary>
    public override void _Ready()
    {
        try
        {
            VerifyIndependentTimers();
            VerifyProgressPreservingRescale();
            GD.Print("Spell card timing test passed.");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    /// <summary>确认两个相同起点的周期可独立推进和重置，一张施展不会改写另一张。</summary>
    private static void VerifyIndependentTimers()
    {
        var first = new SpellCardTimerState(6.0f);
        var second = new SpellCardTimerState(9.0f);
        first.Advance(6.0f);
        second.Advance(6.0f);
        Require(first.RemainingSeconds <= 0.0f &&
            Mathf.IsEqualApprox(second.RemainingSeconds, 3.0f),
            "Independent spell timers did not advance separately.");
        first.Restart(6.0f);
        Require(Mathf.IsEqualApprox(first.RemainingSeconds, 6.0f) &&
            Mathf.IsEqualApprox(second.RemainingSeconds, 3.0f),
            "Restarting one spell timer changed another timer.");
    }

    /// <summary>确认加快或放慢周期会保留已修炼进度比例，而已经到期的奥义继续保持就绪。</summary>
    private static void VerifyProgressPreservingRescale()
    {
        var timer = new SpellCardTimerState(8.0f);
        timer.Advance(2.0f);
        timer.Rescale(4.0f);
        Require(Mathf.IsEqualApprox(timer.RemainingSeconds, 3.0f),
            "Faster spell cadence did not preserve elapsed progress.");
        timer.Rescale(10.0f);
        Require(Mathf.IsEqualApprox(timer.RemainingSeconds, 7.5f),
            "Slower spell cadence did not preserve elapsed progress.");
        timer.Advance(8.0f);
        timer.Rescale(3.0f);
        Require(timer.RemainingSeconds < 0.0f,
            "A ready spell was incorrectly delayed by cadence rescaling.");
    }

    /// <summary>将计时契约失败转换为包含具体原因的测试异常。</summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
