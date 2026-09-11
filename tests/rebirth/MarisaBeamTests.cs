using System.Numerics;
using Rebirth.Core;
using Rebirth.Diagnostics;
using static Rebirth.Tests.MarisaGrowthTests;

namespace Rebirth.Tests;

internal static class MarisaBeamTests
{
    public static void Branches()
    {
        var fixedRun = new RunState(HeroKind.Marisa, 42);
        var steeringRun = new RunState(HeroKind.Marisa, 42);
        foreach (var run in new[] { fixedRun, steeringRun })
        {
            run.Ranks[(int)ArtKind.Stars] = 0;
            Learn(run, UpgradeCatalog.MasterSparkUnlock);
            Target(run, new(400, 0));
        }
        Learn(steeringRun, UpgradeCatalog.SparkSteer, UpgradeCatalog.SparkWide, UpgradeCatalog.SparkClear);
        fixedRun.Step(default); steeringRun.Step(default);
        MarisaGrowthTests.Check(Math.Abs(steeringRun.Beam!.HalfWidth / fixedRun.Beam!.HalfWidth - MarisaTuning.WideMultiplier) < 0.001f, "Wide branch changes width, not damage");
        MarisaGrowthTests.Check(steeringRun.Beam.Damage == fixedRun.Beam.Damage, "Width does not copy pulse damage");
        fixedRun.Enemies[0].Position = steeringRun.Enemies[0].Position = new(0, 400);
        for (var tick = 0; tick < 30; tick++) { fixedRun.Step(default); steeringRun.Step(default); }
        MarisaGrowthTests.Check(fixedRun.Beam.Direction.Y == 0 && steeringRun.Beam.Direction.Y > 0.99f, "Only learned steering tracks the nearest enemy while the player stands still");
        var direction = steeringRun.Beam.Direction;
        MarisaGrowthTests.Check(Math.Abs(direction.Length() - 1) < 0.001f, "Tracking preserves a normalized heading");
        steeringRun.TogglePause(); var remaining = steeringRun.Beam.Remaining;
        for (var tick = 0; tick < 60; tick++) steeringRun.Step(new(System.Numerics.Vector2.UnitX));
        MarisaGrowthTests.Check(steeringRun.Beam.Remaining == remaining && steeringRun.Beam.Direction == direction, "Pause freezes beam direction and life");
        foreach (var clear in new[] { false, true })
        {
            var run = new RunState(HeroKind.Marisa, 42); run.Ranks[(int)ArtKind.Stars] = 0;
            Learn(run, UpgradeCatalog.MasterSparkUnlock);
            if (clear) Learn(run, UpgradeCatalog.SparkClear);
            Target(run, new(400, 0));
            for (var index = 0; index < 50; index++) run.Projectiles.Add(new() { Hostile = true, Position = new(200, 0), Life = 10, Radius = 1 });
            run.Step(default);
            MarisaGrowthTests.Check(run.Projectiles.Count == 50, "Warmup never clears bullets");
            while (run.Beam!.Warmup > 0) run.Step(default);
            run.Step(default);
            MarisaGrowthTests.Check(run.Projectiles.Count == 50 - (clear ? MarisaTuning.BeamClearLimit : 0), "Bullet clearing retains its per-pulse cap");
        }
    }

