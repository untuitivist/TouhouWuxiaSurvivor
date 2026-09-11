using System.Numerics;
using Rebirth.Core;

namespace Rebirth.Tests;

internal static class MarisaGrowthTests
{
    internal static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    internal static void Learn(RunState run, params string[] ids)
    {
        foreach (var id in ids) Check(run.Build.TryApply(UpgradeCatalog.Get(id), 100), "Legal node " + id);
    }

    internal static Enemy Target(RunState run, Vector2 position)
    {
        var target = run.SpawnEnemy(EnemyKind.Elite, position);
        target.Health = target.MaxHealth = 10000;
        target.Speed = target.ContactDamage = 0;
        target.Timer = 10000;
        return target;
    }

    public static void Offers()
    {
        var run = new RunState(HeroKind.Marisa, 42);
        Check(run.Ranks.Sum() == 1 && !run.Build.SignatureUnlocked, "Stars-only starter and locked signature");
        foreach (var id in new[] { UpgradeCatalog.HerbBrew, UpgradeCatalog.SparkSteer, UpgradeCatalog.FinalSpark, "marisa.herbs.refine" })
            Check(!run.Build.CanChoose(UpgradeCatalog.Get(id), 100), "Locked prerequisite " + id);
        Check(!run.Build.CanChoose(UpgradeCatalog.Get(UpgradeCatalog.StarPlanet), 4), "Planet requires level five");
        for (var seed = 0; seed < 100; seed++)
        {
            var offers = UpgradeOffers.Create(run.Build, 3, new(seed));
            Check(offers.Count == 3 && offers.DistinctBy(upgrade => upgrade.Id).Count() == 3, "Three unique choices");
            Check(offers.All(upgrade => run.Build.CanChoose(upgrade, 3)), "Prerequisites and ownership respected");
            Check(offers.Any(upgrade => upgrade.Kind == UpgradeKind.Unlock) && offers.Any(upgrade => upgrade.Kind == UpgradeKind.Behavior), "Unlock and behavior remain visible");
        }
        var reverse = new RunState(HeroKind.Marisa, 42);
        foreach (var candidate in new[] { run, reverse }) Learn(candidate, UpgradeCatalog.HerbsUnlock, UpgradeCatalog.MasterSparkUnlock);
        var nodes = UpgradeCatalog.All.Where(upgrade => upgrade.Owner == HeroKind.Marisa && upgrade.Kind == UpgradeKind.Behavior).Select(upgrade => upgrade.Id).ToArray();
        Learn(run, nodes); Learn(reverse, nodes.Reverse().ToArray());
        Check(run.Build.Traits == reverse.Build.Traits, "All compatible branches commute");
        Check(nodes.All(id => !run.Build.TryApply(UpgradeCatalog.Get(id), 100)), "Branches cannot be applied twice");
    }

    public static void Starter()
    {
        var run = new RunState(HeroKind.Marisa, 42);
        Target(run, new(300, 0));
        for (var index = 0; index < 27; index++)
            run.Projectiles.Add(new() { Hostile = true, Position = Geometry.Angle(index * MathF.Tau / 27) * 25, Life = 2, Radius = 1 });
        run.Step(default);
        Check(run.Stars.Count == 4 && run.Pickups.All(pickup => !pickup.Herbal), "Only gravity stars at start");
        Check(run.SpellCharge >= 100 && run.SpellsCast == 0 && run.Beam == null, "Charge cannot bypass beam unlock");
        Learn(run, UpgradeCatalog.MasterSparkUnlock, UpgradeCatalog.FinalSpark);
        run.Step(default);
        Check(run.SpellsCast == 1 && run.SpellCharge == 0 && run.Beam is { Signature: true, Warmup: > 0 }, "Learned signature activates");
        Check(run.Projectiles.All(projectile => !projectile.Hostile), "Signature keeps its activation clear");
    }

