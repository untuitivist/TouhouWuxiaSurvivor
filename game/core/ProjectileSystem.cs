using System.Numerics;

namespace Rebirth.Core;

internal static class ProjectileSystem
{
    internal static void Step(RunState run)
    {
        var world = run.World;
        for (var index = 0; index < world.Projectiles.Count; index++)
        {
            ref var shield = ref world.Projectiles[index];
            if (shield.Life > 0 && !shield.Hostile && shield.ClearBudget > 0)
                ReimuAbilitySystem.ClearProjectiles(run, shield.Position, Geometry.Advance(shield.Position, shield.Velocity, RunState.StepSeconds), ref shield.ClearBudget);
        }
        for (var index = world.Projectiles.Count - 1; index >= 0; index--)
        {
            ref var projectile = ref world.Projectiles[index];
            if (projectile.Life <= 0) continue;
            if (projectile.TurnRate > 0 && !projectile.Hostile)
            {
                var target = world.Grid.FindById(projectile.TargetId);
                if (target?.Health <= 0 || (target != null && projectile.HitIds.Contains(target.Id))) target = null;
                target ??= run.NearestEnemy(projectile.Position, 1100, projectile.HitIds);
                projectile.TargetId = target?.Id ?? 0;
                if (target != null)
                {
                    var speed = Geometry.Length(projectile.Velocity);
                    var currentAngle = MathF.Atan2(projectile.Velocity.Y, projectile.Velocity.X);
                    var delta = new Vector2(target.Position.X - projectile.Position.X, target.Position.Y - projectile.Position.Y);
                    var angle = Geometry.AngleDelta(currentAngle, MathF.Atan2(delta.Y, delta.X));
                    var heading = currentAngle + Math.Clamp(angle, -projectile.TurnRate * RunState.StepSeconds, projectile.TurnRate * RunState.StepSeconds);
                    projectile.Velocity = new(MathF.Cos(heading) * speed, MathF.Sin(heading) * speed);
                }
            }
            var previous = projectile.Position;
            projectile.Position = Geometry.Advance(projectile.Position, projectile.Velocity, RunState.StepSeconds);
            projectile.Life -= RunState.StepSeconds;
            var minimumX = MathF.Min(previous.X, projectile.Position.X);
            var maximumX = MathF.Max(previous.X, projectile.Position.X);
            var minimumY = MathF.Min(previous.Y, projectile.Position.Y);
            var maximumY = MathF.Max(previous.Y, projectile.Position.Y);

            if (projectile.Hostile)
            {
                var interactionRadius = MathF.Max(34, projectile.Radius + 5);
                if (run.PlayerPosition.X < minimumX - interactionRadius || run.PlayerPosition.X > maximumX + interactionRadius
                    || run.PlayerPosition.Y < minimumY - interactionRadius || run.PlayerPosition.Y > maximumY + interactionRadius) continue;
                var distance = Geometry.SegmentDistanceSquared(run.PlayerPosition, previous, projectile.Position);
                var hitRadius = projectile.Radius + 5;
                if (distance < hitRadius * hitRadius)
                {
                    run.Hurt(projectile.Damage);
                    projectile.Life = 0;
                }
                else if (!projectile.Grazed && distance < 34 * 34 && run.DashDuration <= 0)
                {
                    projectile.Grazed = true;
                    run.Graze();
                }
            }
            else
            {
                foreach (var enemy in world.Grid.Query(projectile.Position, 80))
                {
                    var hitRadius = enemy.Radius + projectile.Radius;
                    if (enemy.Position.X < minimumX - hitRadius || enemy.Position.X > maximumX + hitRadius
                        || enemy.Position.Y < minimumY - hitRadius || enemy.Position.Y > maximumY + hitRadius) continue;
                    if (projectile.HitIds.Contains(enemy.Id) || Geometry.SegmentDistanceSquared(enemy.Position, previous, projectile.Position) >= hitRadius * hitRadius) continue;
                    projectile.HitIds.Add(enemy.Id);
                    if (projectile.DreamOrb) run.Explode(projectile.Position, projectile.Damage);
                    else
                    {
                        var knockback = Geometry.Direction(projectile.Velocity);
                        run.DamageEnemy(enemy, projectile.Damage, new(knockback.X * 4, knockback.Y * 4));
                        if (projectile.Blast) ReimuAbilitySystem.Blast(run, projectile.Position, projectile.Damage, enemy.Id);
                    }
                    if (projectile.Pierce-- <= 0) { projectile.Life = 0; break; }
                }
            }
        }
        world.Projectiles.RemoveAll(static projectile => projectile.Life <= 0);
    }
}
