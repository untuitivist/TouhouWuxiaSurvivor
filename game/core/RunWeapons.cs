using System.Numerics;

namespace Rebirth.Core;

public sealed partial class RunState
{

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
            var projectile = Projectiles[index];
            if (projectile.TurnRate > 0 && !projectile.Hostile)
            {
                var target = Enemies.Find(enemy => enemy.Id == projectile.TargetId && enemy.Health > 0 && !projectile.HitIds.Contains(enemy.Id));
                target ??= NearestEnemy(projectile.Position, 1100, projectile.HitIds);
                projectile.TargetId = target?.Id ?? 0;
                if (target != null)
                {
                    var speed = projectile.Velocity.Length();
                    var currentAngle = MathF.Atan2(projectile.Velocity.Y, projectile.Velocity.X);
                    var delta = target.Position - projectile.Position;
                    var angle = MathF.Atan2(delta.Y, delta.X) - currentAngle;
                    angle = MathF.Atan2(MathF.Sin(angle), MathF.Cos(angle));
                    projectile.Velocity = Geometry.Angle(currentAngle + Math.Clamp(angle, -projectile.TurnRate * StepSeconds, projectile.TurnRate * StepSeconds)) * speed;
                }
            }
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
                    SpellCharge = Math.Min(100, SpellCharge + (Hero == HeroKind.Reimu ? 5 : 3.8f));
                    Emit(EffectKind.Graze, PlayerPosition);
                }
            }
            else
            {
                foreach (var enemy in grid.Query(projectile.Position, 80))
                {
                    if (projectile.HitIds.Contains(enemy.Id) || Geometry.SegmentDistanceSquared(enemy.Position, previous, projectile.Position) >= MathF.Pow(enemy.Radius + projectile.Radius, 2)) continue;
                    projectile.HitIds.Add(enemy.Id);
                    if (projectile.DreamOrb) Explode(projectile.Position, projectile.Damage);
                    else DamageEnemy(enemy, projectile.Damage, Geometry.Direction(projectile.Velocity) * 4);
                    if (projectile.Pierce-- <= 0) { projectile.Life = 0; break; }
                }
            }
            if (projectile.Life <= 0) Projectiles.RemoveAt(index);
        }
    }

    private void Explode(Vector2 position, float damage)
    {
        const float radius = 72;
        Emit(EffectKind.Explosion, position, radius);
        foreach (var enemy in grid.Query(position, radius + 35))
            if (Vector2.DistanceSquared(position, enemy.Position) < MathF.Pow(radius + enemy.Radius, 2))
                DamageEnemy(enemy, damage, Geometry.Direction(enemy.Position - position) * 12);
    }
}
