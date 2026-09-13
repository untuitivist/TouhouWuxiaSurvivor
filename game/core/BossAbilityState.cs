namespace Rebirth.Core;

public sealed class BossAbilityState
{
    public HeroKind Hero { get; }
    public BossBeamState? Beam;
    public BossVolleyState? Volley;
    public float Elapsed;
    public float ShotCooldown = 1.5f;
    public float SpecialCooldown = 5;
    public int VolleyCount;
    public int Phase;

    public BossAbilityState(HeroKind hero) => Hero = hero;
}
