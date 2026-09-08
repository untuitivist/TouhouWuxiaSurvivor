using System.Numerics;

namespace Rebirth.Core;

internal static class EnemySystem
{
    internal static void Step(RunState run)
    {
        foreach (var enemy in run.Enemies)
        {
            if (enemy.Health <= 0) continue;
            var offset = run.PlayerPosition - enemy.Position;
            var distance = offset.Length();
            var direction = Geometry.Direction(offset);
            enemy.Timer -= RunState.StepSeconds;
            enemy.Flash = Math.Max(0, enemy.Flash - RunState.StepSeconds);
            enemy.Velocity = direction * enemy.Speed;
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
            if (enemy.Kind == EnemyKind.Boss) UpdateBoss(run, enemy, direction, distance);
            if (enemy.BoundRemaining > 0)
            {
                enemy.Velocity *= enemy.Kind == EnemyKind.Boss ? ReimuTuning.BossSlowMultiplier : 0;
                enemy.BoundRemaining = Math.Max(0, enemy.BoundRemaining - RunState.StepSeconds);
            }
            enemy.Position = RunState.ClampToArena(enemy.Position + enemy.Velocity * RunState.StepSeconds);
            if (Vector2.DistanceSquared(enemy.Position, run.PlayerPosition) < MathF.Pow(enemy.Radius + 6, 2)) run.Hurt(enemy.ContactDamage);
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

    private static void UpdateBoss(RunState run, Enemy enemy, Vector2 direction, float distance)
    {
        enemy.Velocity *= distance > 290 ? 1 : distance < 200 ? -0.7f : 0;
        enemy.Telegraph = enemy.Timer < 0.7f ? 0.7f - enemy.Timer : 0;
        if (enemy.Timer > 0) return;
        var phase = enemy.Health > enemy.MaxHealth * 0.66f ? 0 : enemy.Health > enemy.MaxHealth * 0.33f ? 1 : 2;
        var sequence = (int)((run.Time - RunState.BossArrival) / 2.2f);
        run.ShootRing(enemy.Position, 16 + phase * 4, sequence * 0.29f, 125 + phase * 15, phase > 0);
        if (phase > 0) run.ShootFan(enemy.Position, direction, 5, 0.17f, 205, 16);
        if (phase == 2) run.ShootRing(enemy.Position, 16, -sequence * 0.41f, 92, true);
        enemy.Timer = 2.35f - phase * 0.18f;
    }

}
