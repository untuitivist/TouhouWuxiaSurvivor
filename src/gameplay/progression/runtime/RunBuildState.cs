using TouhouWuxiaSurvivor.Gameplay.Progression.Definitions;

namespace TouhouWuxiaSurvivor.Gameplay.Progression.Runtime;

/// <summary>
/// 保存本局已选升级重数，是升级目录、倍率计算和结算摘要之间的唯一构筑数据源。
/// </summary>
public sealed class RunBuildState
{
    private readonly Dictionary<RunUpgradeKind, int> _ranks = [];
    public int TotalRanks => _ranks.Values.Sum();

    /// <summary>
    /// 返回指定升级当前重数，尚未获得的项目稳定返回零。
    /// </summary>
    public int GetRank(RunUpgradeKind kind) => _ranks.GetValueOrDefault(kind);

    /// <summary>
    /// 判断定义是否仍低于自身上限且满足前置修炼，供目录过滤可选项目。
    /// </summary>
    public bool CanUpgrade(RunUpgradeDefinition definition)
    {
        if (GetRank(definition.Kind) >= definition.MaxRank)
        {
            return false;
        }

        RunUpgradeRequirement? requirement = definition.Requirement;
        return requirement is null ||
            GetRank(requirement.RequiredKind) >= requirement.MinimumRank;
    }

    /// <summary>
    /// 将定义提高一重并返回是否成功，已满重项目不会改变构筑。
    /// </summary>
    public bool Apply(RunUpgradeDefinition definition)
    {
        if (!CanUpgrade(definition))
        {
            return false;
        }

        _ranks[definition.Kind] = GetRank(definition.Kind) + 1;
        return true;
    }

    /// <summary>
    /// 按目录顺序生成紧凑中文构筑摘要，未取得升级时明确显示尚未修习。
    /// </summary>
    public string Describe()
    {
        string[] entries = RunUpgradeCatalog.All
            .Where(definition => GetRank(definition.Kind) > 0)
            .Select(definition => definition.Category == RunUpgradeCategory.SpellCard
                ? definition.DisplayName
                : $"{definition.DisplayName} {GetRank(definition.Kind)}重")
            .ToArray();
        return entries.Length == 0 ? "尚未修习" : string.Join("、", entries);
    }
}
