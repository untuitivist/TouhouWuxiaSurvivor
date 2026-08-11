using Godot;
using TouhouWuxiaSurvivor.Content;
using TouhouWuxiaSurvivor.Gameplay.Progression.Runtime;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;

namespace TouhouWuxiaSurvivor.Gameplay.Progression.Definitions;

/// <summary>
/// 集中声明本体首批局内升级，并从未满重项目中抽取互不重复的升级选项。
/// </summary>
public static class RunUpgradeCatalog
{
    private static IReadOnlyList<RunUpgradeDefinition>? _all;

    public static IReadOnlyList<RunUpgradeDefinition> All => _all ??= CreateAll();

    /// <summary>
    /// 按稳定 ID 查找升级，供符卡、前置条件和测试避免依赖目录下标。
    /// </summary>
    public static RunUpgradeDefinition? FindById(string id) => All.FirstOrDefault(
        definition => string.Equals(definition.Id, id, StringComparison.Ordinal));

    /// <summary>
    /// 按唯一运行效果类型返回基础或无尽修行；通用 SpellCard 类型必须改用稳定 ID 查询。
    /// </summary>
    public static RunUpgradeDefinition GetRequiredByKind(RunUpgradeKind kind)
    {
        if (kind == RunUpgradeKind.SpellCard)
        {
            throw new InvalidOperationException("Spell card upgrades require a stable upgrade id.");
        }

        return All.Single(definition => definition.Kind == kind);
    }

    /// <summary>
    /// 使用独立随机源洗牌可升级项目并返回指定数量，已满重项目不会占据选择位。
    /// </summary>
    public static IReadOnlyList<RunUpgradeDefinition> CreateOffer(
        RandomNumberGenerator random,
        RunBuildState build,
        ContentPackSelection content,
        int choiceCount = 3)
    {
        var candidates = All.Where(definition =>
            (definition.RequiredContentPack is null ||
                content.IsEnabled(definition.RequiredContentPack)) &&
            build.CanUpgrade(definition)).ToList();
        for (int index = candidates.Count - 1; index > 0; index--)
        {
            int swapIndex = random.RandiRange(0, index);
            (candidates[index], candidates[swapIndex]) =
                (candidates[swapIndex], candidates[index]);
        }

        return candidates.Take(Math.Max(0, choiceCount)).ToArray();
    }

    /// <summary>
    /// 使用菜单当前选择提供兼容入口；正式世界应传入自己的不可变内容快照以免跨局串包。
    /// </summary>
    public static IReadOnlyList<RunUpgradeDefinition> CreateOffer(
        RandomNumberGenerator random,
        RunBuildState build,
        int choiceCount = 3) => CreateOffer(
            random, build, ContentPackSelectionService.Current, choiceCount);

    /// <summary>
    /// 建立六项有限基础修行、三项后期无尽修行和全部内容包符卡解锁，目录顺序保持稳定。
    /// </summary>
    private static IReadOnlyList<RunUpgradeDefinition> CreateAll()
    {
        var definitions = new List<RunUpgradeDefinition>
        {
            new("needle_damage", "封魔针法", RunUpgradeKind.NeedleDamage,
                RunUpgradeCategory.MartialArt, 5, "弹丸伤害 +1"),
            new("hakurei_breathing", "博丽呼吸法", RunUpgradeKind.FireRate,
                RunUpgradeCategory.InnerArt, 5, "射击速度 +12%"),
            new("tengu_step", "天狗步", RunUpgradeKind.MoveSpeed,
                RunUpgradeCategory.InnerArt, 5, "移动速度 +8%"),
            new("soul_seeking", "追魂诀", RunUpgradeKind.TargetRange,
                RunUpgradeCategory.MartialArt, 5, "索敌范围 +10%"),
            new("wind_riding", "御风诀", RunUpgradeKind.ProjectileSpeed,
                RunUpgradeCategory.InnerArt, 5, "弹丸速度 +12%"),
            new("spirit_gathering", "聚灵诀", RunUpgradeKind.SpiritAttraction,
                RunUpgradeCategory.InnerArt, 5, "灵息吸引范围 +25%"),
            new("endless_damage", "真元淬锋", RunUpgradeKind.EndlessDamage,
                RunUpgradeCategory.MartialArt, int.MaxValue, "弹丸伤害持续提高",
                new RunUpgradeRequirement("needle_damage", 5), isRepeatable: true),
            new("endless_fire_rate", "周天吐纳", RunUpgradeKind.EndlessFireRate,
                RunUpgradeCategory.InnerArt, int.MaxValue, "射击速度以递减幅度持续提高",
                new RunUpgradeRequirement("hakurei_breathing", 5), isRepeatable: true),
            new("endless_move_speed", "无相身法", RunUpgradeKind.EndlessMoveSpeed,
                RunUpgradeCategory.InnerArt, int.MaxValue, "移动速度以递减幅度持续提高",
                new RunUpgradeRequirement("tengu_step", 5), isRepeatable: true),
        };
        definitions.AddRange(SpellCardCatalog.All.Select(CreateSpellCardUpgrade));
        return definitions;
    }

    /// <summary>
    /// 从同一张符卡定义投影一次性悟得选项，来源包、前置和显示文案不再平行硬编码。
    /// </summary>
    private static RunUpgradeDefinition CreateSpellCardUpgrade(SpellCardDefinition card) => new(
        card.UnlockUpgradeId,
        card.FullName,
        RunUpgradeKind.SpellCard,
        RunUpgradeCategory.SpellCard,
        1,
        $"自动施展：{card.EffectDescription}",
        new RunUpgradeRequirement(card.PrerequisiteUpgradeId, card.MinimumRank),
        card.SourcePackId,
        card.Id);
}
