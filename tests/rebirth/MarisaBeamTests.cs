using Rebirth.Core;
using Rebirth.Diagnostics;
using static Rebirth.Tests.MarisaGrowthTests;

namespace Rebirth.Tests;

internal static class MarisaBeamTests
{
    public static void Branches()
    {
        foreach (var clear in new[] { false, true })
        {
            var run = new RunState(HeroKind.Marisa, 42);
            run.Ranks[(int)ArtKind.Stars] = 0;
            Learn(run, UpgradeCatalog.MasterSparkUnlock);
            if (clear) Learn(run, UpgradeCatalog.SparkClear);
            var enemy = Target(run, new(400, 0));
            for (var index = 0; index < 50; index++) run.Projectiles.Add(new() { Hostile = true, Position = new(200, 0), Life = 10, Radius = 1 });
            run.Step(default);
            MarisaGrowthTests.Check(run.Projectiles.Count == 50 && enemy.Health == enemy.MaxHealth, "Warmup does not clear or damage");
            while (run.Beam!.Warmup > 0) run.Step(default);
            run.Step(default);
            var expected = 50 - (clear ? MarisaTuning.BeamClearLimit : 0);
            MarisaGrowthTests.Check(run.Projectiles.Count == expected && enemy.Health < enemy.MaxHealth, "Only learned clearing consumes the pulse budget");
            var health = enemy.Health;
            var heading = run.Beam.Direction;
            run.Step(default);
            MarisaGrowthTests.Check(run.Projectiles.Count == expected && enemy.Health == health && run.Beam.Direction == heading, "No per-frame clearing or damage; default aim stays locked");
            if (clear)
            {
                while (run.Projectiles.Count == expected) run.Step(default);
                MarisaGrowthTests.Check(run.Projectiles.Count == expected - MarisaTuning.BeamClearLimit, "Budget resets at next pulse");
            }
        }
        var sweeping = new RunState(HeroKind.Marisa, 42);
        sweeping.Ranks[(int)ArtKind.Stars] = 0;
        Learn(sweeping, UpgradeCatalog.MasterSparkUnlock, UpgradeCatalog.SparkSweep, UpgradeCatalog.SparkClear);
        Target(sweeping, new(400, 0));
        sweeping.Step(default);
        var beam = sweeping.Beam!;
        MarisaGrowthTests.Check(beam.Sweep && beam.ClearsBullets && beam.Direction.Y < 0, "Compatible beam branches captured together");
        var start = beam.Direction;
        while (beam.Warmup > 0) sweeping.Step(default);
        while (beam.Remaining > beam.Duration * 0.2f) sweeping.Step(default);
        MarisaGrowthTests.Check(beam.Direction.Y > 0 && beam.Direction != start && Math.Abs(beam.Direction.Length() - 1) < 0.001f, "Sweeps across original aim at unit length");
        MarisaGrowthTests.Check(Math.Abs(MathF.Atan2(beam.Direction.Y, beam.Direction.X)) <= MarisaTuning.SweepDegrees * MathF.PI / 180, "Sweep stays within authored arc");
        sweeping.TogglePause();
        var remaining = beam.Remaining;
        var headingBeforePause = beam.Direction;
        for (var tick = 0; tick < 60; tick++) sweeping.Step(default);
        MarisaGrowthTests.Check(beam.Remaining == remaining && beam.Direction == headingBeforePause, "Paused sweep freezes duration and angle");
    }

    public static void Stress()
    {
        foreach (var hero in Enum.GetValues<HeroKind>())
        foreach (var count in new[] { 40, 180, 320 })
        {
            var run = new RunState(hero, 42);
            var stress = new CombatStressScenario(run, count);
            MarisaGrowthTests.Check(run.Build.AllocatedPoints == 8 && !run.Build.SignatureUnlocked, "Equal eight-point, non-signature diagnostic builds");
            MarisaGrowthTests.Check(run.Enemies.Count == count && run.Projectiles.Count == stress.ProjectileCount, "Requested populations preserved");
            for (var tick = 0; tick < 120; tick++)
            {
                if (tick % 30 == 0) run.AddExperience(run.NextLevelExperience);
                stress.Refill(run);
                run.Step(default);
            }
            stress.Refill(run);
            MarisaGrowthTests.Check(run.Ticks == 120 && run.Phase == RunPhase.Playing, "Stress simulation keeps advancing through choices");
        }
    }
}
