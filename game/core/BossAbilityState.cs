using System.Numerics;

namespace Rebirth.Core;

public sealed class BossAbilityState
{
    public HeroKind Hero { get; }
    public BuildState Build { get; }
    public MarisaAbilityState Marisa { get; }
    public BeamState? Beam;
    public BoundaryField? Field;
    public float Elapsed;
    public float ShotCooldown = 1.5f;
    public float SpecialCooldown = 4;
    public float FieldCooldown = 5;
    public float OrbitPulse;
    public float RecoveryRemaining;
    public float RecoveryCooldown = 14;
    public int RemediesUsed;
    public int VolleyCount;
    public int Phase;
    public float OrbitAngle => Elapsed * 1.4f;
    public BossAbilityState(HeroKind hero, int seed)
    {
        Hero = hero;
        Build = new(hero);
        Marisa = new(seed);
        Build.Ranks[(int)(hero == HeroKind.Reimu ? ArtKind.Ofuda : ArtKind.Stars)] = 2;
    }

    public Vector2 OrbitPosition(Vector2 center, int index) => center + Geometry.Angle(OrbitAngle + index * MathF.Tau / 3) * 78;
}
