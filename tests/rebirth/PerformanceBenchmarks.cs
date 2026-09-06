using System.Diagnostics;
using System.Numerics;
using System.Text;
using System.Text.Json;
using Rebirth.Core;

namespace Rebirth.Tests;

public static class PerformanceBenchmarks
{
    public static int Run(string[] arguments)
    {
        Measure(180, 600, 120);
        var results = new[] { Measure(180, 600, 1200), Measure(320, 1600, 1200) };
        var json = JsonSerializer.Serialize(new { runtime = Environment.Version.ToString(), results }, new JsonSerializerOptions { WriteIndented = true });
        Console.WriteLine(json);
        var destination = arguments.FirstOrDefault(argument => argument.StartsWith("--output="));
        if (destination != null) File.WriteAllText(destination[9..], json, new UTF8Encoding(false));
        return 0;
    }

    private static object Measure(int enemyCount, int projectileCount, int ticks)
    {
        var run = new RunState(HeroKind.Reimu, 260906);
        Array.Clear(run.Ranks);
        var random = new Random(17);
        for (var index = 0; index < enemyCount; index++)
            run.Enemies.Add(new() { Id = index + 10000, Kind = EnemyKind.Kedama, Position = new(index % 20 * 65 - 620, index / 20 * 65 - 480), Radius = 14, Health = 1000000, MaxHealth = 1000000 });
        var samples = new double[ticks];
        var allocated = GC.GetAllocatedBytesForCurrentThread();
        for (var tick = 0; tick < ticks; tick++)
        {
            var start = Stopwatch.GetTimestamp();
            while (run.Projectiles.Count < projectileCount)
            {
                var hostile = random.Next(4) != 0;
                run.Projectiles.Add(new() { Position = new(random.Next(-650, 650), random.Next(-450, 450)), Velocity = Geometry.Angle((float)random.NextDouble() * MathF.Tau) * 200,
                    Hostile = hostile, Life = 3, Radius = 5, Damage = 0, Art = ArtKind.Ofuda, TurnRate = hostile ? 0 : 5.5f, Pierce = hostile ? 0 : 2 });
            }
            while (run.Phase == RunPhase.Choosing) run.Choose(0);
            run.Step(new(Vector2.Zero));
            samples[tick] = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        }
        allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;
        Array.Sort(samples);
        return new { enemyCount, projectileCount, ticks, meanMs = samples.Average(), p95Ms = samples[(int)(ticks * 0.95)], allocatedPerTick = allocated / ticks,
            finalTicks = run.Ticks, health = run.Health, projectiles = run.Projectiles.Count, grazes = run.Grazes, spells = run.SpellsCast };
    }
}
