namespace Rebirth.Core;

public static class ReimuTuning
{
    public const float BlastRadius = 64;
    public const float BlastMultiplier = 0.35f;
    public const int BlastTargetLimit = 4;
    public const float HomingDamageMultiplier = 0.8f;
    public const float ClusterRange = 600;
    public const float BindDuration = 0.5f;
    public const float BossSlowMultiplier = 0.55f;
    public const int ClearLimit = 3;
    public const float ClearRadius = 22;
    public const float OrbChargeDuration = 0.8f;
    public const float OrbLaunchInterval = 5;
    public const float OrbFlightDuration = 1.4f;
    public const float OrbLaunchSpeed = 460;
    public const float OrbDamageMultiplier = 2;
}

public sealed class ReimuAbilityState
{
    public float ShotCooldown;
    public int VolleyCount;
    public float OrbitCooldown;
    public float FieldCooldown;
    public float LaunchCooldown;
    public float ChargeRemaining;
    public float OrbAbsentRemaining;
    public bool Charging;
}
