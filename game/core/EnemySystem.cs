using System.Numerics;

namespace Rebirth.Core;

internal static class EnemySystem
{
    internal static void Step(RunState run)
    {
        foreach (var enemy in run.Enemies.Active)
        {
            if (enemy.Health <= 0) continue;
            var offset = new Vector2(run.PlayerPosition.X - enemy.Position.X, run.PlayerPosition.Y - enemy.Position.Y);
            var distance = Geometry.Length(offset);
            var direction = distance > 0.01f ? new Vector2(offset.X / distance, offset.Y / distance) : Vector2.UnitY;
            enemy.Timer -= RunState.StepSeconds;
            enemy.Flash = Math.Max(0, enemy.Flash - RunState.StepSeconds);
            enemy.Velocity = new(direction.X * enemy.Speed, direction.Y * enemy.Speed);
            if (enemy.Kind == EnemyKind.Fairy)
            {
                enemy.Velocity *= distance > 320 ? 1 : distance < 230 ? -0.7f : 0;
                if (enemy.Timer <= 0)
                {
                    run.ShootFan(enemy.Position, direction, run.Time > 130 ? 3 : 1, 0.16f, 160, 13);
                    enemy.Timer = 2.6f;
                }
            }
            if (enemy.Kind == EnemyKind.Charger) UpdateCharger(enemy, direction);
            if (enemy.Kind == EnemyKind.Elite && enemy.Timer <= 0)
            {
                run.ShootRing(enemy.Position, 12, run.Time * 0.3f, 135, false);
                enemy.Timer = 3.2f;
            }
            if (enemy.Kind == EnemyKind.Boss) BossAbilitySystem.Step(run, enemy, direction, distance);
            if (enemy.BoundRemaining > 0)
            {
                enemy.Velocity *= enemy.Kind == EnemyKind.Boss ? ReimuTuning.BossSlowMultiplier : 0;
                enemy.BoundRemaining = Math.Max(0, enemy.BoundRemaining - RunState.StepSeconds);
            }
            if (enemy.GravityAcceleration.X != 0 || enemy.GravityAcceleration.Y != 0 || enemy.GravityVelocity.X != 0 || enemy.GravityVelocity.Y != 0)
            {
                enemy.GravityVelocity = MarisaTuning.IntegrateGravity(enemy.GravityVelocity, enemy.GravityAcceleration, MarisaTuning.EnemyGravityDrag, MarisaTuning.EnemyGravityVelocityLimit);
                enemy.Velocity.X += enemy.GravityVelocity.X;
                enemy.Velocity.Y += enemy.GravityVelocity.Y;
            }
            enemy.Position = RunState.ClampToArena(Geometry.Advance(enemy.Position, enemy.Velocity, RunState.StepSeconds));
            var contactRadius = enemy.Radius + 6;
            if (Geometry.DistanceSquared(enemy.Position, run.PlayerPosition) < contactRadius * contactRadius) run.Hurt(enemy.ContactDamage);
        }
    }

    private static void UpdateCharger(Enemy enemy, Vector2 direction)
    {
        if (enemy.Telegraph > 0)
        {
            enemy.Telegraph -= RunState.StepSeconds;
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

}
