using System.Numerics;

namespace Rebirth.Core;

public sealed partial class RunState
{
    public void AddExperience(int amount)
    {
        if (amount <= 0 || Phase is RunPhase.Won or RunPhase.Lost) return;
        Experience += amount;
        while (Experience >= NextLevelExperience)
        {
            Experience -= NextLevelExperience;
            Level++;
            PendingChoices++;
        }
    }

    private void OfferChoices()
    {
        Choices.Clear();
        var candidates = ArtCatalog.All.Where(art => art.Id != ArtKind.Recovery && ArtCatalog.Available(Hero, art.Id) && Ranks[(int)art.Id] < art.MaxRank)
            .Select(art => art.Id).ToList();
        while (Choices.Count < 3 && candidates.Count > 0)
        {
            var index = random.Next(candidates.Count);
            Choices.Add(candidates[index]);
            candidates.RemoveAt(index);
        }
        if (Choices.Count < 3) Choices.Add(ArtKind.Recovery);
        Phase = RunPhase.Choosing;
        Emit(EffectKind.Level, PlayerPosition);
    }

    public bool Choose(int index)
    {
        if (Phase != RunPhase.Choosing || index < 0 || index >= Choices.Count) return false;
        var art = Choices[index];
        if (!ArtCatalog.Available(Hero, art) || Ranks[(int)art] >= ArtCatalog.Get(art).MaxRank) return false;
        if (art != ArtKind.Recovery) Ranks[(int)art]++;
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
        for (var index = Pickups.Count - 1; index >= 0; index--)
        {
            var pickup = Pickups[index];
            var distance = Vector2.DistanceSquared(pickup.Position, PlayerPosition);
            if (distance < PickupRadius * PickupRadius) pickup.Attracted = true;
            if (pickup.Attracted)
            {
                var offset = PlayerPosition - pickup.Position;
                pickup.Position += Geometry.Direction(offset) * Math.Min(offset.Length(), 540 * StepSeconds);
                if (Vector2.DistanceSquared(pickup.Position, PlayerPosition) < 20 * 20)
                {
                    if (pickup.Healing) Heal(pickup.Value);
                    else AddExperience(pickup.Value);
                    Pickups.RemoveAt(index);
                }
            }
        }
        foreach (var seal in Seals)
        {
            if (seal.Complete || Vector2.DistanceSquared(seal.Position, PlayerPosition) > 90 * 90) continue;
            seal.Charge = Math.Min(1, seal.Charge + StepSeconds / 7);
            if (seal.Charge < 1) continue;
            seal.Complete = true;
            Heal(35);
            PendingChoices++;
            SpellCharge = Math.Min(100, SpellCharge + 35);
            foreach (var pickup in Pickups) pickup.Attracted = true;
            Emit(EffectKind.Seal, seal.Position);
        }
    }

    private void DropPickup(Vector2 position, int value, bool healing = false)
    {
        if (Pickups.Count >= PickupLimit)
        {
            var existing = Pickups.Find(pickup => pickup.Healing == healing);
            if (existing != null) { existing.Value += value; return; }
            if (!healing) { AddExperience(value); return; }
            Heal(value);
            return;
        }
        Pickups.Add(new() { Position = position, Value = value, Healing = healing });
    }

    private void DamageEnemy(Enemy enemy, float damage, Vector2 knockback)
    {
        if (enemy.Health <= 0) return;
        enemy.Health -= damage;
        enemy.Flash = 0.1f;
        if (enemy.Kind != EnemyKind.Boss) enemy.Position = ClampToArena(enemy.Position + knockback);
        Emit(EffectKind.Hit, enemy.Position, damage);
        if (enemy.Health > 0) return;
        Kills++;
        SpellCharge = Math.Min(100, SpellCharge + (enemy.Kind == EnemyKind.Elite ? 8 : 0.42f));
        DropPickup(enemy.Position, enemy.Kind == EnemyKind.Elite ? 20 : enemy.Kind == EnemyKind.Kedama ? 1 : 2);
        if (enemy.Kind == EnemyKind.Elite) DropPickup(enemy.Position + new Vector2(18, 0), 30, true);
        Emit(EffectKind.Defeat, enemy.Position);
        if (enemy.Kind == EnemyKind.Boss && Phase != RunPhase.Lost)
        {
            Phase = RunPhase.Won;
            Emit(EffectKind.Victory, enemy.Position);
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
        else StartBeam(true);
        foreach (var pickup in Pickups) pickup.Attracted = true;
        Emit(EffectKind.Spell, PlayerPosition);
    }
}
