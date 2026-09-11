namespace Rebirth.Core;

[Flags]
public enum AbilityTraits
{
    None = 0, Homing = 1, Blast = 2, Cluster = 4, Bind = 8, Clear = 16, Launch = 32, DreamSeal = 64,
    StarMass = 128, StarSpread = 256, HerbBrew = 512, HerbReserve = 1024,
    SparkSteer = 2048, SparkClear = 4096, FinalSpark = 8192, StarLifetime = 16384,
    StarPlanet = 32768, SparkWide = 65536, SparkResonance = 131072
}

public enum UpgradeKind { Unlock, Refine, Behavior, Legacy, Recovery }

public sealed record UpgradeDefinition(string Id, ArtKind Ability, UpgradeKind Kind, string Name, string Description,
    HeroKind? Owner, int MaxRank, AbilityTraits Trait = AbilityTraits.None, int RequiredRank = 0, int MinimumLevel = 1)
{
    public ArtDefinition Art => ArtCatalog.Get(Ability);
    public string Category => Kind switch
    {
        UpgradeKind.Unlock => "解锁能力", UpgradeKind.Behavior => "行为升级", UpgradeKind.Refine => "术式精进",
        UpgradeKind.Recovery => "即时恢复", _ => Owner.HasValue ? "角色修习" : "通用修习"
    };
}

public sealed class BuildState
{
    public HeroKind Hero { get; }
    public int[] Ranks { get; } = new int[ArtCatalog.All.Length];
    public AbilityTraits Traits { get; private set; }
    public int AllocatedPoints { get; private set; }
    public bool SignatureUnlocked => Has(Hero == HeroKind.Reimu ? AbilityTraits.DreamSeal : AbilityTraits.FinalSpark);

    public BuildState(HeroKind hero)
    {
        Hero = hero;
        foreach (var ability in HeroCatalog.Get(hero).InitialAbilities) Ranks[(int)ability] = 1;
    }

    public bool Has(AbilityTraits trait) => (Traits & trait) == trait;
    public int Rank(UpgradeDefinition upgrade) => upgrade.Kind switch
    {
        UpgradeKind.Behavior => Has(upgrade.Trait) ? 1 : 0,
        UpgradeKind.Unlock => Ranks[(int)upgrade.Ability] > 0 ? 1 : 0,
        UpgradeKind.Refine => Math.Max(0, Ranks[(int)upgrade.Ability] - 1),
        UpgradeKind.Recovery => 0,
        _ => Ranks[(int)upgrade.Ability]
    };

    public bool CanChoose(UpgradeDefinition upgrade, int level)
        => (upgrade.Owner == null || upgrade.Owner == Hero) && level >= upgrade.MinimumLevel
        && Ranks[(int)upgrade.Ability] >= upgrade.RequiredRank && Rank(upgrade) < upgrade.MaxRank;

    public bool TryApply(UpgradeDefinition upgrade, int level)
    {
        if (!CanChoose(upgrade, level)) return false;
        if (upgrade.Kind == UpgradeKind.Behavior) Traits |= upgrade.Trait;
        else if (upgrade.Kind is not UpgradeKind.Recovery) Ranks[(int)upgrade.Ability]++;
        AllocatedPoints++;
        return true;
    }
}

public sealed record HeroDefinition(HeroKind Id, string Name, float Health, float Power, float Speed, IReadOnlyList<ArtKind> InitialAbilities);

public static class HeroCatalog
{
    private static readonly HeroDefinition Reimu = new(HeroKind.Reimu, "博丽灵梦", 110, 1, 205, Array.AsReadOnly(new[] { ArtKind.Ofuda }));
    private static readonly HeroDefinition Marisa = new(HeroKind.Marisa, "雾雨魔理沙", 100, 1, 220, Array.AsReadOnly(new[] { ArtKind.Stars }));
    public static HeroDefinition Get(HeroKind hero) => hero switch
    {
        HeroKind.Reimu => Reimu, HeroKind.Marisa => Marisa, _ => throw new ArgumentOutOfRangeException(nameof(hero))
    };
}
