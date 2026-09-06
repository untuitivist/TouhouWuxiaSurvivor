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
        var enemy = new Enemy { Id = ++nextEnemyId, Kind = kind, Position = position, Timer = 1.2f + RandomFloat() * 1.8f };
        (enemy.MaxHealth, enemy.Speed, enemy.Radius, enemy.ContactDamage) = kind switch
        {
            EnemyKind.Kedama => (18 + Time * 0.095f, 70 + Math.Min(Time, 240) * 0.14f, 14, 13),
            EnemyKind.Fairy => (32 + Time * 0.13f, 68, 16, 15),
            EnemyKind.Charger => (48 + Time * 0.16f, 92, 19, 20),
            EnemyKind.Elite => (300 + Time * 2.0f, 63, 27, 22),
            _ => (6200, 70, 32, 26)
        };
        enemy.Health = enemy.MaxHealth;
        if (kind == EnemyKind.Boss) enemy.Timer = 3;
        Enemies.Add(enemy);
        return enemy;
    }

    private void UpdateEnemies()
    {
        foreach (var enemy in Enemies)
        {
            if (enemy.Health <= 0) continue;
            var offset = PlayerPosition - enemy.Position;
            var distance = offset.Length();
            var direction = Geometry.Direction(offset);
            enemy.Timer -= StepSeconds;
            enemy.Flash = Math.Max(0, enemy.Flash - StepSeconds);
            enemy.Velocity = direction * enemy.Speed;
            if (enemy.Kind == EnemyKind.Fairy)
            {
                enemy.Velocity *= distance > 320 ? 1 : distance < 230 ? -0.7f : 0;
                if (enemy.Timer <= 0)
                {
                    ShootFan(enemy.Position, direction, Time > 130 ? 3 : 1, 0.16f, 160, 13);
                    enemy.Timer = 2.6f;
                }
            }
            if (enemy.Kind == EnemyKind.Charger) UpdateCharger(enemy, direction);
            if (enemy.Kind == EnemyKind.Elite && enemy.Timer <= 0)
            {
                ShootRing(enemy.Position, 12, Time * 0.3f, 135, false);
                enemy.Timer = 3.2f;
            }
            if (enemy.Kind == EnemyKind.Boss) UpdateBoss(enemy, direction, distance);
            enemy.Position = ClampToArena(enemy.Position + enemy.Velocity * StepSeconds);
            if (Vector2.DistanceSquared(enemy.Position, PlayerPosition) < MathF.Pow(enemy.Radius + 6, 2)) Hurt(enemy.ContactDamage);
        }
    }

    private void UpdateCharger(Enemy enemy, Vector2 direction)
    {
        if (enemy.Telegraph > 0)
        {
            enemy.Telegraph -= StepSeconds;
            enemy.Velocity = Vector2.Zero;
            if (enemy.Telegraph <= 0) { enemy.Charging = true; enemy.Timer = 0.6f; }
        }
        else if (enemy.Charging)
        {
            enemy.Velocity = enemy.Aim * 410;
            if (enemy.Timer <= 0) { enemy.Charging = false; enemy.Timer = 2.3f; }
        }
        else if (enemy.Timer <= 0)
        {
            enemy.Telegraph = 0.85f;
            enemy.Aim = direction;
        }
    }

    private void UpdateBoss(Enemy enemy, Vector2 direction, float distance)
    {
        enemy.Velocity *= distance > 290 ? 1 : distance < 200 ? -0.7f : 0;
        enemy.Telegraph = enemy.Timer < 0.7f ? 0.7f - enemy.Timer : 0;
        if (enemy.Timer > 0) return;
        var phase = enemy.Health > enemy.MaxHealth * 0.66f ? 0 : enemy.Health > enemy.MaxHealth * 0.33f ? 1 : 2;
        var sequence = (int)((Time - BossArrival) / 2.2f);
        ShootRing(enemy.Position, 16 + phase * 4, sequence * 0.29f, 125 + phase * 15, phase > 0);
        if (phase > 0) ShootFan(enemy.Position, direction, 5, 0.17f, 205, 16);
        if (phase == 2) ShootRing(enemy.Position, 16, -sequence * 0.41f, 92, true);
        enemy.Timer = 2.35f - phase * 0.18f;
    }

    private void ShootFan(Vector2 origin, Vector2 direction, int count, float spread, float speed, float damage)
    {
        for (var index = 0; index < count; index++)
            AddProjectile(new() { Position = origin, Velocity = Geometry.Rotate(direction, (index - (count - 1) / 2f) * spread) * speed, Radius = 5, Damage = damage, Life = 7, Hostile = true });
    }

    private void ShootRing(Vector2 origin, int count, float rotation, float speed, bool alternate)
    {
        for (var index = 0; index < count; index++)
            AddProjectile(new() { Position = origin, Velocity = Geometry.Angle(rotation + MathF.Tau * index / count) * speed, Radius = 5, Damage = 15, Life = 7, Hostile = true, Alternate = alternate });
    }

    private void AddProjectile(Projectile projectile)
    {
        if (Projectiles.Count < ProjectileLimit) Projectiles.Add(projectile);
    }
}