    public static void Mass()
    {
        var totals = new HashSet<float>();
        foreach (var planet in new[] { false, true })
        foreach (var increasedMass in new[] { false, true })
        for (var seed = 0; seed < 200; seed++)
        {
            var first = new Random(seed);
            var second = new Random(seed);
            var total = 0f;
            for (var index = 0; index < 6; index++)
            {
                var mass = MarisaTuning.RollMass(first, increasedMass, planet && index == 0);
                Check(mass == MarisaTuning.RollMass(second, increasedMass, planet && index == 0), "Seed reproduces each independent star mass");
                Check(float.IsFinite(mass) && mass > 0, "Each mass is positive and finite");
                total += mass;
            }
            totals.Add(total);
        }
        Check(totals.Count > 700, "Group totals vary rather than being normalized to a shared pool");
        var baseline = new RunState(HeroKind.Marisa, 42);
        MarisaProjectileSystem.Cast(baseline, Target(baseline, new(200, 0)), 1);
        foreach (var nodes in new[] { new[] { UpgradeCatalog.StarMass }, new[] { UpgradeCatalog.StarSpread }, new[] { UpgradeCatalog.StarPlanet }, new[] { UpgradeCatalog.StarMass, UpgradeCatalog.StarSpread, UpgradeCatalog.StarPlanet } })
        {
            var run = new RunState(HeroKind.Marisa, 42); Learn(run, nodes);
            MarisaProjectileSystem.Cast(run, Target(run, new(200, 0)), 1);
            Check(run.Stars.Count == MarisaTuning.StarCount(run.Build), "Quantity upgrades add real stars");
            for (var index = 0; index < baseline.Stars.Count; index++)
            {
                var star = run.Stars[index];
                if (!star.Planet)
                {
                    var expected = baseline.Stars[index].Mass * (run.Build.Has(AbilityTraits.StarMass) ? MarisaTuning.HeavyMassMultiplier : 1);
                    Check(Math.Abs(star.Mass - expected) < 0.0001f, "Adding stars or a planet never dilutes other stars");
                }
                else Check(star.Mass >= MarisaTuning.MinimumPlanetMass, "Planet rolls its own mass, not a share of the group");
            }
            Check(run.Stars.All(star => star.DamageRate == baseline.Stars[0].DamageRate), "Damage is per star and independent of random mass");
            if (run.Build.Has(AbilityTraits.StarSpread))
                Check(Math.Abs(run.Stars.Sum(star => star.DamageRate) / baseline.Stars.Sum(star => star.DamageRate) - 1.5f) < 0.001f, "Two additional stars contribute two full per-star damage rates");
        }
    }

    public static void Sustain()
    {
        var run = new RunState(HeroKind.Marisa, 42);
        var target = Target(run, new(200, 0));
        run.World.Grid.Rebuild(run.Enemies);
        Check(MarisaProjectileSystem.Cast(run, target, 1), "Cast succeeds");
        foreach (ref var star in run.Stars.Active) { star.Position = target.Position; star.OrbitScale = 0; }
        var mass = run.Stars.Select(star => star.Mass).ToArray();
        var before = target.Health;
        MarisaProjectileSystem.Step(run);
        Check(target.Health == before, "Tearing is timed, not collision damage");
        for (var tick = 0; tick < 60; tick++) MarisaProjectileSystem.Step(run);
        var damage = before - target.Health;
        Check(damage > 20 && damage <= AbilityTuning.Get(ArtKind.Stars, 1).Damage * run.Stars.Count * 1.1f, "Sustained damage adds independent per-star rates");
        Check(run.Stars.Count == 4 && run.Stars.Select(star => star.Mass).SequenceEqual(mass), "Hits neither consume stars nor reroll mass");
        var second = Target(run, new(260, 20));
        target.Health = 0; run.World.Grid.Rebuild(run.Enemies);
        MarisaProjectileSystem.Step(run);
        Check(run.Stars.All(star => star.TargetId == second.Id && star.Life < star.Duration), "Retargets after death without renewing lifetime");
        for (var tick = 0; tick < 300; tick++) MarisaProjectileSystem.Step(run);
        Check(run.Stars.Count == 0, "All stars eventually expire");
    }

    public static void Lifecycle()
    {
        var run = new RunState(HeroKind.Marisa, 42);
        Learn(run, UpgradeCatalog.StarMass, UpgradeCatalog.StarSpread, UpgradeCatalog.StarPlanet, UpgradeCatalog.StarLifetime);
        var target = Target(run, new(200, 0));
        for (var index = 0; index < MarisaTuning.GroupLimit; index++) Check(MarisaProjectileSystem.Cast(run, target, 1), "Whole group fits");
        Check(!MarisaProjectileSystem.Cast(run, target, 1) && run.Stars.Count == 24, "Fifth group rejected atomically");
        Check(run.Stars.Count <= MarisaTuning.StarLimit && run.Stars.All(star => float.IsFinite(star.Mass) && star.Mass > 0), "Entity count is bounded without a global mass pool");
        run.TogglePause(); var stars = run.Stars.ToArray();
        for (var tick = 0; tick < 60; tick++) run.Step(new(Vector2.UnitY));
        Check(run.Stars.SequenceEqual(stars), "Pause freezes every star component");
        run.TogglePause(); run.AddExperience(run.NextLevelExperience); run.Step(default);
        stars = run.Stars.ToArray();
        for (var tick = 0; tick < 60; tick++) run.Step(default);
        Check(run.Phase == RunPhase.Choosing && run.Stars.SequenceEqual(stars), "Choices also freeze star simulation");
        var fresh = new RunState(HeroKind.Marisa, 42);
        Check(fresh.Stars.Count == 0 && fresh.Marisa.PendingHealing == 0, "Restart has independent component storage");
    }

