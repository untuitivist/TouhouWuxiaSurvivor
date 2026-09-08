using System.Diagnostics;

namespace Rebirth.Core;

public sealed class CombatTimings
{
    private long started;
    public double[] Milliseconds { get; } = new double[6];
    public void Begin() => started = Stopwatch.GetTimestamp();
    public void Stamp(int system)
    {
        var now = Stopwatch.GetTimestamp();
        Milliseconds[system] = (now - started) * 1000.0 / Stopwatch.Frequency;
        started = now;
    }
}
