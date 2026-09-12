namespace Rebirth.Core;

public enum MainlineStance { None, RapidOfuda, ChargedOfuda, YoungStars, MatureStars }

public static class MainlineGrowth
{
    public const string RapidOfuda = "reimu.ofuda.stance.rapid";
    public const string ChargedOfuda = "reimu.ofuda.stance.charged";
    public const string YoungStars = "marisa.stars.stance.young";
    public const string MatureStars = "marisa.stars.stance.mature";
    public const string OfudaPower = "reimu.ofuda.training.power";
    public const string OfudaTempo = "reimu.ofuda.training.tempo";
    public const string StarPower = "marisa.stars.training.power";
    public const int FirstChoice = 4;
    public const int TempoLimit = 8;

    public static ArtKind Primary(HeroKind hero) => hero == HeroKind.Reimu ? ArtKind.Ofuda : ArtKind.Stars;
    public static string Name(MainlineStance stance) => stance switch
    {
        MainlineStance.RapidOfuda => "疾札", MainlineStance.ChargedOfuda => "叠札",
        MainlineStance.YoungStars => "星潮", MainlineStance.MatureStars => "星域", _ => "尚未定式"
    };
    public static float Strength(int rank) => 1 + 0.28f * MathF.Sqrt(rank);
    public static float Power(BuildState build) => Strength(build.TrainingRank(build.Hero == HeroKind.Reimu ? OfudaPower : StarPower));
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
            OfudaPower or StarPower => Strength(rank + 1) > Strength(rank),
            OfudaTempo => rank < TempoLimit,
            UpgradeCatalog.StarMass => MassMedian(rank + 1) > MassMedian(rank),
            UpgradeCatalog.StarSpread => Spread(rank + 1) > Spread(rank),
            UpgradeCatalog.StarLifetime => Lifetime(rank + 1) > Lifetime(rank),
            _ => false
        };
    }
    public static AbilityStats OfudaStats(BuildState build)
    {
        var basis = AbilityTuning.Get(ArtKind.Ofuda, build.Ranks[(int)ArtKind.Ofuda]);
        var damage = basis.Damage * Power(build);
        var interval = basis.Interval / (1 + Math.Min(TempoLimit, build.TrainingRank(OfudaTempo)) * 0.045f);
        return build.Stance switch
        {
            MainlineStance.RapidOfuda => basis with { Damage = damage * 0.68f, Interval = interval * 0.6f },
            MainlineStance.ChargedOfuda => basis with { Damage = damage * 2.28f, Interval = interval * 2 },
            _ => basis with { Damage = damage, Interval = interval }
        };
    }
    public static float StarAgeMultiplier(MainlineStance stance, float duration, float remaining)
    {
        var age = Math.Max(0, duration - remaining);
        return stance switch
        {
            MainlineStance.YoungStars => age < Math.Min(3, duration * 0.45f) ? 1.6f : 1,
            MainlineStance.MatureStars => 1 + 0.5f * Math.Clamp((age - 1.5f) / 3, 0, 1),
            _ => 1
        };
    }
    public static string TrainingDescription(UpgradeDefinition upgrade, BuildState build)
    {
        var rank = build.TrainingRank(upgrade.Id);
        return upgrade.Id switch
        {
            OfudaPower or StarPower => GameText.Format($"主线威力 {Strength(rank):0.00} → {Strength(rank + 1):0.00} 倍；后续相对收益渐缓。"),
            OfudaTempo => GameText.Format($"出手效率 +4.5%；上限 {TempoLimit} 重，保留疾札与叠札的节律差异。"),
            UpgradeCatalog.StarMass => GameText.Format($"新星质量中位数 {MassMedian(rank):0.00} → {MassMedian(rank + 1):0.00}；独立抽样，无保底重星。"),
            UpgradeCatalog.StarSpread => GameText.Format($"质量离散参数 {Spread(rank):0.00} → {Spread(rank + 1):0.00}；扩大轻重两端，不固定抽样结果。"),
            UpgradeCatalog.StarLifetime => GameText.Format($"新星寿命 {Lifetime(rank):0.0} → {Lifetime(rank + 1):0.0} 秒；满 {MarisaTuning.StarLimit} 星时暂停生成，旧星不续命。"),
            _ => upgrade.Description
        };
    }
}