    public static void CrowdedTarget()
    {
        var run = new RunState(HeroKind.Marisa, 42);
        for (var index = 0; index < 20; index++) Target(run, new(200, 0));
        var boss = Target(run, new(200, 0)); boss.Kind = EnemyKind.Boss;
        run.World.Grid.Rebuild(run.Enemies);
        run.Stars.Add(new() { Position = boss.Position, Anchor = boss.Position, TargetId = boss.Id, Mass = 12, Planet = true, Life = 2, DamageRate = 100 });
        MarisaProjectileSystem.Step(run);
        Check(boss.Health < boss.MaxHealth, "Dense fodder cannot consume the intended target damage slot");
        Check(run.Enemies.Count(enemy => enemy.Health < enemy.MaxHealth) == MarisaTuning.StarTargetLimit, "Primary targeting still respects the per-star target budget");
    }

    public static void Targeting()
    {
        var run = new RunState(HeroKind.Marisa, 42);
        var nearby = Target(run, new(180, 0)); nearby.Kind = EnemyKind.Kedama;
        var boss = Target(run, new(350, 0)); boss.Kind = EnemyKind.Boss;
        run.World.Grid.Rebuild(run.Enemies);
        Check(MarisaProjectileSystem.FindAttractor(run, Vector2.Zero, 650) == boss, "Heavier enemies attract stars without nearer fodder starving the boss fight");
        boss.Health = 0;
        Check(MarisaProjectileSystem.FindAttractor(run, Vector2.Zero, 650) == nearby, "A same-step beam kill cannot remain the star attractor");
        boss.Health = boss.MaxHealth;
        boss.Position = new(700, 0); run.World.Grid.Rebuild(run.Enemies);
        Check(MarisaProjectileSystem.FindAttractor(run, Vector2.Zero, 650) == nearby, "Mass priority never bypasses acquisition range");
    }

    public static void Planet()
    {
        var run = new RunState(HeroKind.Marisa, 42);
        var enemy = Target(run, new(200, 0)); enemy.Kind = EnemyKind.Kedama;
        var boss = Target(run, new(200, 0)); boss.Kind = EnemyKind.Boss;
        run.Stars.Add(new() { Position = new(260, 0), Anchor = new(260, 0), Mass = 12, Planet = true, Life = 3, GroupId = 1 });
        run.World.Grid.Rebuild(run.Enemies);
        MarisaProjectileSystem.Step(run);
        Check(enemy.GravityVelocity.X > 0 && boss.GravityVelocity.X > 0 && boss.GravityVelocity.X < enemy.GravityVelocity.X * 0.2f, "Planet attracts many enemies; boss resists");
        var previous = enemy.Position;
        EnemySystem.Step(run);
        Check(enemy.Position.X > previous.X && boss.Timer < 10000, "Force affects movement without freezing boss AI");
        for (var index = 0; index < 10; index++) run.Stars.Add(run.Stars[0]);
        MarisaProjectileSystem.Step(run);
        Check(enemy.GravityVelocity.Length() <= MarisaTuning.PullSpeed + 0.001f, "Overlapping planets have a global pull cap");
        run.Stars.Clear(); MarisaProjectileSystem.Step(run);
        Check(enemy.GravityVelocity == Vector2.Zero, "Expired planets leave no permanent force");
    }

    public static void Language()
    {
        GameText.SetLanguage("en");
        try
        {
            var build = new BuildState(HeroKind.Marisa);
            foreach (var node in UpgradeCatalog.All.Where(upgrade => upgrade.Owner == HeroKind.Marisa))
            foreach (var text in new[] { node.Name, node.Description, UpgradeCatalog.Requirement(node), UpgradeCatalog.Progress(node, build), UpgradeCatalog.Description(node, build) })
                Check(!GameText.Get(text).Any(character => character is >= '㐀' and <= '鿿'), "Untranslated Marisa text: " + text);
        }
        finally { GameText.SetLanguage("zh"); }
    }
}
