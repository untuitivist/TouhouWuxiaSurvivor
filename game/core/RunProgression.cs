using System.Numerics;

namespace Rebirth.Core;

public sealed partial class RunState
{
    public void AddExperience(int amount)
    {
        if (amount <= 0 || Phase is RunPhase.Won or RunPhase.Lost) return;
        Experience = (int)Math.Min(int.MaxValue, (long)Experience + amount);
        while (Experience >= NextLevelExperience)
        {
            Experience -= NextLevelExperience;
            Level = Math.Min(int.MaxValue - 1, Level) + 1;
            PendingChoices = Math.Min(int.MaxValue - 1, PendingChoices) + 1;
        }
    }

    private void OfferChoices()
    {
        Choices.Clear();
        Choices.AddRange(UpgradeOffers.Create(Build, Level, upgradeRandom));
        Phase = RunPhase.Choosing;
        Emit(EffectKind.Level, PlayerPosition);
    }

    public bool Choose(int index)
    {
        if (Phase != RunPhase.Choosing || index < 0 || index >= Choices.Count) return false;
        var upgrade = Choices[index];
        if (!Build.TryApply(upgrade, Level)) return false;
        if (upgrade.Stance == MainlineStance.ChargedOfuda) Reimu.ShotCooldown = Math.Max(Reimu.ShotCooldown, MainlineGrowth.OfudaStats(Build).Interval);
        var art = upgrade.Ability;
        if (art == ArtKind.Vitality) Heal(35);
        if (art == ArtKind.Recovery)
        {
            Heal(40);
            SpellCharge = Math.Min(100, SpellCharge + 25);
        }
        PendingChoices--;
        Choices.Clear();
        Phase = RunPhase.Playing;
        if (PendingChoices > 0) OfferChoices();
        return true;
    }

    private void UpdatePickupsAndSeals()
    {
        PickupSystem.Step(this);
        foreach (var seal in Seals)
        {
            if (seal.Complete || Vector2.DistanceSquared(seal.Position, PlayerPosition) > 90 * 90) continue;
            seal.Charge = Math.Min(1, seal.Charge + StepSeconds / 7);
            if (seal.Charge < 1) continue;
            seal.Complete = true;
            Heal(35);
            PendingChoices++;
            SpellCharge = Math.Min(100, SpellCharge + 35);
            foreach (ref var pickup in Pickups.Active) pickup.Attracted = true;
            Emit(EffectKind.Seal, seal.Position);
        }
    }

    internal void CollectPickup(Pickup pickup)
    {
        if (pickup.Herbal) MarisaHerbSystem.Collect(this, pickup);
        else if (pickup.Healing) Heal(pickup.Value);
        else AddExperience(pickup.Value);
    }

    private void DropPickup(Vector2 position, int value, bool healing = false)
    {
        if (Pickups.Count >= PickupLimit)
        {
            var existing = Pickups.FindIndex(pickup => pickup.Healing == healing && !pickup.Herbal);
            if (existing >= 0) { Pickups[existing].Value += value; return; }
            if (!healing) { AddExperience(value); return; }
            Heal(value);
            return;
        }
        Pickups.Add(new() { Position = position, Value = value, Healing = healing });
    }

    internal void DamageEnemy(Enemy enemy, float damage, Vector2 knockback, bool showHit = true)
    {
        if (enemy.Health <= 0) return;
        enemy.Health -= damage;
        enemy.Flash = 0.1f;
        if (enemy.Kind != EnemyKind.Boss && (knockback.X != 0 || knockback.Y != 0))
            enemy.Position = ClampToArena(new(enemy.Position.X + knockback.X, enemy.Position.Y + knockback.Y));
        if (showHit) Emit(EffectKind.Hit, enemy.Position, damage);
        if (enemy.Health > 0) return;
        Kills++;
        SpellCharge = Math.Min(100, SpellCharge + (enemy.Kind == EnemyKind.Elite ? 8 : 0.42f));
        DropPickup(enemy.Position, enemy.Kind == EnemyKind.Elite ? 20 : enemy.Kind == EnemyKind.Kedama ? 1 : 2);
        if (enemy.Kind == EnemyKind.Elite) DropPickup(enemy.Position + new Vector2(18, 0), 30, true);
        Emit(EffectKind.Defeat, enemy.Position);
        if (enemy.Kind == EnemyKind.Boss && Phase != RunPhase.Lost)
        {
            var ownerId = enemy.Id;
            foreach (ref var star in Stars.Active) if (star.OwnerId == ownerId) star.Life = 0;
            foreach (ref var projectile in Projectiles.Active) if (projectile.OwnerId == ownerId) projectile.Life = 0;
            enemy.Abilities = null;
            PlayerGravityAcceleration = Vector2.Zero;
            CompleteBossEncounter(enemy);
        }
    }

    private void CastSignatureSpell()
    {
        SpellCharge = 0;
        SpellsCast++;
        SpellFlash = 0.65f;
        Invulnerability = Math.Max(Invulnerability, 0.6f);
        Projectiles.RemoveAll(projectile => projectile.Hostile);
        if (Hero == HeroKind.Reimu) CastDreamSeal();
        else MarisaBeamSystem.Start(this, true);
        foreach (ref var pickup in Pickups.Active) pickup.Attracted = true;
        Emit(EffectKind.Spell, PlayerPosition);
    }
}
