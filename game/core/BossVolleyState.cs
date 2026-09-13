using System.Numerics;

namespace Rebirth.Core;

public sealed class BossVolleyState
{
    public Vector2 Origin { get; }
    public HeroKind Hero { get; }
    public int Phase { get; }
    public float GapAngle { get; }
    public const float GapHalfAngle = 0.32f;
    public const float WarningHalfAngle = 0.2f;
    public const float WarningStart = 120;
    public const float TelegraphSeconds = 1;
    public int BulletCount => 28 + Phase * 8;
    public int WaveCount => 2 + Phase;
    public float Warmup = TelegraphSeconds;
    public float WaveTimer;
    public int WavesFired;

    public BossVolleyState(Vector2 origin, Vector2 aim, HeroKind hero, int phase, int sequence)
    {
        Origin = origin;
        Hero = hero;
        Phase = phase;
        GapAngle = MathF.Atan2(aim.Y, aim.X) + MathF.Sin(sequence * 0.8f) * 0.35f;
    }

    public float Bearing(int index) => GapAngle + MathF.Tau * index / BulletCount;
    public bool IsGap(float bearing) => Math.Abs(Geometry.AngleDelta(GapAngle, bearing)) < GapHalfAngle
        || Math.Abs(Geometry.AngleDelta(GapAngle + MathF.PI, bearing)) < GapHalfAngle;
    public float Speed(int index) => 135 + Phase * 12 + (Phase > 0 ? MathF.Cos(index * MathF.Tau * 5 / BulletCount) * 22 : 0);
    public ArtKind Art(int wave) => Hero == HeroKind.Marisa ? ArtKind.Stars : Phase == 2 && wave % 2 == 1 ? ArtKind.YinYang : ArtKind.Ofuda;
    public float Radius(int wave) => Art(wave) == ArtKind.YinYang ? 8 : 5;
}
