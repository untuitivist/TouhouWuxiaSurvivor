namespace Rebirth.Core;

public readonly record struct AbilityStats(int Count, float Damage, float Interval, float Range, float Duration = 0);

public static class AbilityTuning
{
    public const float BeamWarmup = 0.45f;
    public const float BeamPulse = 0.12f;
    public const float BoundaryPulse = 0.35f;

    public static AbilityStats Get(ArtKind kind, int rank)
    {
        if (rank is < 1 or > 5) throw new ArgumentOutOfRangeException(nameof(rank));
        return kind switch
        {
            ArtKind.Ofuda => new(rank + 1, 13 + rank * 7, 0.57f, 760),
            ArtKind.YinYang => new(rank + 1, 8 + rank * 5, 0.22f, rank == 5 ? 115 : 85),
            ArtKind.Boundary => new(1, 9 + rank * 10, 5.5f, 95 + rank * 25, 1.95f + rank * 0.35f),
            ArtKind.Stars => new(MarisaTuning.BaseStarCount, rank == 5 ? 2.6f : 0.25f + rank * 0.45f, MarisaTuning.StarInterval, MarisaTuning.TargetRange, MarisaTuning.StarLifetime),
            ArtKind.Herbs => new(1, rank == 5 ? 24 : 2 + rank * 4, MarisaTuning.HerbInterval, 0, MarisaTuning.HerbLifetime),
            ArtKind.MasterSpark => new(1, 5 + rank * 4, 4.2f, 820 + rank * 30, 1.85f + rank * 0.4f),
            _ => throw new ArgumentException("Not a character ability", nameof(kind))
        };
    }

    public static float BeamHalfWidth(int rank) => 18 + rank * 8;
}
