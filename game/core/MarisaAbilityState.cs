using System.Numerics;

namespace Rebirth.Core;

public sealed class MarisaAbilityState(int seed)
{
    public float ShotCooldown;
    public float HerbCooldown;
    public float BeamCooldown;
    public float PendingHealing;
    internal int GravityTick;
    public int GravityInteractions;
    internal float EmissionAngle = (seed & 1023) * MathF.Tau / 1024;
    internal readonly Random MassRandom = new(unchecked(seed ^ 0x4D415253));
    internal readonly Random VisualRandom = new(unchecked(seed ^ 0x53544152));
}

public struct StarBody
{
    public Vector2 Position;
    public Vector2 Velocity;
    public Vector2 Acceleration;
    public float Mass;
    public float DamageRate;
    public float Life;
    public float Duration;
    public float PulseTimer;
    public float Rotation;
    public int VisualSeed;
    public bool Resonating;
    public readonly bool Planet => Mass >= EnemyMassCatalog.Elite;
}
