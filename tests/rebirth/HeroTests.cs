using System.Numerics;
using Rebirth.Core;

namespace Rebirth.Tests;

internal static class HeroTests
{
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static RunState Empty(HeroKind hero)
    {
        var run = new RunState(hero, 260906);
        Array.Clear(run.Ranks);
        return run;
    }

    private static Enemy Target(RunState run, Vector2 position)
    {
        var enemy = run.SpawnEnemy(EnemyKind.Elite, position);
        enemy.Health = enemy.MaxHealth = 100000;
        enemy.Speed = 0;
        enemy.Timer = 1000;
        return enemy;
    }

    public static void UpgradeDescriptions()
    {
        foreach (var art in ArtCatalog.All.Where(art => art.Id != ArtKind.Recovery))
        {
            for (var rank = 0; rank < art.MaxRank; rank++)
                Check(!string.IsNullOrWhiteSpace(ArtCatalog.UpgradeText(art.Id, rank)), "Every rank has a description");
            Check(ArtCatalog.UpgradeText(art.Id, art.MaxRank).Contains("已达圆满"), "No impossible next rank");
            if (art.Owner != null) Check(art.Source.Length > 0, "Each character ability records its source");
        }
        foreach (var kind in new[] { ArtKind.Ofuda, ArtKind.Stars })
        for (var rank = 1; rank <= 5; rank++)
        {
            var run = Empty(ArtCatalog.Get(kind).Owner!.Value);
            run.Ranks[(int)kind] = rank;
            Target(run, new(400, 0));
            run.Step(default);
            var projectiles = run.Projectiles.Where(projectile => !projectile.Hostile && projectile.Art == kind).ToArray();
            var stats = AbilityTuning.Get(kind, rank);
            Check(projectiles.Length == stats.Count, "Actual volley count matches shared tuning");
            Check(projectiles.All(projectile => Math.Abs(projectile.Damage - stats.Damage * run.Power) < 0.001f), "Damage includes hero power exactly once");
            Check(ArtCatalog.UpgradeText(kind, rank - 1).Contains(stats.Count.ToString()), "Preview exposes actual volley count");
        }
    }

    public static void Ownership()
    {
        foreach (var hero in Enum.GetValues<HeroKind>())
        {
            var run = new RunState(hero, 71);
            Check(ArtCatalog.Abilities(hero).Count() == 3, "Three exclusive ability tracks");
            for (var iteration = 0; iteration < 35; iteration++)
            {
                run.AddExperience(run.NextLevelExperience);
                run.Step(default);
                while (run.Phase == RunPhase.Choosing)
                {
                    Check(run.Choices.All(art => ArtCatalog.Available(hero, art.Ability)), "Foreign abilities never enter offers");
                    Check(run.Choose(0), "Legal choice applies");
                }
            }
            var foreign = ArtCatalog.All.First(art => art.Owner != null && art.Owner != hero).Id;
            Check(run.Ranks[(int)foreign] == 0, "Progression cannot acquire another hero's attack");
            run.AddExperience(run.NextLevelExperience);
            run.Step(default);
            run.Choices.Clear();
            run.Choices.Add(UpgradeCatalog.All.First(upgrade => upgrade.Ability == foreign));
            Check(!run.Choose(0) && run.Ranks[(int)foreign] == 0, "Choice application also rejects invalid ownership");
        }
    }

    public static void Homing()
    {
        var run = Empty(HeroKind.Reimu);
        run.Ranks[(int)ArtKind.Ofuda] = 1;
        run.Build.TryApply(UpgradeCatalog.Get(UpgradeCatalog.Homing), 3);
        var first = Target(run, new(300, 0));
        run.Step(default);
        var ofuda = run.Projectiles.First();
        var initialVertical = ofuda.Velocity.Y;
        first.Position = new(240, 250);
        run.Step(default);
        ofuda = run.Projectiles[0];
        Check(ofuda.Velocity.Y > initialVertical && ofuda.TurnRate > 0, "Ofuda bends toward moving target");
        first.Health = 0;
        var second = Target(run, new(300, -250));
        run.Step(default);
        ofuda = run.Projectiles[0];
        Check(ofuda.TargetId == second.Id, "Ofuda reacquires a live target after death");
    }

    public static void StarFocus()
    {
        var wide = Empty(HeroKind.Marisa);
        var narrow = Empty(HeroKind.Marisa);
        wide.Ranks[(int)ArtKind.Stars] = narrow.Ranks[(int)ArtKind.Stars] = 3;
        Target(wide, new(400, 0)); Target(narrow, new(400, 0));
        wide.Step(default); narrow.Step(new(Vector2.Zero, true));
        Check(wide.Projectiles.Count == narrow.Projectiles.Count, "Focus does not create extra stars");
        Check(wide.Projectiles.Max(projectile => Math.Abs(projectile.Velocity.Y)) > narrow.Projectiles.Max(projectile => Math.Abs(projectile.Velocity.Y)) * 2, "Focus actually narrows spread");
        Check(wide.Projectiles.Sum(projectile => projectile.Damage) == narrow.Projectiles.Sum(projectile => projectile.Damage), "Focus is not a hidden damage multiplier");
        Check(wide.Projectiles.All(projectile => projectile.TurnRate == 0), "Stars are not renamed homing ofuda");
    }