    public static void Targeting()
    {
        var run = new RunState(HeroKind.Marisa, 42);
        run.Ranks[(int)ArtKind.Stars] = 0;
        Learn(run, UpgradeCatalog.MasterSparkUnlock, UpgradeCatalog.SparkSteer);
        var nearby = Target(run, new(200, 0)); nearby.Kind = EnemyKind.Kedama;
        var boss = Target(run, new(-400, 0)); boss.Kind = EnemyKind.Boss;
        var dead = Target(run, new(0, 10)); dead.Health = 0;
        MarisaBeamSystem.Start(run, false);
        MarisaGrowthTests.Check(run.Beam!.Direction == Vector2.UnitX, "Initial aim uses the nearest living enemy, not enemy mass or movement");
        nearby.Position = new(0, 200);
        MarisaBeamSystem.Step(run);
        MarisaGrowthTests.Check(run.Beam.Direction.Y > 0 && run.Beam.Direction.Y < 0.06f, "Warmup tracks smoothly rather than snapping or needing movement");
        for (var tick = 0; tick < 29; tick++) MarisaBeamSystem.Step(run);
        MarisaGrowthTests.Check(run.Beam.Direction.Y > 0.99f, "Stationary player follows a moving target");
        boss.Position = new(100, 0);
        for (var tick = 0; tick < 30; tick++) MarisaBeamSystem.Step(run);
        MarisaGrowthTests.Check(run.Beam.Direction.X > 0.99f, "Tracks a newly closer enemy instead of retaining the previous target");
        boss.Health = 0;
        for (var tick = 0; tick < 30; tick++) MarisaBeamSystem.Step(run);
        MarisaGrowthTests.Check(run.Beam.Direction.Y > 0.99f, "Target death redirects to the nearest survivor");
        nearby.Position = new(1300, 0);
        var direction = run.Beam.Direction;
        MarisaBeamSystem.Step(run);
        MarisaGrowthTests.Check(run.Beam.Direction == direction, "No target in range keeps the last aim instead of following movement");
        nearby.Position = run.PlayerPosition;
        MarisaBeamSystem.Step(run);
        MarisaGrowthTests.Check(run.Beam.Direction == direction && float.IsFinite(run.Beam.Direction.X), "Coincident target retains a valid heading");
        run.Beam = null; nearby.Position = new(200, 0);
        MarisaBeamSystem.Start(run, false);
        run.Step(new(-Vector2.UnitY));
        MarisaGrowthTests.Check(run.Facing.Y < 0 && run.Beam!.Direction.Y > 0, "Moving away does not turn the beam toward the movement direction");
        Learn(run, UpgradeCatalog.FinalSpark);
        nearby.Position = new(-200, 0);
        MarisaBeamSystem.Start(run, true);
        MarisaGrowthTests.Check(run.Beam is { Signature: true, Steering: true } && run.Beam.Direction.X < -0.99f, "Signature uses the same nearest-target rule");
        nearby.Position = run.PlayerPosition;
        MarisaBeamSystem.Start(run, false);
        MarisaGrowthTests.Check(run.Beam!.Direction == run.Facing, "Starting on a coincident enemy retains a valid facing");
    }

    public static void Resonance()
    {
        var damages = new float[2];
        for (var index = 0; index < 2; index++)
        {
            var run = new RunState(HeroKind.Marisa, 42);
            if (index == 1) Learn(run, UpgradeCatalog.MasterSparkUnlock, UpgradeCatalog.SparkResonance);
            var target = Target(run, new(200, 0));
            run.World.Grid.Rebuild(run.Enemies);
            MarisaProjectileSystem.Cast(run, 1);
            foreach (ref var star in run.Stars.Active) { star.Position = target.Position; star.Velocity = Vector2.Zero; }
            run.Beam = new() { Direction = System.Numerics.Vector2.UnitX, HalfWidth = 50, Length = 600, Remaining = 5 };
            for (var tick = 0; tick < 60; tick++) MarisaProjectileSystem.Step(run);
            damages[index] = target.MaxHealth - target.Health;
            MarisaGrowthTests.Check(run.Stars.All(star => star.Resonating == (index == 1)), "Only learned resonance charges stars in the active beam");
            var life = run.Stars[0].Life;
            run.Beam.Warmup = 1; MarisaProjectileSystem.Step(run);
            MarisaGrowthTests.Check(run.Stars.All(star => !star.Resonating) && run.Stars[0].Life < life, "Warmup cannot resonate or renew star life");
        }
        MarisaGrowthTests.Check(Math.Abs(damages[1] / damages[0] - MarisaTuning.ResonanceMultiplier) < 0.01f, "Resonance has one bounded multiplier, not recursive charging");
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
