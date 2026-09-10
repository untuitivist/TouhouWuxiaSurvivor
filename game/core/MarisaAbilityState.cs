namespace Rebirth.Core;

public sealed class MarisaAbilityState
{
    public float ShotCooldown;
    public float StardustCooldown;
    public float BeamCooldown;
    public float EchoRemaining;
    internal int EchoCount;
    internal float EchoAngle;
    internal Projectile EchoTemplate;
    internal readonly Projectile[] Fragments = new Projectile[MarisaTuning.FragmentQueueLimit];
    internal int FragmentCount;
}
