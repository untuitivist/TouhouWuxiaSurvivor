using Godot;
using TouhouWuxiaSurvivor.Content;
using TouhouWuxiaSurvivor.Gameplay.Progression.Definitions;

namespace TouhouWuxiaSurvivor.Gameplay.Progression.Runtime;

/// <summary>
/// 从统一横向内容池生成可复现的无放回候选；亲和仅由本局已选构筑产生并保留一个探索位。
/// </summary>
public sealed class RunOfferGenerator
{
    private const float AffinityStep = 0.32f;
    private const float OwnedRankBonus = 0.24f;
    private const float MaximumAffinityMultiplier = 3.4f;

    /// <summary>
    /// 先过滤内容、前置、互斥和重数，再从低亲和项目抽一个探索项并加权抽取其余项目。
    /// </summary>
    public IReadOnlyList<RunUpgradeChoice> CreateOffer(
        RandomNumberGenerator random,
        RunBuildState build,
        ContentPackSelection content,
        int runLevel,
        int choiceCount = 3)
    {
        int requested = Math.Max(0, choiceCount);
        var candidates = CreateCandidates(build, content, runLevel);
        if (requested == 0 || candidates.Count == 0)
        {
            return [];
        }

        var result = new List<RunUpgradeChoice>(requested);
        if (requested >= 3 && HasEstablishedAffinity(build))
        {
            RunUpgradeChoice? exploration = SelectExploration(random, build, candidates);
            if (exploration is not null)
            {
                result.Add(exploration.WithExploration(true));
                candidates.Remove(exploration);
                RemoveAdditionalSpellCards(result, candidates);
            }
        }

        while (result.Count < requested && candidates.Count > 0)
        {
            RunUpgradeChoice selected = SelectWeighted(random, build, candidates);
            result.Add(selected);
            candidates.Remove(selected);
            RemoveAdditionalSpellCards(result, candidates);
        }

        Shuffle(random, result);
        return result;
    }

    /// <summary>
    /// 一旦本轮已经抽到符卡奥义，就移除其余符卡候选，保证三选一至多占一个奥义位置。
    /// </summary>
    private static void RemoveAdditionalSpellCards(
        IReadOnlyList<RunUpgradeChoice> result,
        List<RunUpgradeChoice> candidates)
    {
        if (!result.Any(choice =>
            choice.Definition.Category == RunUpgradeCategory.SpellCard))
        {
            return;
        }

        candidates.RemoveAll(choice =>
            choice.Definition.Category == RunUpgradeCategory.SpellCard);
    }

    /// <summary>
    /// 为每项普通升级和已解锁特化建立独立候选，普通升重与同定义特化允许同局竞争。
    /// </summary>
    private static List<RunUpgradeChoice> CreateCandidates(
        RunBuildState build,
        ContentPackSelection content,
        int runLevel)
    {
        var candidates = new List<RunUpgradeChoice>();
        foreach (RunUpgradeDefinition definition in RunUpgradeCatalog.All)
        {
            bool enabled = definition.RequiredContentPack is null ||
                content.IsEnabled(definition.RequiredContentPack);
            if (!enabled)
            {
                continue;
            }

            if (build.CanUpgrade(definition))
            {
                candidates.Add(new RunUpgradeChoice(definition));
            }

            candidates.AddRange(definition.Specializations
                .Where(item => build.CanSpecialize(definition, item, runLevel))
                .Select(item => new RunUpgradeChoice(definition, item)));
        }

        return candidates;
    }

    /// <summary>
    /// 判断玩家是否已通过选择形成任意亲和，开局不人为定义所谓低亲和路线。
    /// </summary>
    private static bool HasEstablishedAffinity(RunBuildState build) =>
        Enum.GetValues<RunUpgradeAffinity>().Any(affinity => build.GetAffinity(affinity) > 0);

    /// <summary>
    /// 从与当前最高亲和不重叠且未持有的项目中等概率抽取探索项，避免同路线百分之百垄断。
    /// </summary>
    private static RunUpgradeChoice? SelectExploration(
        RandomNumberGenerator random,
        RunBuildState build,
        List<RunUpgradeChoice> candidates)
    {
        int maximum = Enum.GetValues<RunUpgradeAffinity>()
            .Max(build.GetAffinity);
        var dominant = Enum.GetValues<RunUpgradeAffinity>()
            .Where(affinity => build.GetAffinity(affinity) == maximum)
            .ToHashSet();
        RunUpgradeChoice[] alternatives = candidates.Where(choice =>
            build.GetRank(choice.Definition.Id) == 0 &&
            !choice.Affinities.Any(dominant.Contains)).ToArray();
        if (alternatives.Length > 0)
        {
            return SelectWeighted(random, build, alternatives);
        }

        int minimumAffinity = candidates.Min(choice =>
            choice.Affinities.Sum(build.GetAffinity));
        RunUpgradeChoice[] lowest = candidates.Where(choice =>
            choice.Affinities.Sum(build.GetAffinity) == minimumAffinity).ToArray();
        return SelectWeighted(random, build, lowest);
    }

    /// <summary>
    /// 使用基础权重、已持有升重和标签亲和计算纯构筑权重，来源包身份不参与公式。
    /// </summary>
    private static RunUpgradeChoice SelectWeighted(
        RandomNumberGenerator random,
        RunBuildState build,
        IReadOnlyList<RunUpgradeChoice> candidates)
    {
        double[] weights = RunOfferWeightTable.Create(build, candidates);
        double roll = random.Randf() * weights.Sum();
        for (int index = 0; index < candidates.Count; index++)
        {
            roll -= weights[index];
            if (roll <= 0.0)
            {
                return candidates[index];
            }
        }

        return candidates[^1];
    }

    /// <summary>
    /// 计算单项候选权重；亲和倍率封顶，确保高亲和提升显著但仍保留随机性。
    /// </summary>
    public static double CalculateWeight(RunBuildState build, RunUpgradeChoice choice)
    {
        int affinity = choice.Affinities.Sum(build.GetAffinity);
        double affinityMultiplier = Math.Min(
            MaximumAffinityMultiplier, 1.0 + affinity * AffinityStep);
        double rankMultiplier = 1.0 +
            Math.Min(3, build.GetRank(choice.Definition.Id)) * OwnedRankBonus;
        return choice.Definition.BaseOfferWeight * affinityMultiplier * rankMultiplier;
    }

    /// <summary>
    /// 原地洗牌最终候选，保证探索位没有固定按钮位置并维持固定种子可复现性。
    /// </summary>
    private static void Shuffle(RandomNumberGenerator random, List<RunUpgradeChoice> choices)
    {
        for (int index = choices.Count - 1; index > 0; index--)
        {
            int swapIndex = random.RandiRange(0, index);
            (choices[index], choices[swapIndex]) = (choices[swapIndex], choices[index]);
        }
    }
}
