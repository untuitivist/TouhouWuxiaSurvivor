using System.Numerics;

namespace Rebirth.Core;

public sealed partial class RunState
{
    private void UpdateEncounters()
    {
        spawnTimer -= StepSeconds;
        if (spawnTimer <= 0)
        {
            spawnTimer += 1 / (2.0f + Math.Min(Time, BossArrival) / 45);
            if (Enemies.Count < EnemyLimit)
            {
                var roll = RandomFloat();
                var kind = Time > 22 && roll < 0.23f ? EnemyKind.Fairy : Time > 65 && roll < 0.39f ? EnemyKind.Charger : EnemyKind.Kedama;
                SpawnEnemy(kind, SpawnPoint());
            }
        }
        if (Time >= nextElite && !BossSpawned)
        {
            SpawnEnemy(EnemyKind.Elite, SpawnPoint());
            nextElite += 55;
        }
        if (Time >= BossArrival && !BossSpawned)
        {
            BossSpawned = true;
            Projectiles.RemoveAll(projectile => projectile.Hostile);
            SpawnEnemy(EnemyKind.Boss, ClampToArena(PlayerPosition + new Vector2(0, -290)));
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
            EnemyKind.Kedama => (18 + Time * 0.095f, 70 + Math.Min(Time, 240) * 0.14f, 14, 13),
            EnemyKind.Fairy => (32 + Time * 0.13f, 68, 16, 15),
            EnemyKind.Charger => (48 + Time * 0.16f, 92, 19, 20),
            EnemyKind.Elite => (300 + Time * 2.0f, 63, 27, 22),
            _ => (26000, 70, 32, 26)
        };
        enemy.Health = enemy.MaxHealth;
        if (kind == EnemyKind.Boss) enemy.Timer = 3;
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
