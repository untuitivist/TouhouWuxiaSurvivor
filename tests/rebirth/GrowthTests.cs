using System.Numerics;
using Rebirth.Core;

namespace Rebirth.Tests;

internal static class GrowthTests
{
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Learn(RunState run, params string[] nodes)
    {
        foreach (var node in nodes) Check(run.Build.TryApply(UpgradeCatalog.Get(node), 100), "Learn " + node);
    }

    private static Enemy Target(RunState run, Vector2 position, EnemyKind kind = EnemyKind.Elite)
    {
        var enemy = run.SpawnEnemy(kind, position);
        enemy.Health = enemy.MaxHealth = 10000;
        enemy.Speed = 0;
        enemy.Timer = 100;
        return enemy;
    }

    public static void Offers()
    {
        foreach (var hero in Enum.GetValues<HeroKind>())
        for (var seed = 0; seed < 32; seed++)
        {
            var build = new BuildState(hero);
            var random = new Random(seed);
            for (var level = 2; level < 55; level++)
            {
                var offers = UpgradeOffers.Create(build, level, random);
                Check(offers.Count is > 0 and <= 3 && offers.Distinct().Count() == offers.Count, "Unique bounded offers");
                Check(offers.All(upgrade => build.CanChoose(upgrade, level)), "All prerequisites satisfied");
                if (hero == HeroKind.Reimu && level == 2)
                    Check(offers.Any(upgrade => upgrade.Kind == UpgradeKind.Unlock) && offers.All(upgrade => upgrade.Kind != UpgradeKind.Behavior), "Early unlock, no premature behaviors");
                Check(build.TryApply(offers[random.Next(offers.Count)], level), "Offer applies");
            }
        }
        var fresh = new BuildState(HeroKind.Reimu);
        Check(!fresh.CanChoose(UpgradeCatalog.Get(UpgradeCatalog.Clear), 100), "No orphan upgrade");
        Check(!fresh.CanChoose(UpgradeCatalog.Get(UpgradeCatalog.Homing), 2), "Minimum level enforced");
    }

    public static void Combinations()
    {
        foreach (var route in new[] {
            new[] { UpgradeCatalog.Homing, UpgradeCatalog.Blast },
            new[] { UpgradeCatalog.Cluster, UpgradeCatalog.Bind },
            new[] { UpgradeCatalog.Clear, UpgradeCatalog.Launch } })
        {
            var forward = new RunState(HeroKind.Reimu, 42);
            var reverse = new RunState(HeroKind.Reimu, 42);
            foreach (var run in new[] { forward, reverse }) Learn(run, UpgradeCatalog.BoundaryUnlock, UpgradeCatalog.YinYangUnlock);
            Learn(forward, route);
            Learn(reverse, route.Reverse().ToArray());
            Check(forward.Build.Traits == reverse.Build.Traits && forward.Ranks.SequenceEqual(reverse.Ranks), "Combination order independent");
            Check(!forward.Build.TryApply(UpgradeCatalog.Get(route[0]), 100), "No repeat node");
            Check(forward.Build.AllocatedPoints == 4, "Behavior spends one point, not an ability rank");
        }
        Check(!new BuildState(HeroKind.Marisa).TryApply(UpgradeCatalog.Get(UpgradeCatalog.Homing), 100), "Cross-hero rejection");
    }

    public static void Basic()
    {
        var run = new RunState(HeroKind.Reimu, 42);
        Target(run, new(400, 0));
        for (var index = 0; index < 20; index++)
            run.Projectiles.Add(new() { Position = Geometry.Angle(index * MathF.Tau / 20) * 25, Hostile = true, Radius = 1, Life = 2 });
        run.Step(default);
        Check(run.SpellCharge == 100 && run.SpellsCast == 0, "Locked signature cannot clear or attack");
        Check(run.Field == null && ReimuAbilitySystem.OrbitCount(run) == 0 && run.Ranks.Sum() == 1, "Only basic ofuda at start");
        Check(run.Projectiles.Where(projectile => !projectile.Hostile).All(projectile => projectile.Art == ArtKind.Ofuda && projectile.TurnRate == 0 && !projectile.Blast), "Straight unmodified shots");
    }

    public static void StraightAim()
    {
        for (var rank = 1; rank <= 5; rank++)
        {
            var run = new RunState(HeroKind.Reimu, 42);
            var refine = UpgradeCatalog.All.Single(upgrade => upgrade.Ability == ArtKind.Ofuda && upgrade.Kind == UpgradeKind.Refine);
            for (var point = 1; point < rank; point++) Check(run.Build.TryApply(refine, 100), "Legal straight refinement");
            var target = Target(run, new(400, 0));
            run.Step(default);
            Check(run.Projectiles.Count == AbilityTuning.Get(ArtKind.Ofuda, rank).Count && run.Projectiles.Any(projectile => projectile.Velocity.Y == 0), "Every rank preserves a aimed shot without increasing count");
            for (var tick = 0; tick < 60; tick++) run.Step(default);
            Check(target.Health < target.MaxHealth, "Straight ofuda hits a stationary distant target without homing");
        }
    }

