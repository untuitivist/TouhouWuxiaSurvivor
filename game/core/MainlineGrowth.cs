namespace Rebirth.Core;

public static class MainlineGrowth
{
    public const string OfudaPower = "reimu.ofuda.training.power";
    public const string OfudaTempo = "reimu.ofuda.training.tempo";
    public const string StarPower = "marisa.stars.training.power";
    public const string BoundaryPower = "reimu.boundary.training.power";
    public const string YinYangPower = "reimu.yinyang.training.power";
    public const string SparkPower = "marisa.masterspark.training.power";
    public const string HerbPotency = "marisa.herbs.training.potency";
    public const int TempoLimit = 8;

    public static ArtKind Primary(HeroKind hero) => hero == HeroKind.Reimu ? ArtKind.Ofuda : ArtKind.Stars;
    public static string PowerId(ArtKind ability) => ability switch
    {
        ArtKind.Ofuda => OfudaPower, ArtKind.Stars => StarPower,
        ArtKind.Boundary => BoundaryPower, ArtKind.YinYang => YinYangPower,
        ArtKind.MasterSpark => SparkPower, ArtKind.Herbs => HerbPotency,
        _ => throw new ArgumentException("Not a character ability", nameof(ability))
    };
    public static float Strength(int rank) => 1 + 0.28f * MathF.Sqrt(rank);
    public static float Power(BuildState build) => Power(build, Primary(build.Hero));
    public static float Power(BuildState build, ArtKind ability)
        => ability == ArtKind.Herbs ? 1 : Strength(build.TrainingRank(PowerId(ability)));
    public static float MassMedian(int rank)
        => MarisaTuning.BaseMassMedian * MathF.Pow(MarisaTuning.MassGrowth, Math.Min(rank, 6))
            * MathF.Pow(1 + Math.Max(0, rank - 6) * 0.12f, 0.7f);
    public static float Spread(int rank) => MarisaTuning.BaseMassSigma + 0.12f * MathF.Sqrt(rank);
    public static float Lifetime(int rank) => MarisaTuning.StarLifetime + rank * MarisaTuning.ExtendedLifetime;
    public static bool HasEffectiveGain(string id, int rank)
    {
        if (rank == int.MaxValue) return false;
        return id switch
        {
            OfudaPower or StarPower or BoundaryPower or YinYangPower or SparkPower => Strength(rank + 1) > Strength(rank),
            HerbPotency => rank < int.MaxValue - 14,
            OfudaTempo => rank < TempoLimit,
            UpgradeCatalog.StarMass => MassMedian(rank + 1) > MassMedian(rank),
            UpgradeCatalog.StarSpread => Spread(rank + 1) > Spread(rank),
            UpgradeCatalog.StarLifetime => Lifetime(rank + 1) > Lifetime(rank),
            _ => false
        };
    }
    public static AbilityStats Stats(BuildState build, ArtKind ability)
    {
        var basis = AbilityTuning.Get(ability, build.Ranks[(int)ability]);
        var damage = ability == ArtKind.Herbs
            ? Math.Min(int.MaxValue, (long)basis.Damage + build.TrainingRank(HerbPotency))
            : basis.Damage * Power(build, ability);
        return basis with { Damage = damage };
    }
    public static AbilityStats OfudaStats(BuildState build)
    {
        var stats = Stats(build, ArtKind.Ofuda);
        return stats with { Interval = stats.Interval / (1 + Math.Min(TempoLimit, build.TrainingRank(OfudaTempo)) * 0.045f) };
    }
    public static string TrainingDescription(UpgradeDefinition upgrade, BuildState build)
    {
        var rank = build.TrainingRank(upgrade.Id);
        return upgrade.Id switch
        {
            OfudaPower or StarPower or BoundaryPower or YinYangPower or SparkPower => GameText.Format($"本术威力 {Strength(rank):0.00} → {Strength(rank + 1):0.00} 倍；不影响其他术式，后续相对收益渐缓。"),
            HerbPotency => GameText.Format($"每份新药恢复量额外 +{rank} → +{rank + 1}；不加快生成，不提高存放上限，旧药不变。"),
            OfudaTempo => GameText.Format($"御札出手效率 +4.5%；上限 {TempoLimit} 重，可与威力和行为升级共同生效。"),
            UpgradeCatalog.StarMass => GameText.Format($"新星质量中位数 {MassMedian(rank):0.00} → {MassMedian(rank + 1):0.00}；独立抽样，无保底重星。"),
            UpgradeCatalog.StarSpread => GameText.Format($"质量离散参数 {Spread(rank):0.00} → {Spread(rank + 1):0.00}；扩大轻重两端，不固定抽样结果。"),
            UpgradeCatalog.StarLifetime => GameText.Format($"新星寿命 {Lifetime(rank):0.0} → {Lifetime(rank + 1):0.0} 秒；满 {MarisaTuning.StarLimit} 星时暂停生成，旧星不续命。"),
            _ => upgrade.Description
        };
    }
}
