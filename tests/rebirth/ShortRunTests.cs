using System.Numerics;
using Rebirth.Core;
using static Rebirth.Tests.MarisaGrowthTests;

namespace Rebirth.Tests;

internal static class ShortRunTests
{
    private static void Check(bool condition, string message) => MarisaGrowthTests.Check(condition, message);
    public static void Pacing()
    {
        Check(RunState.BossArrival == 240, "Four minutes of growth leave about one minute for the duel");
        Check(RunPacing.ExperienceFor(1) == 17 && RunPacing.ExperienceFor(10) == 80, "Fast early gains leave a finite budget for the stronger short-run upgrades");
        Check(RunPacing.ExperienceFor(int.MaxValue) == int.MaxValue, "Continuation XP saturates without overflow");
        Check(new[] { 0, 60, 120, 180 }.Select(seconds => RunPacing.At(seconds).Name).Distinct().Count() == 4, "All four pressure stages occur before the Boss");
        foreach (var hero in Enum.GetValues<HeroKind>())
        {
            var total = UpgradeCatalog.All.Where(upgrade => (upgrade.Owner == hero || upgrade.Owner == null)
                && upgrade.MaxRank != int.MaxValue && upgrade.Kind != UpgradeKind.Recovery).Sum(upgrade => upgrade.MaxRank);
            Check(total > 26, "A short run cannot complete every finite upgrade; no route lock is needed");
        }
    }

    public static void PerceptibleGrowth()
    {
        foreach (var ability in ArtCatalog.All.Where(art => art.Owner.HasValue))
        for (var rank = 1; rank < ability.MaxRank; rank++)
        {
            var before = AbilityTuning.Get(ability.Id, rank);
            var after = AbilityTuning.Get(ability.Id, rank + 1);
            var visible = after.Count > before.Count || ability.Id == ArtKind.Boundary && after.Range - before.Range >= 25
                || ability.Id == ArtKind.MasterSpark && AbilityTuning.BeamHalfWidth(rank + 1) - AbilityTuning.BeamHalfWidth(rank) >= 8;
            Check(visible || after.Damage >= before.Damage * 1.25f, "Every base rank adds a visible form change or at least 25% base potency");
            Check(ArtCatalog.UpgradeText(ability.Id, rank).Contains("→"), "Cards show before/after values");
        }
        for (var rank = 0; rank < 20; rank++)
            Check(Math.Abs(MainlineGrowth.Strength(rank + 1) - MainlineGrowth.Strength(rank) - 0.5f) < 0.001f, "Each potency rank adds half of base damage, not a square-root sliver");
        var run = new RunState(HeroKind.Reimu, 42);
        Learn(run, MainlineGrowth.OfudaPower);
        Target(run, new(300, 0));
        run.Step(default);
        Check(run.Projectiles.All(projectile => projectile.Damage == AbilityTuning.Get(ArtKind.Ofuda, 1).Damage * 1.5f), "The displayed potency changes actual emitted shots");
        Check(run.Build.CanChoose(UpgradeCatalog.Get(UpgradeCatalog.BoundaryUnlock), 100), "Stronger growth does not close other directions");
    }

