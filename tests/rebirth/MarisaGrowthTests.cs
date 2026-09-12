using System.Numerics;
using Rebirth.Core;

namespace Rebirth.Tests;

internal static class MarisaGrowthTests
{
    public static void ScalarGravityReference()
    {
        var random = new Random(1708);
        for (var index = 0; index < 2048; index++)
        {
            var offset = new Vector2(random.NextSingle() * 2000 - 1000, random.NextSingle() * 2000 - 1000);
            var distanceSquared = offset.LengthSquared();
            var softened = distanceSquared + MarisaTuning.GravitySoftening * MarisaTuning.GravitySoftening;
            var falloff = 1 - distanceSquared / (MarisaTuning.TargetRange * MarisaTuning.TargetRange);
            var expected = distanceSquared >= MarisaTuning.TargetRange * MarisaTuning.TargetRange ? Vector2.Zero
                : offset * (MarisaTuning.GravityStrength * falloff * falloff / (softened * MathF.Sqrt(softened)));
            Check(Geometry.DistanceSquared(StarGravitySystem.AccelerationPerMass(offset), expected) < 0.000001f, "Scalar force matches independent vector reference");
            var velocity = new Vector2(random.NextSingle() * 1000 - 500, random.NextSingle() * 1000 - 500);
            var acceleration = offset * 20;
            var limited = acceleration.LengthSquared() > MarisaTuning.GravityAccelerationLimit * MarisaTuning.GravityAccelerationLimit
                ? Vector2.Normalize(acceleration) * MarisaTuning.GravityAccelerationLimit : acceleration;
            var integrated = (velocity + limited * RunState.StepSeconds) / (1 + MarisaTuning.StarDrag * RunState.StepSeconds);
            if (integrated.LengthSquared() > MarisaTuning.StarVelocityLimit * MarisaTuning.StarVelocityLimit)
                integrated = Vector2.Normalize(integrated) * MarisaTuning.StarVelocityLimit;
            Check(Geometry.DistanceSquared(MarisaTuning.IntegrateGravity(velocity, acceleration, MarisaTuning.StarDrag, MarisaTuning.StarVelocityLimit), integrated) < 0.00001f, "Scalar damping and limits match vector reference");
        }
    }

    public static void CosmeticIsolation()
    {
        var first = new RunState(HeroKind.Marisa, 42);
        var second = new RunState(HeroKind.Marisa, 42);
        for (var index = 0; index < 37; index++) second.Marisa.VisualRandom.Next();
        for (var index = 0; index < 30; index++)
        {
            MarisaProjectileSystem.Cast(first, 1);
            MarisaProjectileSystem.Cast(second, 1);
            Check(first.Stars[index].Mass == second.Stars[index].Mass && first.Stars[index].DamageRate == second.Stars[index].DamageRate, "Cosmetic randomness cannot change mass or DPS");
        }
        Check(first.Stars.Any(star => star.VisualSeed != second.Stars[0].VisualSeed), "Stars receive distinct cosmetic seeds");
        var visuals = first.Stars.Select(star => star.VisualSeed).ToArray();
        for (var tick = 0; tick < 60; tick++) MarisaProjectileSystem.Step(first);
        Check(first.Stars.Select(star => star.VisualSeed).SequenceEqual(visuals), "Each star keeps its original color throughout movement");
    }

    internal static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    internal static void Learn(RunState run, params string[] ids)
    {
        foreach (var id in ids) Check(run.Build.TryApply(UpgradeCatalog.Get(id), 100), "Legal node " + id);
    }

    internal static Enemy Target(RunState run, Vector2 position, EnemyKind kind = EnemyKind.Elite)
    {
        var target = run.SpawnEnemy(kind, position);
        target.Health = target.MaxHealth = 10000;
        target.Speed = target.ContactDamage = 0;
        target.Timer = 10000;
        target.Abilities = null;
        return target;
    }

