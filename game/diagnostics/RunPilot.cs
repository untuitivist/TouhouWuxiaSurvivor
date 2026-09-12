using System.Numerics;
using Rebirth.Core;

namespace Rebirth.Diagnostics;

public static class RunPilot
{
    public static void ResolveChoices(RunState run, MainlineStance preferredStance = MainlineStance.None)
    {
        while (run.Phase == RunPhase.Choosing)
        {
            var preferred = Enumerable.Range(0, run.Choices.Count).MaxBy(index => run.Choices[index].Kind == UpgradeKind.Stance && run.Choices[index].Stance == preferredStance
                ? 120 : ChoicePriority(run, run.Choices[index]));
            run.Choose(preferred);
        }
    }

    private static int ChoicePriority(RunState run, UpgradeDefinition upgrade)
    {
        if (upgrade.Kind == UpgradeKind.Stance) return 95;
        if (upgrade.Id == UpgradeCatalog.HerbsUnlock) return 100;
        if (upgrade.Kind == UpgradeKind.Refine && upgrade.Ability == MainlineGrowth.Primary(run.Hero)) return 90;
        if (upgrade.Ability == ArtKind.Flow && run.Ranks[(int)ArtKind.Flow] < 2) return 85;
        if (upgrade.Id is UpgradeCatalog.Homing or UpgradeCatalog.HerbBrew or UpgradeCatalog.SparkSteer) return 80;
        if (upgrade.Id is UpgradeCatalog.Bind or UpgradeCatalog.Clear or UpgradeCatalog.SparkClear) return 78;
        if (upgrade.Id is UpgradeCatalog.Cluster or UpgradeCatalog.SparkResonance) return 72;
        if (upgrade.Kind == UpgradeKind.Unlock) return 75;
        if (upgrade.Ability == ArtKind.Vitality && run.Health < run.MaxHealth * 0.6f) return 70;
        if (upgrade.Id is UpgradeCatalog.Blast or UpgradeCatalog.DreamSeal or UpgradeCatalog.FinalSpark) return 65;
        if (upgrade.Id == UpgradeCatalog.StarMass && run.Build.TrainingRank(upgrade.Id) < 4) return 60;
        if (upgrade.Ability == ArtKind.Herbs && upgrade.Kind == UpgradeKind.Refine) return run.Health < run.MaxHealth * 0.65f ? 85 : 55;
        return upgrade.Owner == run.Hero ? 40 : 20;
    }

    public static FrameInput Input(RunState run, int tick)
    {
        Vector2? medicine = null;
        Vector2? experience = null;
        var medicineDistance = float.MaxValue;
        var experienceDistance = float.MaxValue;
        foreach (ref readonly var pickup in run.Pickups.Active)
        {
            var distance = Geometry.DistanceSquared(pickup.Position, run.PlayerPosition);
            if (pickup.Healing || pickup.Herbal)
            {
                if (run.Health < run.MaxHealth * 0.65f && distance < medicineDistance)
                { medicine = pickup.Position; medicineDistance = distance; }
            }
            else if (distance < experienceDistance) { experience = pickup.Position; experienceDistance = distance; }
        }
        var target = medicine ?? run.Seals.FirstOrDefault(seal => !seal.Complete)?.Position
            ?? experience ?? run.Boss?.Position ?? Vector2.Zero;
        var direction = target - run.PlayerPosition;
        var desiredDistance = run.PurifiedSeals < 3 || experience.HasValue || medicine.HasValue ? 18 : 240;
        var movement = direction.Length() > desiredDistance ? Geometry.Direction(direction) : Vector2.Zero;
        if (run.Boss is { } opponent && (!medicine.HasValue || medicineDistance > 250 * 250))
        {
            var offset = run.PlayerPosition - opponent.Position;
            var radius = offset.Length();
            var outward = Geometry.Direction(offset);
            movement = new Vector2(-outward.Y, outward.X) * 0.85f + outward * Math.Clamp((320 - radius) / 100, -1, 1);
        }
        var danger = false;
        foreach (var enemy in run.Enemies)
        {
            var away = run.PlayerPosition - enemy.Position;
            var distance = away.Length();
            if (distance < 95 && distance > 0) movement += away / distance * (1 - distance / 95) * 2.5f;
            if (distance < enemy.Radius + 25) danger = true;
        }
        foreach (var projectile in run.Projectiles)
        {
            if (!projectile.Hostile) continue;
            var away = run.PlayerPosition - projectile.Position - projectile.Velocity * 0.18f;
            var distance = away.Length();
            if (distance < 55 && distance > 0) movement += away / distance * (1 - distance / 55) * 2;
            if (distance < 24) danger = true;
        }
        foreach (ref readonly var star in run.Stars.Active)
        {
            if (!star.Hostile) continue;
            var away = run.PlayerPosition - star.Position - star.Velocity * 0.15f;
            var radius = MarisaTuning.DamageRadius(star.Mass) + 35;
            var distance = away.Length();
            if (distance < radius && distance > 0) movement += away / distance * 2;
            if (distance < radius - 20) danger = true;
        }
        if (run.Boss?.Abilities?.Field is { } field)
        {
            var away = run.PlayerPosition - field.Position;
            if (Math.Max(Math.Abs(away.X), Math.Abs(away.Y)) < field.HalfSize + 25)
            {
                movement = away.LengthSquared() < 1 ? Vector2.UnitX : Geometry.Direction(away);
                danger |= field.Warmup < 0.35f;
            }
        }
        if (run.Boss is { Abilities.Beam: { } beam } boss)
        {
            var offset = run.PlayerPosition - boss.Position;
            var along = Vector2.Dot(offset, beam.Direction);
            var normal = new Vector2(-beam.Direction.Y, beam.Direction.X);
            var across = Vector2.Dot(offset, normal);
            if (along > 0 && along < beam.Length && Math.Abs(across) < beam.HalfWidth + 70)
            {
                movement = normal * (across < 0 ? -1 : 1);
                danger |= beam.Warmup < 0.35f;
            }
        }
        return new(movement, false, danger || run.Health < run.MaxHealth * 0.5f && tick % 180 == 0);
    }
}