    public static void BossPatterns()
    {
        foreach (var hero in Enum.GetValues<HeroKind>())
        for (var phase = 0; phase < 3; phase++)
        {
            var run = new RunState(hero, 42);
            var boss = run.SpawnEnemy(EnemyKind.Boss, new(300, 0));
            var state = boss.Abilities!;
            state.Phase = phase;
            boss.Health = boss.MaxHealth * (phase == 0 ? 1 : phase == 1 ? 0.5f : 0.2f);
            state.SpecialCooldown = 10000;
            state.ShotCooldown = 0;
            var health = run.Health;
            BossAbilitySystem.Step(run, boss, -Vector2.UnitX, 300);
            var volley = state.Volley!;
            Check(volley.Warmup > 0.9f && run.Projectiles.Count == 0, "Each authored ring starts with a non-damaging warning");
            for (var tick = 0; tick < 50; tick++) BossAbilitySystem.Step(run, boss, Vector2.UnitY, 300);
            Check(run.Health == health && run.Projectiles.Count == 0, "Changing aim during windup cannot fire early");
            boss.Position += new Vector2(80, 80);
            for (var tick = 0; tick < 100; tick++) BossAbilitySystem.Step(run, boss, Vector2.UnitY, 300);
            Check(run.Stars.Count == 0 && run.Projectiles.Count > 0, "Boss stars are straight danmaku, never gravity bodies");
            Check(run.Projectiles.Count <= BossAbilitySystem.ProjectileBudget, "Danmaku stays inside its independent budget");
            foreach (var projectile in run.Projectiles)
            {
                Check(projectile.OwnerId == boss.Id && projectile.Hostile && projectile.TurnRate == 0 && !projectile.Blast && !projectile.DreamOrb, "Boss projectiles never inherit player homing or behavior upgrades");
                Check(Vector2.Distance(projectile.Position, volley.Origin) < 24.01f, "Launch origin remains at the warning snapshot even if the Boss moves");
                var angle = MathF.Atan2(projectile.Velocity.Y, projectile.Velocity.X);
                Check(!volley.IsGap(angle), "Every wave preserves both announced corridors");
                var velocity = Geometry.Direction(projectile.Velocity);
                for (var corridor = 0; corridor < 2; corridor++)
                for (var side = -1; side <= 1; side++)
                for (var distance = BossVolleyState.WarningStart; distance <= 700; distance += 40)
                {
                    var safe = volley.Origin + Geometry.Angle(volley.GapAngle + corridor * MathF.PI + side * BossVolleyState.WarningHalfAngle) * distance;
                    Check(Geometry.SegmentDistanceSquared(safe, projectile.Position, projectile.Position + velocity * 1400) > MathF.Pow(projectile.Radius + 5, 2), "The visible corridor is wider than the real collision radius");
                }
            }
            var velocities = run.Projectiles.Select(projectile => projectile.Velocity).ToArray();
            ProjectileSystem.Step(run);
            Check(run.Projectiles.Select(projectile => projectile.Velocity).SequenceEqual(velocities), "Moving the player after launch never bends a Boss shot");
            run.Projectiles.Add(new() { Position = new(0, 200), Velocity = Vector2.UnitY, Life = 8 });
            state.Phase = -1;
            BossAbilitySystem.Step(run, boss, Vector2.UnitY, 300);
            Check(run.Projectiles.All(projectile => !projectile.Hostile || projectile.Life <= 0) && run.Projectiles.Last().Life == 8, "Phase transitions clear only owned danger and leave a breathing window");
        }
        BeamSnapshots();
    }

    private static void BeamSnapshots()
    {
        foreach (var phase in new[] { 1, 2 })
        {
            var run = new RunState(HeroKind.Reimu, 42);
            var boss = run.SpawnEnemy(EnemyKind.Boss, new(300, 0));
            var state = boss.Abilities!;
            state.Phase = phase;
            boss.Health = boss.MaxHealth * (phase == 1 ? 0.5f : 0.2f);
            state.SpecialCooldown = 0;
            BossAbilitySystem.Step(run, boss, -Vector2.UnitX, 300);
            var beam = state.Beam!;
            Check(beam.Warmup > 1.4f && !beam.Contains(run.PlayerPosition, 5), "Beam lanes give 1.5 seconds of harmless warning");
            Check(beam.Count == (phase == 2 ? 3 : 1), "Later phase uses three separated fixed lanes");
            var origin = beam.Origin;
            var bearings = Enumerable.Range(0, beam.Count).Select(beam.Bearing).ToArray();
            boss.Position += new Vector2(100, 100);
            run.TogglePause();
            var elapsed = state.Elapsed;
            run.Step(new(Vector2.UnitY));
            Check(state.Elapsed == elapsed, "Pause freezes Boss choreography");
            run.TogglePause();
            for (var tick = 0; tick < 95; tick++) BossAbilitySystem.Step(run, boss, Vector2.UnitY, 300);
            Check(beam.Origin == origin && Enumerable.Range(0, beam.Count).Select(beam.Bearing).SequenceEqual(bearings), "Neither origin nor direction follows the Boss or player after warning starts");
            Check(beam.Contains(origin + beam.Direction * 250), "Announced lane becomes dangerous after its warning");
            Check(!beam.Contains(origin + Geometry.Rotate(beam.Direction, 0.3f) * 250, 5), "Gap between final beam lanes remains walkable");
            for (var tick = 0; tick < 105; tick++) BossAbilitySystem.Step(run, boss, Vector2.UnitY, 300);
            Check(state.Beam == null && state.ShotCooldown > 0, "Beam ends before the next attack; no endless tracking or healing");
        }
    }
}
