using TouhouWuxiaSurvivor.Gameplay.Progression.Definitions;

namespace TouhouWuxiaSurvivor.Gameplay.Progression.Runtime;

/// <summary>
/// 把候选的亲和权重转换为最终抽取权重，并固定全部奥义共同占用的类别预算。
/// </summary>
public static class RunOfferWeightTable
{
    private const double SpellPoolWeight = 2.0;

    /// <summary>
    /// 普通修行各自保留原始权重；全部可用奥义先归一化后共享固定总权重，使内容包数量只改变身份。
    /// </summary>
    public static double[] Create(
        RunBuildState build,
        IReadOnlyList<RunUpgradeChoice> candidates)
    {
        double[] raw = candidates.Select(choice =>
            RunOfferGenerator.CalculateWeight(build, choice)).ToArray();
        double spellTotal = candidates.Select((choice, index) => new { choice, index })
            .Where(item => item.choice.Definition.Category == RunUpgradeCategory.SpellCard)
            .Sum(item => raw[item.index]);
        if (spellTotal <= 0.0)
        {
            return raw;
        }

        for (int index = 0; index < candidates.Count; index++)
        {
            if (candidates[index].Definition.Category == RunUpgradeCategory.SpellCard)
            {
                raw[index] = raw[index] / spellTotal * SpellPoolWeight;
            }
        }

        return raw;
    }

    /// <summary>
    /// 返回当前候选中奥义类别的最终总权重，供平行内容契约直接验证而不依赖随机抽样误差。
    /// </summary>
    public static double GetSpellPoolWeight(
        RunBuildState build,
        IReadOnlyList<RunUpgradeChoice> candidates)
    {
        double[] weights = Create(build, candidates);
        return candidates.Select((choice, index) => new { choice, index })
            .Where(item => item.choice.Definition.Category == RunUpgradeCategory.SpellCard)
            .Sum(item => weights[item.index]);
    }
}
