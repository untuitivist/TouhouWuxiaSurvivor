namespace Rebirth.Core;

public sealed class MarisaAbilityState(int seed)
{
    public float ShotCooldown;
    public float HerbCooldown;
    public float BeamCooldown;
    public float PendingHealing;
    internal int NextGroup;
    internal readonly Random MassRandom = new(unchecked(seed ^ 0x4D415253));
}

public struct StarBody
{
    public System.Numerics.Vector2 Position;
    public System.Numerics.Vector2 Anchor;
    public float Mass;
    public float DamageRate;
    public float Life;
    public float Duration;
    public float PulseTimer;
    public float TargetTimer;
    public float OrbitAngle;
    public float OrbitScale;
    public int TargetId;
    public int GroupId;
    public bool Planet;
    public bool Resonating;
}
