using System.Numerics;

namespace Rebirth.Core;

public sealed partial class RunState
{
    private void UpdateWeapons()
    {
        swordTimer -= StepSeconds * CastSpeed;
        orbitTimer -= StepSeconds * CastSpeed;
        talismanTimer -= StepSeconds * CastSpeed;
        lightningTimer -= StepSeconds * CastSpeed;
        var target = NearestEnemy(PlayerPosition, 680);
        if (swordTimer <= 0 && target != null)
        {
            CastSwords(target);
            swordTimer = 0.53f;
        }
        var orbitRank = Ranks[(int)ArtKind.Orbit];
        if (orbitRank > 0 && orbitTimer <= 0)
        {
            for (var index = 0; index < orbitRank + 1; index++)
            {
                var position = PlayerPosition + Geometry.Angle(OrbitAngle + index * MathF.Tau / (orbitRank + 1)) * OrbitRadius;
                foreach (var enemy in grid.Query(position, 75))
                    if (Vector2.DistanceSquared(position, enemy.Position) < MathF.Pow(enemy.Radius + 28, 2))
                        DamageEnemy(enemy, (8 + orbitRank * 5) * Power, Geometry.Direction(enemy.Position - PlayerPosition) * 9);
            }
            orbitTimer = 0.19f;
        }
        var talismanRank = Ranks[(int)ArtKind.Talisman];
        if (talismanRank > 0 && talismanTimer <= 0 && target != null)
        {
            var count = talismanRank >= 5 ? 3 : talismanRank >= 3 ? 2 : 1;
            for (var index = 0; index < count; index++)
                AddProjectile(new() { Position = PlayerPosition, Velocity = Geometry.Rotate(Geometry.Direction(target.Position - PlayerPosition), (index - (count - 1) / 2f) * 0.19f) * 400, Radius = 10, Damage = (28 + talismanRank * 14) * Power, Life = 2, Art = ArtKind.Talisman });
            talismanTimer = 1.8f;
        }
        var lightningRank = Ranks[(int)ArtKind.Lightning];
        if (lightningRank > 0 && lightningTimer <= 0 && target != null)
        {
            CastLightning(target, lightningRank);
            lightningTimer = lightningRank >= 5 ? 1.05f : 1.65f;
        }
    }

    private void CastSwords(Enemy target)
    {
        var rank = Ranks[(int)ArtKind.Sword];
        var count = rank >= 5 ? 5 : 1 + rank / 2;
        var travel = Vector2.Distance(target.Position, PlayerPosition) / 680;
        var direction = Geometry.Direction(target.Position + target.Velocity * travel - PlayerPosition);
        for (var index = 0; index < count; index++)
        {
            AddProjectile(new()
            {
                Position = PlayerPosition,
                Velocity = Geometry.Rotate(direction, (index - (count - 1) / 2f) * 0.11f) * 680,
                Radius = 7,
                Damage = (17 + rank * 8) * Power,
                Life = 1.3f,
                Pierce = rank >= 5 ? 3 : rank >= 3 ? 1 : 0,
                Art = ArtKind.Sword
            });
        }
    }

    private void CastLightning(Enemy target, int rank)
    {
        var visited = new HashSet<int>();
        var origin = PlayerPosition;
        Enemy? current = target;
        for (var index = 0; index < (rank >= 5 ? 9 : rank + 1) && current != null; index++)
        {
            visited.Add(current.Id);
            Events.Add(new(EffectKind.Lightning, origin, current.Position));
            DamageEnemy(current, (22 + rank * 9) * Power, Vector2.Zero);
            origin = current.Position;
            current = NearestEnemy(origin, 240, visited);
        }
    }

    private Enemy? NearestEnemy(Vector2 origin, float range, HashSet<int>? excluded = null)
    {
        Enemy? nearest = null;
        var distance = range * range;
        foreach (var enemy in Enemies)
        {
            if (enemy.Health <= 0 || excluded?.Contains(enemy.Id) == true) continue;
            var candidate = Vector2.DistanceSquared(enemy.Position, origin);
            if (candidate >= distance) continue;
            distance = candidate;
            nearest = enemy;
        }
        return nearest;
    }

    private void UpdateProjectiles()
    {
        for (var index = Projectiles.Count - 1; index >= 0; index--)
        {
            if (index >= Projectiles.Count) continue;
            var projectile = Projectiles[index];
            var previous = projectile.Position;
            projectile.Position += projectile.Velocity * StepSeconds;
            projectile.Life -= StepSeconds;
            if (projectile.Hostile)
            {
                var distance = Geometry.SegmentDistanceSquared(PlayerPosition, previous, projectile.Position);
                if (distance < MathF.Pow(projectile.Radius + 5, 2))
                {
                    Hurt(projectile.Damage);
                    projectile.Life = 0;
                }
                else if (!projectile.Grazed && distance < 34 * 34 && DashDuration <= 0)
                {
                    projectile.Grazed = true;
                    Grazes++;
                    Qi = Math.Min(100, Qi + (Hero == HeroKind.Reimu ? 5 : 3.8f));
                    Emit(EffectKind.Graze, PlayerPosition);
                }
            }
            else
            {
                foreach (var enemy in grid.Query(projectile.Position, 80))
                {
                    if (projectile.HitIds.Contains(enemy.Id) || Geometry.SegmentDistanceSquared(enemy.Position, previous, projectile.Position) >= MathF.Pow(enemy.Radius + projectile.Radius, 2)) continue;
                    projectile.HitIds.Add(enemy.Id);
                    if (projectile.Art == ArtKind.Talisman) Explode(projectile.Position, projectile.Damage);
                    else DamageEnemy(enemy, projectile.Damage, Geometry.Direction(projectile.Velocity) * 4);
                    if (projectile.Pierce-- <= 0) { projectile.Life = 0; break; }
                }
            }
            if (projectile.Life <= 0)
            {
                if (index < Projectiles.Count && ReferenceEquals(Projectiles[index], projectile)) Projectiles.RemoveAt(index);
                else Projectiles.Remove(projectile);
            }
        }
    }

    private void Explode(Vector2 position, float damage)
    {
        var rank = Ranks[(int)ArtKind.Talisman];
        var radius = 65 + rank * 13;
        Emit(EffectKind.Explosion, position, radius);
        foreach (var enemy in grid.Query(position, radius + 35))
            if (Vector2.DistanceSquared(position, enemy.Position) < MathF.Pow(radius + enemy.Radius, 2))
                DamageEnemy(enemy, damage, Geometry.Direction(enemy.Position - position) * 12);
    }
}
