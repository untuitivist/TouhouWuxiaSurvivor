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
    public const string StardustUnlock = "marisa.stardust.unlock";
    public const string MasterSparkUnlock = "marisa.masterspark.unlock";
    public const string StarPierce = "marisa.stars.pierce";
    public const string StarSplit = "marisa.stars.split";
    public const string StardustRecall = "marisa.stardust.recall";
    public const string StardustEcho = "marisa.stardust.echo";
    public const string SparkSweep = "marisa.masterspark.sweep";
    public const string SparkClear = "marisa.masterspark.clear";
    public const string FinalSpark = "marisa.final-spark";
    public static readonly IReadOnlyList<UpgradeDefinition> All = Array.AsReadOnly(Create().ToArray());
    private static readonly IReadOnlyDictionary<string, UpgradeDefinition> ById = All.ToDictionary(upgrade => upgrade.Id, StringComparer.Ordinal);
    public static UpgradeDefinition Get(string id) => ById[id];
    public static UpgradeDefinition Legacy(ArtKind art) => All.Single(upgrade => upgrade.Ability == art && upgrade.Kind is UpgradeKind.Legacy or UpgradeKind.Recovery);

    private static IEnumerable<UpgradeDefinition> Create()
    {
        foreach (var art in ArtCatalog.All)
        {
            if (art.Owner is { } owner)
            {
                var prefix = owner.ToString().ToLowerInvariant() + "." + art.Id.ToString().ToLowerInvariant();
                if (!HeroCatalog.Get(owner).InitialAbilities.Contains(art.Id))
                    yield return new(prefix + ".unlock", art.Id, UpgradeKind.Unlock,
                        "解锁 · " + art.Name, art.Description, owner, 1);
                yield return new(prefix + ".refine", art.Id, UpgradeKind.Refine,
                    art.Name + " · 精进", "增加该能力的基础强度，不自动获得行为分支。", owner, art.MaxRank - 1, RequiredRank: 1);
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
        yield return Behavior(StarPierce, ArtKind.Stars, "星 · 贯穿", $"星弹额外贯穿一敌，单弹伤害降低 {(1 - MarisaTuning.PierceDamageMultiplier) * 100:0}%。可与分裂兼修。", AbilityTraits.StarPierce);
        yield return Behavior(StarSplit, ArtKind.Stars, "星 · 分裂", $"星弹首次命中时分出 {MarisaTuning.FragmentCount} 枚短程碎星，各造成 {MarisaTuning.FragmentDamageMultiplier * 100:0}% 单弹伤害；不回击原目标、不连锁分裂。", AbilityTraits.StarSplit);
        yield return Behavior(StardustRecall, ArtKind.Stardust, "尘 · 回旋", $"星屑飞行 {MarisaTuning.RecallDelay:0.00} 秒后返身，额外贯穿一敌，伤害降低 {(1 - MarisaTuning.RecallDamageMultiplier) * 100:0}%；缩短外射距离，同一星屑不重复命中同一敌人。", AbilityTraits.StardustRecall);
        yield return Behavior(StardustEcho, ArtKind.Stardust, "尘 · 双重星环", $"原地相隔 {MarisaTuning.EchoDelay:0.0} 秒放出两轮错位星环，每轮造成原伤害的 {MarisaTuning.EchoDamageMultiplier * 100:0}%；可与回旋兼修。", AbilityTraits.StardustEcho);
        yield return Behavior(SparkSweep, ArtKind.MasterSpark, "炮 · 横扫", $"魔炮从瞄准方向左侧扫向右侧，总角度 {MarisaTuning.SweepDegrees * 2:0}°；扩大覆盖，但不增加每次伤害。", AbilityTraits.SparkSweep);
        yield return Behavior(SparkClear, ArtKind.MasterSpark, "炮 · 消弹", $"魔炮每次伤害脉冲最多消除火线内 {MarisaTuning.BeamClearLimit} 发敌弹；蓄势期间不消弹，可与横扫兼修。", AbilityTraits.SparkClear);
        yield return new(FinalSpark, ArtKind.MasterSpark, UpgradeKind.Behavior, "解锁 · 满蓄势魔炮",
            "已学魔炮后，解锁满蓄势自动发动的强化 Master Spark 与发动时全屏消弹。此前只积累蓄势。", HeroKind.Marisa, 1, AbilityTraits.FinalSpark, 1, 5);
    }

    private static UpgradeDefinition Behavior(string id, ArtKind ability, string name, string description, AbilityTraits trait)
        => new(id, ability, UpgradeKind.Behavior, name, description, ArtCatalog.Get(ability).Owner, 1, trait, 1, 3);

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
        Take(candidates.Where(upgrade => upgrade.Kind == UpgradeKind.Unlock).ToArray());
        Take(candidates.Where(upgrade => upgrade.Kind == UpgradeKind.Behavior).ToArray());
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
