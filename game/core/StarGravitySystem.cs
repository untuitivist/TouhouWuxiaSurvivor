using System.Numerics;

namespace Rebirth.Core;

internal static class StarGravitySystem
{
    internal static Vector2 AccelerationPerMass(Vector2 offset)
    {
        var distanceSquared = offset.LengthSquared();
        var rangeSquared = MarisaTuning.TargetRange * MarisaTuning.TargetRange;
        if (distanceSquared >= rangeSquared) return Vector2.Zero;
        var softened = distanceSquared + MarisaTuning.GravitySoftening * MarisaTuning.GravitySoftening;
        var falloff = 1 - distanceSquared / rangeSquared;
        return offset * (MarisaTuning.GravityStrength * falloff * falloff / (softened * MathF.Sqrt(softened)));
    }

    internal static void Step(RunState run)
    {
        if (run.Marisa.GravityTick++ % MarisaTuning.GravityIntervalTicks != 0) return;
        run.Marisa.GravityInteractions = 0;
        foreach (var enemy in run.Enemies) enemy.GravityAcceleration = Vector2.Zero;
        var stars = run.Stars.Active;
        foreach (ref var star in stars) star.Acceleration = Vector2.Zero;
        for (var first = 0; first < stars.Length; first++)
        {
            ref var star = ref stars[first];
            if (star.Life <= 0) continue;
            for (var second = first + 1; second < stars.Length; second++)
            {
                ref var other = ref stars[second];
                if (other.Life <= 0) continue;
                var acceleration = AccelerationPerMass(other.Position - star.Position);
                if (acceleration == Vector2.Zero) continue;
                run.Marisa.GravityInteractions++;
                star.Acceleration += acceleration * other.Mass;
                other.Acceleration -= acceleration * star.Mass;
            }
            foreach (var enemy in run.World.Grid.Query(star.Position, MarisaTuning.TargetRange))
            {
                if (enemy.Health <= 0) continue;
                var acceleration = AccelerationPerMass(enemy.Position - star.Position);
                if (acceleration == Vector2.Zero) continue;
                run.Marisa.GravityInteractions++;
                star.Acceleration += acceleration * enemy.Mass;
                enemy.GravityAcceleration -= acceleration * star.Mass;
            }
        }
    }
}
