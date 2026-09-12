namespace Rebirth.Core;

[Flags]
public enum AbilityTraits
{
    None = 0, Homing = 1, Blast = 2, Cluster = 4, Bind = 8, Clear = 16, Launch = 32, DreamSeal = 64,
    StarMass = 128, StarSpread = 256, HerbBrew = 512, HerbReserve = 1024,
    SparkSteer = 2048, SparkClear = 4096, FinalSpark = 8192, StarLifetime = 16384,
    SparkWide = 65536, SparkResonance = 131072
}

public enum UpgradeKind { Unlock, Refine, Behavior, Legacy, Recovery, Training, Stance }

public sealed record UpgradeDefinition(string Id, ArtKind Ability, UpgradeKind Kind, string Name, string Description,
    HeroKind? Owner, int MaxRank, AbilityTraits Trait = AbilityTraits.None, int RequiredRank = 0, int MinimumLevel = 1, MainlineStance Stance = MainlineStance.None)
{
    public ArtDefinition Art => ArtCatalog.Get(Ability);
    public string Category => Kind switch
    {
        UpgradeKind.Unlock => "解锁能力", UpgradeKind.Behavior => "行为升级", UpgradeKind.Refine => "术式精进", UpgradeKind.Training => "术式精进",
        UpgradeKind.Recovery => "即时恢复", UpgradeKind.Stance => "关键定式", _ => Owner.HasValue ? "角色修习" : "通用修习"
    };
}

public sealed class BuildState
{
    private readonly Dictionary<string, int> trainingRanks = new(StringComparer.Ordinal);
    public HeroKind Hero { get; }
    public int[] Ranks { get; } = new int[ArtCatalog.All.Length];
    public AbilityTraits Traits { get; private set; }
    public int AllocatedPoints { get; private set; }
    public MainlineStance Stance { get; private set; }
    public bool SignatureUnlocked => Has(Hero == HeroKind.Reimu ? AbilityTraits.DreamSeal : AbilityTraits.FinalSpark);

    public BuildState(HeroKind hero)
    {
        Hero = hero;
        foreach (var ability in HeroCatalog.Get(hero).InitialAbilities) Ranks[(int)ability] = 1;
    }

    public int TrainingRank(string id) => trainingRanks.GetValueOrDefault(id);
    public bool Has(AbilityTraits trait) => (Traits & trait) == trait;
    public int Rank(UpgradeDefinition upgrade) => upgrade.Kind switch
    {
        UpgradeKind.Behavior => Has(upgrade.Trait) ? 1 : 0,
        UpgradeKind.Training => TrainingRank(upgrade.Id),
        UpgradeKind.Stance => Stance == upgrade.Stance ? 1 : 0,
        UpgradeKind.Unlock => Ranks[(int)upgrade.Ability] > 0 ? 1 : 0,
        UpgradeKind.Refine => Math.Max(0, Ranks[(int)upgrade.Ability] - 1),
        UpgradeKind.Recovery => 0,
        _ => Ranks[(int)upgrade.Ability]
    };

    public bool CanChoose(UpgradeDefinition upgrade, int level)
        => UpgradeCatalog.IsKnown(upgrade) && (upgrade.Owner == null || upgrade.Owner == Hero) && level >= upgrade.MinimumLevel
        && Ranks[(int)upgrade.Ability] >= upgrade.RequiredRank && Rank(upgrade) < upgrade.MaxRank
        && (upgrade.Kind != UpgradeKind.Stance || Stance == MainlineStance.None && AllocatedPoints >= MainlineGrowth.FirstChoice - 1)
        && (upgrade.Kind != UpgradeKind.Training || MainlineGrowth.HasEffectiveGain(upgrade.Id, Rank(upgrade)));

    public bool TryApply(UpgradeDefinition upgrade, int level)
    {
        if (!CanChoose(upgrade, level)) return false;
        if (upgrade.Kind == UpgradeKind.Stance) Stance = upgrade.Stance;
        else if (upgrade.Kind == UpgradeKind.Training)
        {
            trainingRanks[upgrade.Id] = TrainingRank(upgrade.Id) + 1;
            Traits |= upgrade.Trait;
        }
        else if (upgrade.Kind == UpgradeKind.Behavior) Traits |= upgrade.Trait;
        else if (upgrade.Kind is not UpgradeKind.Recovery) Ranks[(int)upgrade.Ability]++;
        AllocatedPoints = Math.Min(int.MaxValue - 1, AllocatedPoints) + 1;
        return true;
    }
}

public sealed record HeroDefinition(HeroKind Id, string Name, float Health, float Power, float Speed, IReadOnlyList<ArtKind> InitialAbilities);

public static class HeroCatalog
{
    public static HeroDefinition Get(HeroKind hero) => CharacterCatalog.Get(hero).Playable;
}