    public static void Offers()
    {
        var run = new RunState(HeroKind.Marisa, 42);
        Check(run.Ranks.Sum() == 1 && !run.Build.SignatureUnlocked, "Stars-only starter and locked signature");
        foreach (var id in new[] { UpgradeCatalog.HerbBrew, UpgradeCatalog.SparkSteer, UpgradeCatalog.FinalSpark, "marisa.herbs.refine" })
            Check(!run.Build.CanChoose(UpgradeCatalog.Get(id), 100), "Locked prerequisite " + id);
        Check(!UpgradeCatalog.All.Any(upgrade => upgrade.Id == "marisa.stars.planet"), "No guaranteed-planet upgrade remains");
        for (var seed = 0; seed < 100; seed++)
        {
            var offers = UpgradeOffers.Create(run.Build, 3, new(seed));
            Check(offers.Count == 3 && offers.DistinctBy(upgrade => upgrade.Id).Count() == 3, "Three unique choices");
            Check(offers.All(upgrade => run.Build.CanChoose(upgrade, 3)), "Prerequisites and ownership respected");
            Check(offers.Any(upgrade => upgrade.Kind == UpgradeKind.Unlock) && offers.Any(upgrade => upgrade.Ability == ArtKind.Stars), "Unlock and primary growth remain visible before the stance choice");
        }
        var reverse = new RunState(HeroKind.Marisa, 42);
        foreach (var candidate in new[] { run, reverse }) Learn(candidate, UpgradeCatalog.HerbsUnlock, UpgradeCatalog.MasterSparkUnlock);
        var nodes = UpgradeCatalog.All.Where(upgrade => upgrade.Owner == HeroKind.Marisa && upgrade.Kind is UpgradeKind.Behavior or UpgradeKind.Training).Select(upgrade => upgrade.Id).ToArray();
        Learn(run, nodes); Learn(reverse, nodes.Reverse().ToArray());
        Check(run.Build.Traits == reverse.Build.Traits, "Compatible branches commute");
        foreach (var id in nodes)
        {
            var node = UpgradeCatalog.Get(id);
            Check(run.Build.TryApply(node, 100) == (node.Kind == UpgradeKind.Training), "Only distribution and lifetime training repeat");
        }
        Check(run.Build.TrainingRank(UpgradeCatalog.StarMass) == 2 && run.Ranks[(int)ArtKind.Stars] == 1, "Distribution training is independent of damage rank");
    }

    public static void Starter()
    {
        var run = new RunState(HeroKind.Marisa, 42);
        Target(run, new(300, 0));
        for (var index = 0; index < 27; index++)
            run.Projectiles.Add(new() { Hostile = true, Position = Geometry.Angle(index * MathF.Tau / 27) * 25, Life = 2, Radius = 1 });
        run.Step(default);
        Check(run.Stars.Count == 1 && run.Pickups.All(pickup => !pickup.Herbal), "One independently emitted star, no group cast");
        Check(run.SpellCharge >= 100 && run.SpellsCast == 0 && run.Beam == null, "Charge cannot bypass beam unlock");
        Learn(run, UpgradeCatalog.MasterSparkUnlock, UpgradeCatalog.FinalSpark);
        run.Step(default);
        Check(run.SpellsCast == 1 && run.SpellCharge == 0 && run.Beam is { Signature: true, Warmup: > 0 }, "Learned signature activates");
        Check(run.Projectiles.All(projectile => !projectile.Hostile), "Signature keeps its activation clear");
    }

    public static void Emission()
    {
        var run = new RunState(HeroKind.Marisa, 42);
        for (var tick = 0; tick < 120; tick++)
        {
            var previous = run.Stars.Count;
            MarisaAbilitySystem.Step(run);
            Check(run.Stars.Count - previous is >= 0 and <= 1, "Output is one star at a time, never a simultaneous ring or clump");
        }
        Check(run.Stars.Count is >= 10 and <= 12, "Small emissions repeat throughout the two-second window");
        Check(run.Stars.Select(star => (int)(star.Rotation / (MathF.PI / 2))).Distinct().Count() == 4, "Emission covers all four quadrants without an enemy target");
        Check(run.Stars.All(star => star.Duration == MarisaTuning.StarLifetime), "Each star owns its lifetime");
    }

