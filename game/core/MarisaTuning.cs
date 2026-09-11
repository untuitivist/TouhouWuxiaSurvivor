using System.Numerics;

namespace Rebirth.Core;

public static class MarisaTuning
{
    public const int StarLimit = 32;
    public const int GroupLimit = 4;
    public const int BaseStarCount = 4;
    public const float MinimumStarMass = 1.2f;
    public const float MaximumStarMass = 4.8f;
    public const float MinimumPlanetMass = 6;
    public const float MaximumPlanetMass = 10;
    public const float HeavyMassMultiplier = 5f / 3;
    public const float StarInterval = 1.7f;
    public const float StarLifetime = 3.8f;
    public const float ExtendedLifetime = 1.4f;
    public const float StarPulse = 0.15f;
    public const float StarSpeed = 350;
    public const float PlanetSpeed = 230;
    public const float TargetInterval = 0.25f;
    public const float TargetRange = 650;
    public const int StarTargetLimit = 12;
    public const float PullSpeed = 125;
    public const float BossPullMultiplier = 0.12f;
    public const float ResonanceMultiplier = 1.25f;
    public const float SteeringRadians = MathF.PI;
    public const float WideMultiplier = 1.6f;
    public const int BeamClearLimit = 18;
    public const float HerbInterval = 11;
    public const float HerbLifetime = 22;
    public const int HerbLimit = 3;
    public const float BrewRate = 2;
    public const float BrewLimit = 18;

    public static int StarCount(BuildState build) => BaseStarCount + (build.Has(AbilityTraits.StarSpread) ? 2 : 0);
    public static float Lifetime(BuildState build) => StarLifetime + (build.Has(AbilityTraits.StarLifetime) ? ExtendedLifetime : 0);
    public static float DamageRadius(float mass) => 34 + MathF.Sqrt(mass) * 9;
    public static float PullRadius(float mass) => 95 + MathF.Sqrt(mass) * 16;
    public static Vector2 LimitVector(Vector2 velocity, float limit)
        => velocity.LengthSquared() > limit * limit ? Geometry.Direction(velocity) * limit : velocity;

    public static float RollMass(Random random, bool increasedMass, bool planet)
    {
        var minimum = planet ? MinimumPlanetMass : MinimumStarMass;
        var maximum = planet ? MaximumPlanetMass : MaximumStarMass;
        var mass = minimum + (float)random.NextDouble() * (maximum - minimum);
        return mass * (increasedMass ? HeavyMassMultiplier : 1);
    }
}