    public static void Blast()
    {
        var run = new RunState(HeroKind.Reimu, 42);
        Learn(run, UpgradeCatalog.Homing, UpgradeCatalog.Blast);
        var direct = Target(run, new(100, 0));
        var nearby = Target(run, new(150, 0));
        var outside = Target(run, new(300, 0));
        run.Step(default);
        Check(run.Projectiles.All(projectile => projectile.TurnRate > 0 && projectile.Blast), "Both traits captured at launch");
        Check(run.Projectiles.All(projectile => Math.Abs(projectile.Damage - AbilityTuning.Get(ArtKind.Ofuda, 1).Damage * ReimuTuning.HomingDamageMultiplier) < 0.01f), "Homing damage cost is applied exactly once");
        run.Projectiles.Clear();
        run.Projectiles.Add(new() { Art = ArtKind.Ofuda, Position = direct.Position, Radius = 1, Life = 1, Damage = 20, Blast = true });
        run.Step(default);
        Check(Math.Abs(direct.Health - (10000 - 20)) < 0.01f && Math.Abs(nearby.Health - (10000 - 20 * ReimuTuning.BlastMultiplier)) < 0.01f, "Direct hit is not damaged twice; nearby receives one splash");
        Check(outside.Health == 10000 && run.Events.Count(entry => entry.Kind == EffectKind.Explosion) == 1, "No recursive explosions or distant damage");
        run.Projectiles.Clear();
        run.Enemies.Clear();
        direct = Target(run, new(100, 0));
        var crowd = Enumerable.Range(0, 10).Select(index => Target(run, new(120 + index, 0))).ToArray();
        run.Projectiles.Add(new() { Art = ArtKind.Ofuda, Position = direct.Position, Radius = 1, Life = 1, Damage = 20, Blast = true });
        run.Step(default);
        Check(crowd.Count(enemy => enemy.Health < enemy.MaxHealth) == ReimuTuning.BlastTargetLimit, "Splash target cap prevents unbounded dense-group scaling");
    }

    public static void Boundary()
    {
        var run = new RunState(HeroKind.Reimu, 42);
        Learn(run, UpgradeCatalog.BoundaryUnlock, UpgradeCatalog.Cluster, UpgradeCatalog.Bind);
        Target(run, new(120, 0));
        for (var index = 0; index < 5; index++) Target(run, new(400 + index * 10, 0));
        var boss = Target(run, new(425, 0), EnemyKind.Boss);
        run.Step(default);
        Check(run.Field != null && run.Field.Position.X >= 400 && boss.BoundRemaining > 0, "Dense remote group receives binding field");
        boss.Speed = 100;
        boss.Timer = 0;
        var before = boss.Position;
        var bound = run.Enemies.First(enemy => enemy.Kind == EnemyKind.Elite && enemy.Position.X >= 400);
        bound.Speed = 100;
        var boundBefore = bound.Position;
        run.Step(default);
        Check(Math.Abs(Vector2.Distance(before, boss.Position) - 100 * RunState.StepSeconds * ReimuTuning.BossSlowMultiplier) < 0.01f, "Boss slowed rather than rooted");
        Check(run.Projectiles.Any(projectile => projectile.Hostile) && boss.Timer > 0, "Boss still attacks");
        Check(bound.Position == boundBefore, "Normal enemy rooted");
    }

    public static void Orbit()
    {
        var run = new RunState(HeroKind.Reimu, 42);
        Learn(run, UpgradeCatalog.YinYangUnlock, UpgradeCatalog.Launch, UpgradeCatalog.Clear);
        Target(run, new(700, 0));
        run.Step(default);
        Check(run.Reimu.Charging && ReimuAbilitySystem.OrbitCount(run) == 2, "Charging keeps both guards");
        var charge = run.Reimu.ChargeRemaining;
        run.TogglePause();
        for (var index = 0; index < 60; index++) run.Step(default);
        Check(run.Reimu.ChargeRemaining == charge, "Pause freezes charge");
        run.TogglePause();
        for (var index = 0; index < 50; index++) run.Step(default);
        var launched = run.Projectiles.Single(projectile => projectile.Art == ArtKind.YinYang);
        Check(launched.Pierce == 2 && launched.ClearBudget == ReimuTuning.ClearLimit && ReimuAbilitySystem.OrbitCount(run) == 1, "One launched guard with three-target pierce and finite clear");
        for (var index = 0; index < 90; index++) run.Step(default);
        Check(ReimuAbilitySystem.OrbitCount(run) == 2, "Guard restored after flight");
    }

    public static void Clear()
    {
        foreach (var reverse in new[] { false, true })
        {
            var run = new RunState(HeroKind.Reimu, 42);
            var shield = new Projectile { Art = ArtKind.YinYang, Position = Vector2.Zero, Life = 1, ClearBudget = 3 };
            if (!reverse) run.Projectiles.Add(shield);
            run.Projectiles.Add(new() { Hostile = true, Position = Vector2.Zero, Radius = 2, Life = 1, Damage = 50 });
            if (reverse) run.Projectiles.Add(shield);
            run.Step(default);
            Check(run.Health == run.MaxHealth && run.Projectiles.All(projectile => !projectile.Hostile), "Clear before player collision regardless of insertion order");
        }
        var orbit = new RunState(HeroKind.Reimu, 42);
        Learn(orbit, UpgradeCatalog.YinYangUnlock, UpgradeCatalog.Clear);
        var position = new Vector2(orbit.OrbitRadius, 0);
        for (var index = 0; index < 8; index++) orbit.Projectiles.Add(new() { Hostile = true, Position = position, Life = 1, Radius = 2 });
        orbit.Step(default);
        Check(orbit.Projectiles.Count(projectile => projectile.Hostile) == 5, "Shared per-pulse budget across orbiting guards");
    }
}
