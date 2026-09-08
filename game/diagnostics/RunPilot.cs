using System.Numerics;
using Rebirth.Core;

namespace Rebirth.Diagnostics;

public static class RunPilot
{
    public static void ResolveChoices(RunState run)
    {
        while (run.Phase == RunPhase.Choosing)
        {
            var preferred = run.Choices.FindIndex(art => art.Owner == run.Hero);
            run.Choose(preferred < 0 ? 0 : preferred);
        }
    }

    public static FrameInput Input(RunState run, int tick)
    {
        var target = run.Seals.FirstOrDefault(seal => !seal.Complete)?.Position ?? (run.Boss?.Position ?? Vector2.Zero);
        var direction = target - run.PlayerPosition;
        var desiredDistance = run.PurifiedSeals < 3 ? 25 : 190;
        var movement = direction.Length() > desiredDistance ? Geometry.Direction(direction) : Vector2.Zero;
        foreach (var enemy in run.Enemies)
        {
            var away = run.PlayerPosition - enemy.Position;
            var distance = away.Length();
            if (distance < 95 && distance > 0) movement += away / distance * (1 - distance / 95) * 2.5f;
        }
        foreach (var projectile in run.Projectiles)
        {
            if (!projectile.Hostile) continue;
            var away = run.PlayerPosition - projectile.Position - projectile.Velocity * 0.18f;
            var distance = away.Length();
            if (distance < 55 && distance > 0) movement += away / distance * (1 - distance / 55) * 2;
        }
        return new(movement, false, run.Health < run.MaxHealth * 0.5f && tick % 180 == 0);
    }
}
