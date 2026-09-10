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
        target.Health = target.MaxHealth = 1000000;
        target.Speed = target.ContactDamage = 0;
        target.Timer = 10000;
        return target;
    }

    public static void Offers()
    {
        var run = new RunState(HeroKind.Marisa, 42);
        var build = run.Build;
        Check(build.Ranks.Sum() == 1 && !build.SignatureUnlocked, "Stars-only starter and locked signature");
        foreach (var id in new[] { UpgradeCatalog.StardustRecall, UpgradeCatalog.SparkSweep, UpgradeCatalog.FinalSpark, "marisa.stardust.refine" })
            Check(!build.CanChoose(UpgradeCatalog.Get(id), 100), "Locked prerequisite " + id);
        Check(!build.CanChoose(UpgradeCatalog.Get(UpgradeCatalog.StarSplit), 2), "Behavior requires level three");
        for (var seed = 0; seed < 100; seed++)
        {
            var offers = UpgradeOffers.Create(build, 3, new(seed));
            Check(offers.Count == 3 && offers.DistinctBy(upgrade => upgrade.Id).Count() == 3, "Three unique choices");
            Check(offers.All(upgrade => build.CanChoose(upgrade, 3)), "Prerequisites and ownership respected");
            Check(offers.Any(upgrade => upgrade.Kind == UpgradeKind.Unlock) && offers.Any(upgrade => upgrade.Kind == UpgradeKind.Behavior), "Unlock and behavior remain visible");
        }
        var nodes = new[] { UpgradeCatalog.StarPierce, UpgradeCatalog.StarSplit, UpgradeCatalog.StardustRecall, UpgradeCatalog.StardustEcho, UpgradeCatalog.SparkSweep, UpgradeCatalog.SparkClear };
        var reverse = new RunState(HeroKind.Marisa, 42);
        foreach (var candidate in new[] { run, reverse }) Learn(candidate, UpgradeCatalog.StardustUnlock, UpgradeCatalog.MasterSparkUnlock);
        Learn(run, nodes); Learn(reverse, nodes.Reverse().ToArray());
        Check(build.Traits == reverse.Build.Traits && build.Ranks.SequenceEqual(reverse.Ranks), "Compatible learning order commutes");
        Check(!build.TryApply(UpgradeCatalog.Get(nodes[0]), 100), "No duplicate behavior");
        Check(!new BuildState(HeroKind.Reimu).TryApply(UpgradeCatalog.Get(nodes[0]), 100), "No cross-hero traits");
        Check(!build.CanChoose(UpgradeCatalog.Get(UpgradeCatalog.FinalSpark), 4) && build.CanChoose(UpgradeCatalog.Get(UpgradeCatalog.FinalSpark), 5), "Signature requires level five and beam");
        foreach (var node in UpgradeCatalog.All.Where(upgrade => upgrade.Kind != UpgradeKind.Recovery))
            while (build.TryApply(node, 100)) { }
        Check(UpgradeOffers.Create(build, 100, new(1)).Single().Id == UpgradeCatalog.Recovery, "Exhausted tree falls back to recovery");
    }

    public static void Starter()
    {
        var run = new RunState(HeroKind.Marisa, 42);
        Target(run, new(400, 0));
        for (var index = 0; index < 27; index++) run.Graze();
        for (var tick = 0; tick < 100; tick++) run.Step(default);
        Check(run.SpellCharge == 100 && run.SpellsCast == 0 && run.Beam == null, "Full charge does not bypass the unlock");
        Check(run.Projectiles.All(projectile => projectile.Art == ArtKind.Stars), "Only stars at start");
        Learn(run, UpgradeCatalog.MasterSparkUnlock, UpgradeCatalog.FinalSpark);
        run.Projectiles.Add(new() { Hostile = true, Position = new(-300, 0), Life = 10 });
        run.Step(default);
        Check(run.SpellsCast == 1 && run.SpellCharge == 0 && run.Beam is { Signature: true, Warmup: > 0 }, "Learned full-charge beam activates");
        Check(run.Projectiles.All(projectile => !projectile.Hostile), "Signature retains one-shot screen clear");
    }

    public static void Split()
    {
        var run = new RunState(HeroKind.Marisa, 42);
        var direct = Target(run, new(100, 0));
        run.World.Grid.Rebuild(run.Enemies);
        run.Projectiles.Add(new() { Art = ArtKind.Stars, Position = new(90, 0), Velocity = new(600, 0), Radius = 5, Life = 2, Damage = 100, Pierce = 1, StarSplit = true });
        ProjectileSystem.Step(run);
        Check(run.Projectiles.Count == 4 && direct.MaxHealth - direct.Health == 100, "Split flushes after the loop, not during iteration");
        Check(run.Projectiles.All(projectile => !projectile.StarSplit && projectile.HitIds.Contains(direct.Id)), "Parent and fragments exclude original target and cannot split again");
        Check(run.Projectiles.Skip(1).All(projectile => projectile.Radius == 5 && projectile.Damage == 25 && projectile.Pierce == 0), "Fragments have bounded damage and penetration");
        for (var tick = 0; tick < 40; tick++) ProjectileSystem.Step(run);
        Check(direct.MaxHealth - direct.Health == 100 && run.Projectiles.Count == 1, "No repeat hits or recursive fragments");
        run.Projectiles.Clear();
        for (var index = 0; index < 100; index++)
        {
            var parent = new Projectile { StarSplit = true, Damage = 20, Velocity = Vector2.UnitX };
            MarisaProjectileSystem.OnHit(run, ref parent, direct.Id);
        }
        Check(run.Marisa.FragmentCount == MarisaTuning.FragmentQueueLimit, "Queue is explicitly bounded");
        for (var index = 0; index < RunState.ProjectileLimit - 1; index++) run.Projectiles.Add(new() { Life = 2 });
        MarisaProjectileSystem.Flush(run);
        Check(run.Projectiles.Count == RunState.ProjectileLimit && run.Marisa.FragmentCount == 0, "Shared entity cap and queue reset are preserved");
    }

    public static void Recall()
    {
        var run = new RunState(HeroKind.Marisa, 42);
        var target = Target(run, new(100, 0));
        var dust = new Projectile { Art = ArtKind.Stardust, Position = new(180, 0), Velocity = new(215, 0), Life = 2, RecallRemaining = 0.02f, Damage = 100, Radius = 8, Pierce = 2 };
        dust.HitIds.Add(target.Id);
        run.Projectiles.Add(dust);
        run.World.Grid.Rebuild(run.Enemies);
        ProjectileSystem.Step(run);
        Check(run.Projectiles[0].Velocity.X > 0, "Outward phase stays outward");
        ProjectileSystem.Step(run);
        Check(run.Projectiles[0].Velocity.X < 0 && run.Projectiles[0].RecallRemaining < 0, "Return changes motion only after delay");
        for (var tick = 0; tick < 60; tick++) ProjectileSystem.Step(run);
        Check(target.Health == target.MaxHealth && run.Projectiles.Count == 0, "Hit history survives recall; caught dust retires");
    }

    public static void Echo()
    {
        var run = new RunState(HeroKind.Marisa, 42);
        Learn(run, UpgradeCatalog.StardustUnlock, UpgradeCatalog.StardustEcho, UpgradeCatalog.StardustRecall);
        run.Ranks[(int)ArtKind.Stars] = 0;
        Target(run, new(800, 0));
        run.Step(default);
        var count = AbilityTuning.Get(ArtKind.Stardust, 1).Count;
        Check(run.Projectiles.Count == count && run.Marisa.EchoCount == count, "First ring and pending echo");
        var origin = run.Marisa.EchoTemplate.Position;
        var delay = run.Marisa.EchoRemaining;
        var damage = run.Marisa.EchoTemplate.Damage;
        run.TogglePause();
        for (var tick = 0; tick < 60; tick++) run.Step(default);
        Check(run.Marisa.EchoRemaining == delay, "Pause freezes echo");
        run.TogglePause();
        run.AddExperience(run.NextLevelExperience); run.Step(default);
        delay = run.Marisa.EchoRemaining;
        for (var tick = 0; tick < 60; tick++) run.Step(default);
        Check(run.Phase == RunPhase.Choosing && run.Marisa.EchoRemaining == delay, "Choice screen freezes echo");
        while (run.Phase == RunPhase.Choosing) Check(run.Choose(0), "Resolve choice");
        run.Ranks[(int)ArtKind.Stars] = 0;
        run.Marisa.StardustCooldown = 100; run.Marisa.BeamCooldown = 100;
        run.Ranks[(int)ArtKind.Power] = 3;
        while (run.Marisa.EchoCount > 0) run.Step(new(Vector2.UnitY));
        var second = run.Projectiles.Where(projectile => projectile.Art == ArtKind.Stardust && projectile.Life > 2).ToArray();
        Check(second.Length == count && run.PlayerPosition.Y > 50, "Delayed ring appears while player moves");
        Check(second.All(projectile => Vector2.Distance(projectile.Position, origin) < 4 && Math.Abs(projectile.Damage - damage) < 0.001f), "Echo retains original origin and damage snapshot");
        Check(second.All(projectile => projectile.RecallRemaining > 0 && projectile.Pierce == 2), "Echo and recall combine");
    }

    public static void Language()
    {
        GameText.SetLanguage("en");
        try
        {
            var build = new BuildState(HeroKind.Marisa);
            foreach (var node in UpgradeCatalog.All.Where(upgrade => upgrade.Owner == HeroKind.Marisa))
            foreach (var text in new[] { node.Name, node.Description, UpgradeCatalog.Requirement(node), UpgradeCatalog.Progress(node, build) })
                Check(!GameText.Get(text).Any(character => character is >= '㐀' and <= '鿿'), "Untranslated Marisa text: " + text);
        }
        finally { GameText.SetLanguage("zh"); }
    }
}
