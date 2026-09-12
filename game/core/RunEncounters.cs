using System.Numerics;

namespace Rebirth.Core;

public sealed partial class RunState
{
    private void UpdateEncounters()
    {
        if (Time >= nextRecovery)
        {
            nextRecovery += RunPacing.WaveSeconds;
            if (!BossSpawned && Boss == null && Pickups.Count < PickupLimit)
                DropPickup(ClampToArena(PlayerPosition + Geometry.Rotate(Facing, MathF.PI / 2) * 130), RunPacing.RecoveryAmount, true);
        }
        spawnTimer -= StepSeconds;
        if (spawnTimer <= 0)
        {
            spawnTimer += 1 / RunPacing.SpawnRate(Time, BossSpawned, EndlessRounds);
            if (Enemies.Count < EnemyLimit)
            {
                var roll = RandomFloat();
                var stage = RunPacing.At(Time);
                var kind = Time > 45 && roll < stage.FairyChance ? EnemyKind.Fairy
                    : Time > 100 && roll < stage.FairyChance + stage.ChargerChance ? EnemyKind.Charger : EnemyKind.Kedama;
                SpawnEnemy(kind, SpawnPoint());
            }
        }
        if (Time >= nextElite && !BossSpawned)
        {
            SpawnEnemy(EnemyKind.Elite, SpawnPoint());
            nextElite += 150;
        }
        if (Time >= NextBossTime && !BossSpawned && Boss == null)
        {
            BossSpawned = true;
            Projectiles.RemoveAll(projectile => projectile.Hostile);
            SpawnEnemy(EnemyKind.Boss, ClampToArena(PlayerPosition + new Vector2(0, -360)));
            Emit(EffectKind.Boss, PlayerPosition);
        }
    }

    private Vector2 SpawnPoint()
    {
        for (var attempt = 0; attempt < 12; attempt++)
        {
            var point = ClampToArena(PlayerPosition + Geometry.Angle(RandomFloat() * MathF.Tau) * (730 + RandomFloat() * 100));
            if (Vector2.DistanceSquared(point, PlayerPosition) > 500 * 500) return point;
        }
        return ClampToArena(PlayerPosition - Geometry.Direction(PlayerPosition) * 730);
    }

    internal Enemy SpawnEnemy(EnemyKind kind, Vector2 position)
    {
        var enemy = new Enemy { Id = ++nextEnemyId, Kind = kind, Mass = EnemyMassCatalog.Get(kind), Position = position, Timer = 1.2f + RandomFloat() * 1.8f };
        (enemy.MaxHealth, enemy.Speed, enemy.Radius, enemy.ContactDamage) = kind switch
        {
            EnemyKind.Kedama => (18, 80, 14, 13),
            EnemyKind.Fairy => (32, 68, 16, 15),
            EnemyKind.Charger => (48, 92, 19, 20),
            EnemyKind.Elite => (300, 63, 27, 22),
            _ => (26000, 70, 32, 26)
        };
        enemy.ThreatScale = RunPacing.At(Time).HealthScale * (1 + EndlessRounds * 0.3f);
        enemy.MaxHealth *= enemy.ThreatScale;
        enemy.Health = enemy.MaxHealth;
        if (kind == EnemyKind.Boss)
        {
            var character = CharacterCatalog.BossCandidates(Hero).Single();
            var profile = character.Boss;
            enemy.Character = character.Id;
            enemy.Abilities = new(character.Id, unchecked(Seed ^ enemy.Id * 7919));
            enemy.MaxHealth = profile.Health * (1 + EndlessRounds * 0.6f);
            enemy.Health = enemy.MaxHealth;
            enemy.ThreatScale = 1 + EndlessRounds * 0.6f;
            enemy.Speed = profile.Speed;
            enemy.Mass = profile.Mass;
            enemy.Radius = profile.Radius;
            enemy.ContactDamage = profile.ContactDamage;
            enemy.Timer = 3;
        }
        Enemies.Add(enemy);
        return enemy;
    }

    internal void ShootFan(Vector2 origin, Vector2 direction, int count, float spread, float speed, float damage)
    {
        for (var index = 0; index < count; index++)
            AddProjectile(new() { Position = origin, Velocity = Geometry.Rotate(direction, (index - (count - 1) / 2f) * spread) * speed, Radius = 5, Damage = damage, Life = 7, Hostile = true });
    }

    internal void ShootRing(Vector2 origin, int count, float rotation, float speed, bool alternate)
    {
        for (var index = 0; index < count; index++)
            AddProjectile(new() { Position = origin, Velocity = Geometry.Angle(rotation + MathF.Tau * index / count) * speed, Radius = 5, Damage = 15, Life = 7, Hostile = true, Alternate = alternate });
    }

    internal void AddProjectile(Projectile projectile)
    {
        if (Projectiles.Count < ProjectileLimit) Projectiles.Add(projectile);
    }
}
