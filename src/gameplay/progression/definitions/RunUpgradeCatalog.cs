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
    /// 使用统一亲和生成器返回普通升级定义兼容视图；新运行链应读取完整候选以支持特化。
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
            build.CanUpgrade(definition))
            .Select(definition => new RunUpgradeChoice(definition))
            .ToList();
        var result = new List<RunUpgradeDefinition>();
        while (result.Count < Math.Max(0, choiceCount) && candidates.Count > 0)
        {
            double[] weights = RunOfferWeightTable.Create(build, candidates);
            double roll = random.Randf() * weights.Sum();
            int selectedIndex = candidates.Count - 1;
            for (int index = 0; index < candidates.Count; index++)
            {
                roll -= weights[index];
                if (roll <= 0.0)
                {
                    selectedIndex = index;
                    break;
                }
            }

            result.Add(candidates[selectedIndex].Definition);
            bool selectedSpell = candidates[selectedIndex].Definition.Category ==
                RunUpgradeCategory.SpellCard;
            candidates.RemoveAt(selectedIndex);
            if (selectedSpell)
            {
                candidates.RemoveAll(choice =>
                    choice.Definition.Category == RunUpgradeCategory.SpellCard);
            }
        }

        return result;
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
    /// 建立六项有限基础修行、六项后期无尽修行和全部内容包符卡解锁，目录顺序保持稳定。
    /// </summary>
    private static IReadOnlyList<RunUpgradeDefinition> CreateAll()
    {
        var definitions = new List<RunUpgradeDefinition>(BaseRunUpgradeFactory.CreateAll());
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
        string.Equals(card.SourcePackId, ContentPackCatalog.Base.Id, StringComparison.Ordinal)
            ? null
            : card.SourcePackId,
        card.Id,
        affinities: SpellCardAffinityResolver.Resolve(card));
}