    public static void BeamGeometry()
    {
        var run = Empty(HeroKind.Marisa);
        run.Ranks[(int)ArtKind.MasterSpark] = 3;
        run.Build.TryApply(UpgradeCatalog.Get(UpgradeCatalog.SparkClear), 3);
        var inside = Target(run, new(400, 0));
        run.Step(default);
        var outside = Target(run, new(400, 180));
        var behind = Target(run, new(-150, 0));
        Check(run.Beam is { Warmup: > 0 } && inside.Health == inside.MaxHealth, "Telegraph has no damage");
        var heading = run.Beam!.Direction;
        while (run.Beam.Warmup > 0) run.Step(default);
        Check(inside.Health == inside.MaxHealth, "Warmup remains harmless through its final tick");
        var bullet = new Projectile { Position = new(200, 0), Hostile = true, Radius = 1, Life = 2 };
        var safeBullet = new Projectile { Position = new(200, 150), Hostile = true, Radius = 1, Life = 2 };
        run.Projectiles.Add(bullet); run.Projectiles.Add(safeBullet);
        run.Step(default);
        Check(inside.Health < inside.MaxHealth && outside.Health == outside.MaxHealth && behind.Health == behind.MaxHealth, "Beam damages its forward corridor only");
        Check(!run.Projectiles.Any(projectile => projectile.Hostile && projectile.Position == bullet.Position) && run.Projectiles.Any(projectile => projectile.Hostile && projectile.Position == safeBullet.Position), "Only bullets inside the beam are cleared");
        var afterPulse = inside.Health;
        run.Step(default);
        Check(inside.Health == afterPulse, "Beam uses pulse interval rather than per-frame damage");
        inside.Position = new(300, 200);
        run.Step(default);
        Check(run.Beam.Direction == heading, "Active beam does not auto-snap to moving targets");
        Check(!run.BeamContains(new(1500, 0)) && !run.BeamContains(new(-60, 0)), "Finite range and no backward ray");
    }

    public static void StationaryBoundary()
    {
        var run = Empty(HeroKind.Reimu);
        run.Ranks[(int)ArtKind.Boundary] = 2;
        var inside = Target(run, new(70, 0));
        var outside = Target(run, new(260, 0));
        run.Step(default);
        Check(run.Field != null && inside.Health < inside.MaxHealth && outside.Health == outside.MaxHealth, "Field damages its square, not the entire screen");
        var position = run.Field!.Position;
        for (var index = 0; index < 30; index++) run.Step(new(Vector2.UnitY));
        Check(run.Field.Position == position && run.PlayerPosition.Y > 90, "Leaving does not drag the field along");
    }

    public static void SignatureIdentity()
    {
        foreach (var hero in Enum.GetValues<HeroKind>())
        {
            var run = Empty(hero);
            if (hero == HeroKind.Reimu)
            {
                run.Ranks[(int)ArtKind.Ofuda] = 1;
                run.Build.TryApply(UpgradeCatalog.Get(UpgradeCatalog.DreamSeal), 5);
            }
            else
            {
                run.Ranks[(int)ArtKind.MasterSpark] = 1;
                run.Build.TryApply(UpgradeCatalog.Get(UpgradeCatalog.FinalSpark), 5);
            }
            var count = hero == HeroKind.Reimu ? 20 : 27;
            for (var index = 0; index < count; index++)
                run.Projectiles.Add(new() { Position = Geometry.Angle(index * MathF.Tau / count) * 25, Hostile = true, Radius = 1, Life = 2 });
            run.Step(default);
            Check(run.SpellsCast == 1 && run.Projectiles.All(projectile => !projectile.Hostile), "Both signatures retain activation safety clear");
            if (hero == HeroKind.Reimu) Check(run.Beam == null && run.Projectiles.Count(projectile => projectile.DreamOrb) == 7, "Reimu creates seven dream orbs, not a beam");
            else Check(run.Beam is { Signature: true, Warmup: > 0 } && run.Projectiles.All(projectile => !projectile.DreamOrb), "Marisa creates a telegraphed signature beam");
        }
    }

    public static void AbilityPause()
    {
        foreach (var hero in Enum.GetValues<HeroKind>())
        {
            var run = Empty(hero);
            run.Ranks[(int)(hero == HeroKind.Reimu ? ArtKind.Boundary : ArtKind.MasterSpark)] = 2;
            Target(run, new(150, 0));
            run.Step(default);
            run.TogglePause();
            var fieldLife = run.Field?.Remaining;
            var beamWarmup = run.Beam?.Warmup;
            for (var index = 0; index < 60; index++) run.Step(default);
            Check(run.Field?.Remaining == fieldLife && run.Beam?.Warmup == beamWarmup, "Pause freezes fields and beam warmup");
            run.TogglePause();
            run.AddExperience(30);
            run.Step(default);
            fieldLife = run.Field?.Remaining; beamWarmup = run.Beam?.Warmup;
            for (var index = 0; index < 60; index++) run.Step(default);
            Check(run.Phase == RunPhase.Choosing && run.Field?.Remaining == fieldLife && run.Beam?.Warmup == beamWarmup, "Upgrade inspection cannot advance active abilities");
        }
    }
}
