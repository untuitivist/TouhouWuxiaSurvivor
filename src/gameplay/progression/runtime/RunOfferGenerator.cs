using Godot;
using TouhouWuxiaSurvivor.Content;
using TouhouWuxiaSurvivor.Gameplay.Progression.Definitions;

namespace TouhouWuxiaSurvivor.Gameplay.Progression.Runtime;

/// <summary>
/// 从统一横向池生成“继续精进、形成路线、补足短板”三类候选；亲和只影响概率而不建立锁定。
/// </summary>
public sealed class RunOfferGenerator
{
    private const float AffinityStep = 0.32f;
    private const float OwnedRankBonus = 0.24f;
    private const float MaximumAffinityMultiplier = 3.4f;

    /// <summary>
    /// 先过滤内容、前置和重数，再按精进、成势与补缺职责组装最多三项无放回候选。
    /// </summary>
    public IReadOnlyList<RunUpgradeChoice> CreateOffer(
        RandomNumberGenerator random,
        RunBuildState build,
        ContentPackSelection content,
        int runLevel,
        int choiceCount = 3,
        bool allowRepeatable = false)
    {
        int requested = Math.Max(0, choiceCount);
        var candidates = CreateCandidates(build, content, runLevel, allowRepeatable);
        if (requested == 0 || candidates.Count == 0)
        {
            return [];
        }

        var result = new List<RunUpgradeChoice>(requested);
        if (requested >= 3)
        {
            RunUpgradeChoice? momentum = SelectMomentum(random, build, candidates);
            if (momentum is not null)
            {
                result.Add(momentum.WithRole(RunUpgradeOfferRole.Momentum));
                candidates.Remove(momentum);
                RemoveAdditionalSpellCards(result, candidates);
            }

            RunUpgradeChoice? complement = SelectComplement(random, build, candidates);
            if (complement is not null)
            {
                result.Add(complement.WithRole(RunUpgradeOfferRole.Complement));
                candidates.Remove(complement);
                RemoveAdditionalSpellCards(result, candidates);
            }

            RunUpgradeChoice? supplement = SelectExploration(random, build, candidates);
            if (supplement is not null)
            {
                result.Add(supplement.WithRole(RunUpgradeOfferRole.Exploration));
                candidates.Remove(supplement);
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
        int runLevel,
        bool allowRepeatable)
    {
        var candidates = new List<RunUpgradeChoice>();
        foreach (RunUpgradeDefinition definition in RunUpgradeCatalog.All)
        {
            bool enabled = definition.RequiredContentPack is null ||
                content.IsEnabled(definition.RequiredContentPack);
            if (!enabled || definition.IsRepeatable && !allowRepeatable)
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
    /// 从未持有且与主亲和不重叠的项目中抽取补足项；没有主亲和时优先效用或奥义。
    /// </summary>
    private static RunUpgradeChoice? SelectExploration(
        RandomNumberGenerator random,
        RunBuildState build,
        List<RunUpgradeChoice> candidates)
    {
        if (candidates.Count == 0)
        {
            return null;
        }

        HashSet<RunUpgradeAffinity> dominant = GetDominantAffinities(build);
        RunUpgradeChoice[] alternatives = candidates.Where(choice =>
            build.GetRank(choice.Definition.Id) == 0 &&
            (!choice.Affinities.Any(dominant.Contains) || dominant.Count == 0)).ToArray();
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
    /// 从已经取得且尚未满级的普通升级中抽取精进项，确保 A/B 有机会连续成长而非随机失踪。
    /// </summary>
    private static RunUpgradeChoice? SelectMomentum(
        RandomNumberGenerator random,
        RunBuildState build,
        IReadOnlyList<RunUpgradeChoice> candidates)
    {
        RunUpgradeChoice[] matching = candidates.Where(choice =>
            choice.Specialization is null &&
            build.GetRank(choice.Definition.Id) > 0).ToArray();
        if (matching.Length == 0)
        {
            return null;
        }

        return SelectWeighted(random, build, matching);
    }

    /// <summary>
    /// 选择与当前最高亲和或已练前置相连的新节点，使第二张牌自然形成路线但不强制流派。
    /// </summary>
    private static RunUpgradeChoice? SelectComplement(
        RandomNumberGenerator random,
        RunBuildState build,
        IReadOnlyList<RunUpgradeChoice> candidates)
    {
        HashSet<RunUpgradeAffinity> dominant = GetDominantAffinities(build);
        RunUpgradeChoice[] bridges = candidates.Where(choice =>
            build.GetRank(choice.Definition.Id) == 0 &&
            (choice.Affinities.Any(dominant.Contains) ||
                choice.Definition.Requirements.Any(requirement =>
                    build.GetRank(requirement.RequiredUpgradeId) > 0))).ToArray();
        return bridges.Length == 0 ? null : SelectWeighted(random, build, bridges);
    }

    /// <summary>
    /// 返回本局并列最高的亲和集合；调用方只会在至少一点亲和形成后使用。
    /// </summary>
    private static HashSet<RunUpgradeAffinity> GetDominantAffinities(RunBuildState build)
    {
        int maximum = Enum.GetValues<RunUpgradeAffinity>().Max(build.GetAffinity);
        if (maximum <= 0)
        {
            return [];
        }
        return Enum.GetValues<RunUpgradeAffinity>()
            .Where(affinity => build.GetAffinity(affinity) == maximum)
            .ToHashSet();
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
