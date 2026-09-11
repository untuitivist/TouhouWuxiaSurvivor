using System.Numerics;
using System.Runtime.CompilerServices;

namespace Rebirth.Core;

public enum HeroKind { Reimu, Marisa }
public enum RunPhase { Playing, Choosing, Paused, Won, Lost }
public enum EnemyKind { Kedama, Fairy, Charger, Elite, Boss }
public enum ArtKind { Ofuda, YinYang, Boundary, Stars, Herbs, MasterSpark, Power, Haste, Vitality, Flow, Recovery }
public enum EffectKind { Hit, Defeat, Graze, Hurt, Dash, Spell, Seal, Beam, Explosion, Level, Boss, Victory }

public readonly record struct FrameInput(Vector2 Move, bool Focus = false, bool Dash = false);
public readonly record struct CombatEvent(EffectKind Kind, Vector2 Position, Vector2 Target, float Value = 0);

public sealed class Enemy
{
    public int Id;
    public EnemyKind Kind;
    public Vector2 Position;
    public Vector2 Velocity;
    public Vector2 GravityVelocity;
    public Vector2 GravityAcceleration;
    public float Mass = EnemyMassCatalog.Kedama;
    public Vector2 Aim;
    public float Health;
    public float MaxHealth;
    public float Radius;
    public float Speed;
    public float Timer;
    public float Telegraph;
    public float Flash;
    public float ContactDamage;
    public bool Charging;
    public float BoundRemaining;
    public float StarHitDisplayDamage;
}

public struct Projectile
{
    public Vector2 Position;
    public Vector2 Velocity;
    public float Life;
    public float Radius;
    public float Damage;
    public int Pierce;
    public ArtKind Art;
    public bool Hostile;
    public bool Grazed;
    public bool Alternate;
    public float TurnRate;
    public int TargetId;
    public bool DreamOrb;
    public bool Blast;
    public int ClearBudget;
    public int TintIndex;
    public HitHistory HitIds;
}

public sealed class BeamState
{
    public Vector2 Direction;
    public Vector2 AimDirection;
    public bool Steering;
    public bool ClearsBullets;
    public float Duration;
    public float Warmup;
    public float Remaining;
    public float PulseTimer;
    public float Damage;
    public float HalfWidth;
    public float Length;
    public bool Signature;
}

public sealed class BoundaryField
{
    public Vector2 Position;
    public float Remaining;
    public float PulseTimer;
    public float HalfSize;
    public float Damage;
}

public struct Pickup
{
    public Vector2 Position;
    public int Value;
    public bool Healing;
    public bool Herbal;
    public float Life;
    public bool Attracted;
    public bool Collected;
}

public sealed class Seal
{
    public required string Name;
    public Vector2 Position;
    public float Charge;
    public bool Complete;
}

public static class Geometry
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float AngleDelta(float from, float to)
    {
        var delta = to - from;
        if (delta >= MathF.PI) return delta - MathF.Tau;
        if (delta <= -MathF.PI) return delta + MathF.Tau;
        return delta;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Length(Vector2 vector) => MathF.Sqrt(vector.X * vector.X + vector.Y * vector.Y);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float DistanceSquared(Vector2 first, Vector2 second)
    {
        var horizontal = first.X - second.X;
        var vertical = first.Y - second.Y;
        return horizontal * horizontal + vertical * vertical;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 Advance(Vector2 position, Vector2 velocity, float seconds)
        => new(position.X + velocity.X * seconds, position.Y + velocity.Y * seconds);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 Direction(Vector2 vector)
    {
        var lengthSquared = vector.X * vector.X + vector.Y * vector.Y;
        if (lengthSquared <= 0.0001f) return Vector2.UnitY;
        var inverseLength = 1 / MathF.Sqrt(lengthSquared);
        return new(vector.X * inverseLength, vector.Y * inverseLength);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 Angle(float radians) => new(MathF.Cos(radians), MathF.Sin(radians));
    public static Vector2 Rotate(Vector2 vector, float radians) => new(vector.X * MathF.Cos(radians) - vector.Y * MathF.Sin(radians), vector.X * MathF.Sin(radians) + vector.Y * MathF.Cos(radians));
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float SegmentDistanceSquared(Vector2 point, Vector2 start, Vector2 end)
    {
        var segmentX = end.X - start.X;
        var segmentY = end.Y - start.Y;
        var offsetX = point.X - start.X;
        var offsetY = point.Y - start.Y;
        var length = segmentX * segmentX + segmentY * segmentY;
        var fraction = length < 0.0001f ? 0 : Math.Clamp((offsetX * segmentX + offsetY * segmentY) / length, 0, 1);
        var distanceX = point.X - (start.X + segmentX * fraction);
        var distanceY = point.Y - (start.Y + segmentY * fraction);
        return distanceX * distanceX + distanceY * distanceY;
    }
}
