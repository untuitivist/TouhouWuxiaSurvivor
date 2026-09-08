namespace Rebirth.Core;

public static class UpgradeCatalog
{
    public const string Homing = "reimu.ofuda.homing";
    public const string Blast = "reimu.ofuda.blast";
    public const string Cluster = "reimu.boundary.cluster";
    public const string Bind = "reimu.boundary.bind";
    public const string Clear = "reimu.yinyang.clear";
    public const string Launch = "reimu.yinyang.launch";
    public const string DreamSeal = "reimu.dream-seal";
    public const string BoundaryUnlock = "reimu.boundary.unlock";
    public const string YinYangUnlock = "reimu.yinyang.unlock";
    public const string Recovery = "shared.recovery";
    public static readonly IReadOnlyList<UpgradeDefinition> All = Array.AsReadOnly(Create().ToArray());
    private static readonly IReadOnlyDictionary<string, UpgradeDefinition> ById = All.ToDictionary(upgrade => upgrade.Id, StringComparer.Ordinal);
    public static UpgradeDefinition Get(string id) => ById[id];
    public static UpgradeDefinition Legacy(ArtKind art) => All.Single(upgrade => upgrade.Ability == art && upgrade.Kind is UpgradeKind.Legacy or UpgradeKind.Recovery);

    private static IEnumerable<UpgradeDefinition> Create()
    {
        foreach (var art in ArtCatalog.All)
        {
            if (art.Owner == HeroKind.Reimu)
            {
                if (art.Id != ArtKind.Ofuda)
                    yield return new(art.Id == ArtKind.Boundary ? BoundaryUnlock : YinYangUnlock, art.Id, UpgradeKind.Unlock,
                        "解锁 · " + art.Name, art.Description, HeroKind.Reimu, 1);
                yield return new("reimu." + art.Id.ToString().ToLowerInvariant() + ".refine", art.Id, UpgradeKind.Refine,
                    art.Name + " · 精进", "增加该能力的基础强度，不自动获得行为分支。", HeroKind.Reimu, art.MaxRank - 1, RequiredRank: 1);
            }
            else yield return new(art.Id == ArtKind.Recovery ? Recovery : "base." + art.Id.ToString().ToLowerInvariant(), art.Id,
                art.Id == ArtKind.Recovery ? UpgradeKind.Recovery : UpgradeKind.Legacy, art.Name, art.Description, art.Owner, art.MaxRank);
        }
        yield return Behavior(Homing, ArtKind.Ofuda, "追踪符", $"御札转向并重新索敌，单札伤害降低 {1 - ReimuTuning.HomingDamageMultiplier:P0}。可与爆炸兼修。", AbilityTraits.Homing);
        yield return Behavior(Blast, ArtKind.Ofuda, "爆炸符", $"命中后向半径 {ReimuTuning.BlastRadius:0} 内最多 {ReimuTuning.BlastTargetLimit} 名其他敌人溅射 {ReimuTuning.BlastMultiplier:P0} 单札伤害，不重复伤害直击目标。可与追踪兼修。", AbilityTraits.Blast);
        yield return Behavior(Cluster, ArtKind.Boundary, "阵 · 聚敌选点", $"在 {ReimuTuning.ClusterRange:0} 范围内的敌群密集处留阵，不再只放在脚下。可与禁锢兼修。", AbilityTraits.Cluster);
        yield return Behavior(Bind, ArtKind.Boundary, "阵 · 禁锢", $"阵内伤害脉冲定住普通敌人 {ReimuTuning.BindDuration:0.0} 秒；首领仅减速，不封锁弹幕。", AbilityTraits.Bind);
        yield return Behavior(Clear, ArtKind.YinYang, "玉 · 消弹", $"环绕玉接触敌弹时，每轮最多消除 {ReimuTuning.ClearLimit} 发；发射的玉同样可消弹。", AbilityTraits.Clear);
        yield return Behavior(Launch, ArtKind.YinYang, "玉 · 蓄力发射", $"自动蓄力 {ReimuTuning.OrbChargeDuration:0.0} 秒后射出一枚玉，贯穿三敌；飞行时少一枚护身玉，可与消弹兼修。", AbilityTraits.Launch);
        yield return new(DreamSeal, ArtKind.Ofuda, UpgradeKind.Behavior, "解锁 · 梦想封印",
            "解锁满蓄势自动发动的梦想封印。此前蓄势可以积累，但不会自动清弹或攻击。", HeroKind.Reimu, 1, AbilityTraits.DreamSeal, 1, 5);
    }

    private static UpgradeDefinition Behavior(string id, ArtKind ability, string name, string description, AbilityTraits trait)
        => new(id, ability, UpgradeKind.Behavior, name, description, HeroKind.Reimu, 1, trait, 1, 3);

    public static string Requirement(UpgradeDefinition upgrade)
    {
        var ability = upgrade.RequiredRank > 0 ? $"已获得 {upgrade.Art.Name}" : "无需其他能力";
        return upgrade.MinimumLevel > 1 ? $"{ability} · 修习 {upgrade.MinimumLevel}" : ability;
    }

    public static string Description(UpgradeDefinition upgrade, BuildState build)
        => upgrade.Kind is UpgradeKind.Refine or UpgradeKind.Legacy && upgrade.Art.Owner.HasValue
            ? ArtCatalog.UpgradeText(upgrade.Ability, build.Ranks[(int)upgrade.Ability]) : upgrade.Description;

    public static string Progress(UpgradeDefinition upgrade, BuildState build)
        => upgrade.Kind is UpgradeKind.Unlock or UpgradeKind.Behavior ? "一次领悟 · " + Requirement(upgrade)
            : upgrade.Kind == UpgradeKind.Recovery ? "即时生效" : $"{build.Rank(upgrade) + 1} / {upgrade.MaxRank} · " + Requirement(upgrade);

    public static IEnumerable<UpgradeDefinition> Behaviors(BuildState build, ArtKind ability)
        => All.Where(upgrade => upgrade.Ability == ability && upgrade.Kind == UpgradeKind.Behavior && build.Rank(upgrade) > 0);
}

public static class UpgradeOffers
{
    public static IReadOnlyList<UpgradeDefinition> Create(BuildState build, int level, Random random)
    {
        var candidates = UpgradeCatalog.All.Where(upgrade => upgrade.Kind != UpgradeKind.Recovery && build.CanChoose(upgrade, level)).ToList();
        var result = new List<UpgradeDefinition>(3);
        if (build.Hero == HeroKind.Reimu)
        {
            Take(candidates.Where(upgrade => upgrade.Kind == UpgradeKind.Unlock).ToArray());
            Take(candidates.Where(upgrade => upgrade.Kind == UpgradeKind.Behavior).ToArray());
        }
        while (result.Count < 3 && candidates.Count > 0) Take(candidates.ToArray());
        if (result.Count < 3) result.Add(UpgradeCatalog.Get(UpgradeCatalog.Recovery));
        return result;

        void Take(UpgradeDefinition[] pool)
        {
            if (pool.Length == 0 || result.Count == 3) return;
            var selected = pool[random.Next(pool.Length)];
            candidates.Remove(selected);
            result.Add(selected);
        }
    }
}
