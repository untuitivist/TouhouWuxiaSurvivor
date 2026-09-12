using System.Numerics;
using System.Runtime.CompilerServices;

namespace Rebirth.Core;

public static class MarisaTuning
{
    public const int StarLimit = 96;
    public const int BaseStarCount = 1;
    public const float StarInterval = 0.18f;
    public const float StarLifetime = 8;
    public const float ExtendedLifetime = 0.6f;
    public const float StarPulse = 0.12f;
    public const float StarSpeed = 180;
    public const float EmissionAngleStep = 2.39996323f;
    public const float BaseMassMedian = 2.5f;
    public const float BaseMassSigma = 0.6f;
    public const float MassGrowth = 1.32f;
    public const float TargetRange = 800;
    public const float GravityStrength = 250000;
    public const float GravitySoftening = 70;
    public const int GravityIntervalTicks = 3;
    public const float GravityAccelerationLimit = 900;
    public const float StarVelocityLimit = 360;
    public const float EnemyGravityVelocityLimit = 180;
    public const float StarDrag = 1.7f;
    public const float EnemyGravityDrag = 2;
    public const float ResonanceMultiplier = 1.25f;
    public const float SteeringRadians = MathF.PI;
    public const float WideMultiplier = 1.6f;
    public const int BeamClearLimit = 18;
    public const float HerbInterval = 11;
    public const float HerbLifetime = 22;
    public const int HerbLimit = 3;
    public const float BrewRate = 2;
    public const float BrewLimit = 18;

    public static float MassMedian(BuildState build)
        => MainlineGrowth.MassMedian(build.TrainingRank(UpgradeCatalog.StarMass));
    public static float MassSigma(BuildState build)
        => MainlineGrowth.Spread(build.TrainingRank(UpgradeCatalog.StarSpread));
    public static float Lifetime(BuildState build) => MainlineGrowth.Lifetime(build.TrainingRank(UpgradeCatalog.StarLifetime));
    public static float DamageRadius(float mass) => Math.Min(90, 10 + MathF.Sqrt(mass) * 4);
    public static float VisualSize(float mass) => Math.Min(96, 14 + MathF.Sqrt(mass) * 8);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 LimitVector(Vector2 velocity, float limit)
    {
        var maximum = Math.Max(Math.Abs(velocity.X), Math.Abs(velocity.Y));
        if (maximum == 0 || maximum <= limit && velocity.X * velocity.X + velocity.Y * velocity.Y <= limit * limit) return velocity;
        var scaledX = velocity.X / maximum;
        var scaledY = velocity.Y / maximum;
        var multiplier = limit / MathF.Sqrt(scaledX * scaledX + scaledY * scaledY);
        return new(scaledX * multiplier, scaledY * multiplier);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 IntegrateGravity(Vector2 velocity, Vector2 acceleration, float drag, float velocityLimit)
    {
        var limited = LimitVector(acceleration, GravityAccelerationLimit);
        var damping = 1 + drag * RunState.StepSeconds;
        return LimitVector(new((velocity.X + limited.X * RunState.StepSeconds) / damping,
            (velocity.Y + limited.Y * RunState.StepSeconds) / damping), velocityLimit);
    }

    public static float RollMass(Random random, BuildState build)
    {
        var normal = (float)(Math.Sqrt(-2 * Math.Log(1 - random.NextDouble())) * Math.Cos(Math.Tau * random.NextDouble()));
        var logMass = MathF.Log(MassMedian(build)) + MassSigma(build) * normal;
        return MathF.Exp(Math.Clamp(logMass, -12, 60));
    }
}