    public static void Mass()
    {
        var baseline = new BuildState(HeroKind.Marisa);
        var grown = new BuildState(HeroKind.Marisa);
        for (var index = 0; index < 36; index++) grown.TryApply(UpgradeCatalog.Get(UpgradeCatalog.StarMass), 100);
        for (var index = 0; index < 4; index++) grown.TryApply(UpgradeCatalog.Get(UpgradeCatalog.StarSpread), 100);
        var first = new Random(42); var replay = new Random(42); var trained = new Random(42);
        var original = new List<float>(); var upgraded = new List<float>();
        for (var index = 0; index < 10000; index++)
        {
            var mass = MarisaTuning.RollMass(first, baseline);
            Check(mass == MarisaTuning.RollMass(replay, baseline), "Mass samples reproduce for the same seed");
            Check(float.IsFinite(mass) && mass > 0, "Every sample has positive finite mass");
            original.Add(mass); upgraded.Add(MarisaTuning.RollMass(trained, grown));
        }
        Check(original.Take(64).All(mass => mass < EnemyMassCatalog.Elite), "No slot or quota forces a large star");
        Check(original.Chunk(4).Select(group => group.Sum()).Distinct().Count() > 2400, "No total mass pool or normalization");
        var expectedMean = MarisaTuning.MassMedian(grown) * MathF.Exp(MarisaTuning.MassSigma(grown) * MarisaTuning.MassSigma(grown) / 2);
        Check(Math.Abs(upgraded.Average() / expectedMean - 1) < 0.08f && upgraded.Any(mass => mass > EnemyMassCatalog.Boss), "Extended training follows its actual distribution and can produce boss-scale stars");
        Check(upgraded.Count(mass => mass > EnemyMassCatalog.Boss) is > 0 and < 2000, "Boss-scale stars remain a rare sample, not a guarantee");
        var run = new RunState(HeroKind.Marisa, 42);
        MarisaProjectileSystem.Cast(run, 1);
        var previous = run.Stars[0];
        Learn(run, UpgradeCatalog.StarMass, UpgradeCatalog.StarLifetime, UpgradeCatalog.StarLifetime);
        MarisaProjectileSystem.Cast(run, 1);
        Check(run.Stars[0].Equals(previous), "Growth neither rerolls nor renews existing stars");
        Check(run.Stars[1].Duration > previous.Duration && Math.Abs(run.Stars[1].DamageRate / run.Stars[1].Mass - previous.DamageRate / previous.Mass) < 0.0001f, "Lifetime training is separate; mass scales physical tearing without sharing a damage pool");
    }

    public static void Sustain()
    {
        var run = new RunState(HeroKind.Marisa, 42);
        var target = Target(run, new(200, 0));
        run.World.Grid.Rebuild(run.Enemies);
        MarisaProjectileSystem.Cast(run, 1);
        ref var star = ref run.Stars.Active[0]; star.Position = target.Position; star.Velocity = Vector2.Zero;
        var original = star;
        MarisaProjectileSystem.Step(run);
        Check(target.Health == target.MaxHealth, "Tearing is timed, not collision damage");
        for (var tick = 0; tick < 60; tick++) MarisaProjectileSystem.Step(run);
        var damage = target.MaxHealth - target.Health;
        Check(damage > original.DamageRate * 0.8f && damage < original.DamageRate * 1.2f, "Frequent small pulses yield the per-star DPS");
        Check(run.Stars.Count == 1 && run.Stars[0].Mass == original.Mass, "Hits neither consume stars nor reroll mass");
        target.Health = 0;
        var second = Target(run, run.Stars[0].Position);
        run.World.Grid.Rebuild(run.Enemies);
        for (var tick = 0; tick < 10; tick++) MarisaProjectileSystem.Step(run);
        Check(second.Health < second.MaxHealth, "A star tears new nearby enemies without a target ID");
        for (var tick = 0; tick < 420; tick++) MarisaProjectileSystem.Step(run);
        Check(run.Stars.Count == 0, "All stars expire without target-based life renewal");
    }

    public static void Lifecycle()
    {
        var run = new RunState(HeroKind.Marisa, 42);
        for (var index = 0; index < MarisaTuning.StarLimit; index++) Check(MarisaProjectileSystem.Cast(run, 1), "One star fits");
        Check(!MarisaProjectileSystem.Cast(run, 1), "Finite storage rejects an extra star without a group budget");
        for (var tick = 0; tick < 60; tick++) MarisaAbilitySystem.Step(run);
        Check(run.Marisa.ShotCooldown >= 0, "Saturated storage never accumulates a catch-up salvo");
        run.TogglePause(); var stars = run.Stars.ToArray();
        for (var tick = 0; tick < 60; tick++) run.Step(new(Vector2.UnitY));
        Check(run.Stars.SequenceEqual(stars), "Pause freezes all star components");
        run.TogglePause(); run.AddExperience(run.NextLevelExperience); run.Step(default);
        stars = run.Stars.ToArray();
        for (var tick = 0; tick < 60; tick++) run.Step(default);
        Check(run.Phase == RunPhase.Choosing && run.Stars.SequenceEqual(stars), "Upgrade choices freeze physics");
        Check(new RunState(HeroKind.Marisa, 42).Stars.Count == 0, "Restart has separate component storage");
    }

