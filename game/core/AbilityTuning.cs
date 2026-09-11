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
            ArtKind.Boundary => new(1, 12 + rank * 7, 5.5f, 105 + rank * 15, 2.1f + rank * 0.2f),
            ArtKind.Stars => new(MarisaTuning.BaseStarCount, 6.5f + rank * 1.5f, MarisaTuning.StarInterval, MarisaTuning.TargetRange, MarisaTuning.StarLifetime),
            ArtKind.Herbs => new(1, 4 + rank * 2, MarisaTuning.HerbInterval, 0, MarisaTuning.HerbLifetime),
            ArtKind.MasterSpark => new(1, 7 + rank * 2, 4.2f, 820 + rank * 30, 2 + rank * 0.25f),
            _ => throw new ArgumentException("Not a character ability", nameof(kind))
        };
    }

    public static float BeamHalfWidth(int rank) => 21 + rank * 5;
}
