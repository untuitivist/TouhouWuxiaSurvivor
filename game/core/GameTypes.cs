using System.Numerics;

namespace Rebirth.Core;

public enum HeroKind { Reimu, Marisa }
public enum RunPhase { Playing, Choosing, Paused, Won, Lost }
public enum EnemyKind { Kedama, Fairy, Charger, Elite, Boss }
public enum ArtKind { Ofuda, YinYang, Boundary, Stars, Stardust, MasterSpark, Power, Haste, Vitality, Flow, Recovery }
public enum EffectKind { Hit, Defeat, Graze, Hurt, Dash, Spell, Seal, Beam, Explosion, Level, Boss, Victory }

public readonly record struct FrameInput(Vector2 Move, bool Focus = false, bool Dash = false);
public readonly record struct CombatEvent(EffectKind Kind, Vector2 Position, Vector2 Target, float Value = 0);

public sealed class Enemy
{
    public int Id;
    public EnemyKind Kind;
    public Vector2 Position;
    public Vector2 Velocity;
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
    public int TintIndex;
    public HitHistory HitIds;
}

public sealed class BeamState
{
    public Vector2 Direction;
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
    public static Vector2 Direction(Vector2 vector) => vector.LengthSquared() > 0.0001f ? Vector2.Normalize(vector) : Vector2.UnitY;
    public static Vector2 Angle(float radians) => new(MathF.Cos(radians), MathF.Sin(radians));
    public static Vector2 Rotate(Vector2 vector, float radians) => new(vector.X * MathF.Cos(radians) - vector.Y * MathF.Sin(radians), vector.X * MathF.Sin(radians) + vector.Y * MathF.Cos(radians));
    public static float SegmentDistanceSquared(Vector2 point, Vector2 start, Vector2 end)
    {
        var segment = end - start;
        var length = segment.LengthSquared();
        var fraction = length < 0.0001f ? 0 : Math.Clamp(Vector2.Dot(point - start, segment) / length, 0, 1);
        return Vector2.DistanceSquared(point, start + segment * fraction);
    }
}
