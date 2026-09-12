using System.Numerics;
using System.Runtime.CompilerServices;

namespace Rebirth.Core;

internal static class StarGravitySystem
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float AccelerationFactor(float offsetX, float offsetY)
    {
        var distanceSquared = offsetX * offsetX + offsetY * offsetY;
        var rangeSquared = MarisaTuning.TargetRange * MarisaTuning.TargetRange;
        if (distanceSquared >= rangeSquared) return 0;
        var softened = distanceSquared + MarisaTuning.GravitySoftening * MarisaTuning.GravitySoftening;
        var falloff = 1 - distanceSquared / rangeSquared;
        return MarisaTuning.GravityStrength * falloff * falloff / (softened * MathF.Sqrt(softened));
    }

    internal static Vector2 AccelerationPerMass(Vector2 offset)
    {
        var factor = AccelerationFactor(offset.X, offset.Y);
        return new(offset.X * factor, offset.Y * factor);
    }

    internal static void Step(RunState run)
    {
        if (run.Marisa.GravityTick++ % MarisaTuning.GravityIntervalTicks != 0) return;
        var interactions = 0;
        run.PlayerGravityAcceleration = Vector2.Zero;
        foreach (var enemy in run.Enemies.Active) enemy.GravityAcceleration = Vector2.Zero;
        var stars = run.Stars.Active;
        foreach (ref var star in stars) star.Acceleration = Vector2.Zero;
        for (var first = 0; first < stars.Length; first++)
        {
            ref var star = ref stars[first];
            if (star.Life <= 0) continue;
            var positionX = star.Position.X;
            var positionY = star.Position.Y;
            var mass = star.Mass;
            var accumulatedX = star.Acceleration.X;
            var accumulatedY = star.Acceleration.Y;
            for (var second = first + 1; second < stars.Length; second++)
            {
                ref var other = ref stars[second];
                if (other.Life <= 0) continue;
                var offsetX = other.Position.X - positionX;
                var offsetY = other.Position.Y - positionY;
                var factor = AccelerationFactor(offsetX, offsetY);
                if (factor == 0 || offsetX == 0 && offsetY == 0) continue;
                var accelerationX = offsetX * factor;
                var accelerationY = offsetY * factor;
                interactions++;
                accumulatedX += accelerationX * other.Mass;
                accumulatedY += accelerationY * other.Mass;
                other.Acceleration.X -= accelerationX * mass;
                other.Acceleration.Y -= accelerationY * mass;
            }
            foreach (var enemy in run.Enemies.Active)
            {
                if (enemy.Health <= 0 || enemy.Id == star.OwnerId) continue;
                var offsetX = enemy.Position.X - positionX;
                var offsetY = enemy.Position.Y - positionY;
                var factor = AccelerationFactor(offsetX, offsetY);
                if (factor == 0 || offsetX == 0 && offsetY == 0) continue;
                var accelerationX = offsetX * factor;
                var accelerationY = offsetY * factor;
                interactions++;
                accumulatedX += accelerationX * enemy.Mass;
                accumulatedY += accelerationY * enemy.Mass;
                enemy.GravityAcceleration.X -= accelerationX * mass;
                enemy.GravityAcceleration.Y -= accelerationY * mass;
            }
            if (star.Hostile)
            {
                var offsetX = run.PlayerPosition.X - positionX;
                var offsetY = run.PlayerPosition.Y - positionY;
                var factor = AccelerationFactor(offsetX, offsetY);
                if (factor != 0 && (offsetX != 0 || offsetY != 0))
                {
                    interactions++;
                    var acceleration = new Vector2(offsetX * factor, offsetY * factor);
                    accumulatedX += acceleration.X * run.PlayerMass;
                    accumulatedY += acceleration.Y * run.PlayerMass;
                    run.PlayerGravityAcceleration -= acceleration * mass;
                }
            }
            star.Acceleration = new(accumulatedX, accumulatedY);
        }
        run.Marisa.GravityInteractions = interactions;
    }
}
