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
    public const string HerbsUnlock = "marisa.herbs.unlock";
    public const string MasterSparkUnlock = "marisa.masterspark.unlock";
    public const string StarMass = "marisa.stars.mass";
    public const string StarSpread = "marisa.stars.spread";
    public const string StarLifetime = "marisa.stars.lifetime";
    public const string HerbBrew = "marisa.herbs.brew";
    public const string HerbReserve = "marisa.herbs.reserve";
    public const string SparkSteer = "marisa.masterspark.steer";
    public const string SparkWide = "marisa.masterspark.wide";
    public const string SparkResonance = "marisa.masterspark.resonance";
    public const string SparkClear = "marisa.masterspark.clear";
    public const string FinalSpark = "marisa.final-spark";
    public static readonly IReadOnlyList<UpgradeDefinition> All = Array.AsReadOnly(Create().ToArray());
    private static readonly IReadOnlyDictionary<string, UpgradeDefinition> ById = All.ToDictionary(upgrade => upgrade.Id, StringComparer.Ordinal);
    public static UpgradeDefinition Get(string id) => ById[id];
    public static bool IsKnown(UpgradeDefinition upgrade) => ById.TryGetValue(upgrade.Id, out var known) && ReferenceEquals(known, upgrade);
    public static UpgradeDefinition Legacy(ArtKind art) => All.Single(upgrade => upgrade.Ability == art && upgrade.Kind is UpgradeKind.Legacy or UpgradeKind.Recovery);

    private static IEnumerable<UpgradeDefinition> Create()
    {
        yield return new(MainlineGrowth.RapidOfuda, ArtKind.Ofuda, UpgradeKind.Stance, "定式 · 疾札",
            "均匀连发，单札较轻，持续补伤。本局放弃叠札；追踪、爆炸和辅助仍可兼修。", HeroKind.Reimu, 1, RequiredRank: 1, Stance: MainlineStance.RapidOfuda);
        yield return new(MainlineGrowth.ChargedOfuda, ArtKind.Ofuda, UpgradeKind.Stance, "定式 · 叠札",
            "自动积蓄后集中出手，每札穿过一个额外目标，保留输出空档。本局放弃疾札；积蓄可移动，追踪和爆炸仍可兼修。", HeroKind.Reimu, 1, RequiredRank: 1, Stance: MainlineStance.ChargedOfuda);
        yield return new(MainlineGrowth.YoungStars, ArtKind.Stars, UpgradeKind.Stance, "定式 · 星潮",
            "新生星早段撕扯增强，随后回到基础。本局放弃星域；质量、变谱和寿命仍可成长，新星生效。", HeroKind.Marisa, 1, RequiredRank: 1, Stance: MainlineStance.YoungStars);
        yield return new(MainlineGrowth.MatureStars, ArtKind.Stars, UpgradeKind.Stance, "定式 · 星域",
            "星体逐渐进入较强的成熟阶段，建立星场需要时间。本局放弃星潮；不赠送重星，新星生效。", HeroKind.Marisa, 1, RequiredRank: 1, Stance: MainlineStance.MatureStars);
        yield return new(MainlineGrowth.OfudaPower, ArtKind.Ofuda, UpgradeKind.Training, "符 · 养威",
            "反复强化御札威力，不增加实体，不解锁另一侧定式。", HeroKind.Reimu, int.MaxValue, RequiredRank: 1, MinimumLevel: 3);
        yield return new(MainlineGrowth.OfudaTempo, ArtKind.Ofuda, UpgradeKind.Training, "符 · 行札",
            "有限强化出手效率，不把两种定式压成同一节律。", HeroKind.Reimu, MainlineGrowth.TempoLimit, RequiredRank: 1, MinimumLevel: 3);
        yield return Training(MainlineGrowth.StarPower, "星 · 撕扯精进", "反复强化单位质量撕扯，不增加星体数量。", AbilityTraits.None);
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
        yield return Training(StarMass, "星 · 增质", "提高新星质量分布中位数，前六重较快，之后相对收益渐缓；每颗独立抽样，无保底重星。", AbilityTraits.StarMass);
        yield return Training(StarSpread, "星 · 变谱", "扩大质量分布的离散程度，让轻星与稀有重星都更有机会出现；可反复修习，不指定行星名额。", AbilityTraits.StarSpread);
        yield return Training(StarLifetime, "星 · 长明", "新生星体存活时间增加 0.6 秒；可反复修习，不刷新已有星体寿命。", AbilityTraits.StarLifetime);
        yield return Behavior(HerbBrew, ArtKind.Herbs, "药 · 缓释", "拾取药菇后额外缓慢恢复其一半药量，每秒最多 2 点，待恢复量最多 18；不靠伤害或击杀无限吸血。", AbilityTraits.HerbBrew);
        yield return Behavior(HerbReserve, ArtKind.Herbs, "药 · 留药", "药菇保存时间增加 16 秒，在场上限由三份变为五份；满血时不消耗药菇。", AbilityTraits.HerbReserve);
        yield return Behavior(SparkSteer, ArtKind.MasterSpark, "炮 · 追敌", "魔炮持续瞄准最近的存活敌人并平滑转向；停步也会追踪，无目标时保持方向。", AbilityTraits.SparkSteer);
        yield return Behavior(SparkWide, ArtKind.MasterSpark, "炮 · 广域", "魔炮宽度增加 60%，每次伤害不变；可与追敌和共鸣兼修。", AbilityTraits.SparkWide);
        yield return Behavior(SparkResonance, ArtKind.MasterSpark, "炮 · 星光共鸣", "正在照射的魔炮使火线内星体的持续撕扯增强 25%；离开火线即结束，不增加质量或刷新寿命。", AbilityTraits.SparkResonance);
        yield return Behavior(SparkClear, ArtKind.MasterSpark, "炮 · 消弹", $"魔炮每次伤害脉冲最多消除火线内 {MarisaTuning.BeamClearLimit} 发敌弹；蓄势期间不消弹，可与追敌兼修。", AbilityTraits.SparkClear);
        yield return new(FinalSpark, ArtKind.MasterSpark, UpgradeKind.Behavior, "解锁 · 满蓄势魔炮",
            "已学魔炮后，解锁满蓄势自动发动的强化 Master Spark 与发动时全屏消弹。此前只积累蓄势。", HeroKind.Marisa, 1, AbilityTraits.FinalSpark, 1, 5);
    }

    private static UpgradeDefinition Training(string id, string name, string description, AbilityTraits trait)
        => new(id, ArtKind.Stars, UpgradeKind.Training, name, description, HeroKind.Marisa, int.MaxValue, trait, 1, 3);

    private static UpgradeDefinition Behavior(string id, ArtKind ability, string name, string description, AbilityTraits trait)
        => new(id, ability, UpgradeKind.Behavior, name, description, ArtCatalog.Get(ability).Owner, 1, trait, 1, 3);

    public static string Requirement(UpgradeDefinition upgrade)
    {
        if (upgrade.Kind == UpgradeKind.Stance) return "第四次成长起二选一 · 本局不可兼得";
        var ability = upgrade.RequiredRank > 0 ? $"已获得 {upgrade.Art.Name}" : "无需其他能力";
        return upgrade.MinimumLevel > 1 ? $"{ability} · 修习 {upgrade.MinimumLevel}" : ability;
    }

    public static string Description(UpgradeDefinition upgrade, BuildState build)
        => upgrade.Kind == UpgradeKind.Training ? MainlineGrowth.TrainingDescription(upgrade, build)
            : upgrade.Kind is UpgradeKind.Refine or UpgradeKind.Legacy && upgrade.Art.Owner.HasValue
            ? ArtCatalog.UpgradeText(upgrade.Ability, build.Ranks[(int)upgrade.Ability]) : upgrade.Description;

    public static string Progress(UpgradeDefinition upgrade, BuildState build)
        => upgrade.Kind == UpgradeKind.Training ? upgrade.MaxRank == int.MaxValue
            ? GameText.Format($"可反复修习 · 当前 {build.Rank(upgrade)} 重 · {Requirement(upgrade)}")
            : GameText.Format($"当前 {build.Rank(upgrade)} / {upgrade.MaxRank} 重 · {Requirement(upgrade)}")
            : upgrade.Kind is UpgradeKind.Unlock or UpgradeKind.Behavior or UpgradeKind.Stance ? "一次领悟 · " + Requirement(upgrade)
            : upgrade.Kind == UpgradeKind.Recovery ? "即时生效" : $"{build.Rank(upgrade) + 1} / {upgrade.MaxRank} · " + Requirement(upgrade);

    public static string LearnedName(UpgradeDefinition upgrade, BuildState build)
        => upgrade.Kind == UpgradeKind.Training ? GameText.Format($"{upgrade.Name} {build.Rank(upgrade)}重") : GameText.Get(upgrade.Name);

    public static IEnumerable<UpgradeDefinition> Behaviors(BuildState build, ArtKind ability)
        => All.Where(upgrade => upgrade.Ability == ability && upgrade.Kind is UpgradeKind.Behavior or UpgradeKind.Training or UpgradeKind.Stance && build.Rank(upgrade) > 0);
}

public static class UpgradeOffers
{
    public static IReadOnlyList<UpgradeDefinition> Create(BuildState build, int level, Random random)
    {
        var candidates = UpgradeCatalog.All.Where(upgrade => upgrade.Kind != UpgradeKind.Recovery && build.CanChoose(upgrade, level)).ToList();
        var result = new List<UpgradeDefinition>(3);
        var stances = candidates.Where(upgrade => upgrade.Kind == UpgradeKind.Stance).ToArray();
        if (stances.Length > 0)
        {
            foreach (var stance in stances) { result.Add(stance); candidates.Remove(stance); }
            Take(candidates.Where(upgrade => upgrade.Ability == MainlineGrowth.Primary(build.Hero)).ToArray());
        }
        else
        {
            Take(candidates.Where(upgrade => upgrade.Ability == MainlineGrowth.Primary(build.Hero)).ToArray());
            Take(candidates.Where(upgrade => build.Stance == MainlineStance.None ? upgrade.Kind == UpgradeKind.Unlock
                : upgrade.Kind is UpgradeKind.Unlock or UpgradeKind.Behavior or UpgradeKind.Legacy).ToArray());
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