    public static void CrowdedTarget()
    {
        var run = new RunState(HeroKind.Marisa, 42);
        for (var index = 0; index < 20; index++) Target(run, new(200, 0), EnemyKind.Kedama);
        var boss = Target(run, new(200, 0), EnemyKind.Boss);
        run.World.Grid.Rebuild(run.Enemies);
        run.Stars.Add(new() { Position = boss.Position, Mass = 12, Life = 2, DamageRate = 10 });
        MarisaProjectileSystem.Step(run);
        Check(run.Enemies.All(enemy => enemy.Health < enemy.MaxHealth), "Every nearby enemy participates, without a primary-target reservation");
    }

    public static void Targeting()
    {
        var run = new RunState(HeroKind.Marisa, 42);
        var first = Target(run, new(120, 40), EnemyKind.Fairy);
        var second = Target(run, new(-90, -70), EnemyKind.Elite);
        run.Stars.Add(new() { Position = Vector2.Zero, Mass = 3, Life = 2 });
        run.Stars.Add(new() { Position = new(-40, 30), Mass = 8, Life = 2 });
        run.World.Grid.Rebuild(run.Enemies);
        StarGravitySystem.Step(run);
        var expected = StarGravitySystem.AccelerationPerMass(run.Stars[1].Position) * 8
            + StarGravitySystem.AccelerationPerMass(first.Position) * first.Mass
            + StarGravitySystem.AccelerationPerMass(second.Position) * second.Mass;
        Check(Vector2.Distance(run.Stars[0].Acceleration, expected) < 0.001f, "All stars and nearby enemies contribute additively, not only the nearest");
        var momentum = run.Stars[0].Acceleration * 3 + run.Stars[1].Acceleration * 8
            + first.GravityAcceleration * first.Mass + second.GravityAcceleration * second.Mass;
        Check(momentum.Length() < 0.02f, "Reciprocal forces preserve mass-weighted momentum before integration limits");
        Check(first.GravityAcceleration.LengthSquared() > 0 && second.GravityAcceleration.LengthSquared() > 0, "Each enemy responds to all stars");
        Check(StarGravitySystem.AccelerationPerMass(Vector2.Zero) == Vector2.Zero, "Softened overlapping bodies never create NaN");
        Check(StarGravitySystem.AccelerationPerMass(new(10000, 0)) == Vector2.Zero, "Distant bodies do not force an all-world pair scan");
    }

    public static void Planet()
    {
        var displacements = new List<float>();
        foreach (var mass in new[] { 2f, 800f, 1e25f })
        {
            var run = new RunState(HeroKind.Marisa, 42);
            var boss = Target(run, new(200, 0), EnemyKind.Boss);
            run.Stars.Add(new() { Position = new(100, 0), Mass = mass, Life = 3 });
            run.World.Grid.Rebuild(run.Enemies);
            StarGravitySystem.Step(run);
            Check(boss.Mass == EnemyMassCatalog.Boss && boss.GravityAcceleration.X < 0, "Boss uses real mass, never a type-specific immunity");
            Check((Math.Abs(boss.GravityAcceleration.X) > Math.Abs(run.Stars[0].Acceleration.X)) == (mass > boss.Mass), "Relative masses determine which body is more strongly accelerated");
            EnemySystem.Step(run);
            displacements.Add(200 - boss.Position.X);
            Check(float.IsFinite(boss.Position.X) && boss.Timer < 10000, "Very heavy samples still move bosses safely without freezing their AI");
            run.Stars.Clear(); run.Marisa.GravityTick = 0; StarGravitySystem.Step(run);
            Check(boss.GravityAcceleration == Vector2.Zero, "Expired stars leave no permanent acceleration");
        }
        Check(displacements[1] > displacements[0] * 20 && displacements[2] >= displacements[1], "Boss attraction is hard at low mass but strong at high mass");
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
