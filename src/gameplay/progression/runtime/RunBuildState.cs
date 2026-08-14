using TouhouWuxiaSurvivor.Gameplay.Progression.Definitions;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;

namespace TouhouWuxiaSurvivor.Gameplay.Progression.Runtime;

/// <summary>
/// 保存本局已选升级重数，是升级目录、倍率计算和结算摘要之间的唯一构筑数据源。
/// </summary>
public sealed class RunBuildState
{
    private readonly Dictionary<string, int> _ranks = new(StringComparer.Ordinal);
    private readonly HashSet<string> _specializations = new(StringComparer.Ordinal);
    public int TotalRanks => _ranks.Values.Sum();
    public int TotalSpecializations => _specializations.Count;

    /// <summary>
    /// 返回指定升级当前重数，尚未获得的项目稳定返回零。
    /// </summary>
    public int GetRank(RunUpgradeKind kind) => GetRank(
        RunUpgradeCatalog.GetRequiredByKind(kind).Id);

    /// <summary>
    /// 按稳定升级 ID 返回当前重数，使任意数量符卡不再占用一项编译期枚举值。
    /// </summary>
    public int GetRank(string upgradeId) => _ranks.GetValueOrDefault(upgradeId);

    /// <summary>
    /// 判断本局是否已选择指定特化稳定 ID，战斗倍率与候选过滤均从这里读取。
    /// </summary>
    public bool HasSpecialization(string specializationId) =>
        _specializations.Contains(specializationId);

    /// <summary>
    /// 汇总本局玩家实际选择产生的亲和值；每次升重与特化均为自身标签各贡献一点。
    /// </summary>
    public int GetAffinity(RunUpgradeAffinity affinity)
    {
        int rankAffinity = RunUpgradeCatalog.All.Sum(definition =>
            definition.Affinities.Contains(affinity) ? GetRank(definition.Id) : 0);
        int specializationAffinity = RunUpgradeCatalog.All
            .SelectMany(definition => definition.Specializations)
            .Count(item => HasSpecialization(item.Id) && item.Affinities.Contains(affinity));
        return rankAffinity + specializationAffinity;
    }

    /// <summary>
    /// 判断定义是否仍低于自身上限且满足前置修炼，供目录过滤可选项目。
    /// </summary>
    public bool CanUpgrade(RunUpgradeDefinition definition)
        => GetUpgradeBlockReason(definition) is null;

    /// <summary>
    /// 返回升级不可选的明确原因；空值表示重数、前置、互斥与奥义槽位均允许本次选择。
    /// </summary>
    public string? GetUpgradeBlockReason(RunUpgradeDefinition definition)
    {
        if (!definition.IsRepeatable && GetRank(definition.Id) >= definition.MaxRank)
        {
            return "已达到最高重数";
        }

        if (definition.Requirements.Any(requirement =>
            GetRank(requirement.RequiredUpgradeId) < requirement.MinimumRank))
        {
            return "尚未满足前置修炼";
        }

        if (definition.ExcludedUpgradeIds.Any(id => GetRank(id) > 0))
        {
            return "与已选修炼互斥";
        }

        SpellCardDefinition? card = SpellCardSlotPolicy.Resolve(definition);
        if (definition.Category == RunUpgradeCategory.SpellCard && card is null)
        {
            return "奥义定义缺失";
        }

        return card is null ? null : SpellCardSlotPolicy.GetBlockReason(this, card);
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

        _ranks[definition.Id] = checked(GetRank(definition.Id) + 1);
        return true;
    }

    /// <summary>
    /// 判断特化是否达到境界、基础重数、前置和互斥要求，且同一分支不可重复取得。
    /// </summary>
    public bool CanSpecialize(
        RunUpgradeDefinition definition,
        RunUpgradeSpecialization specialization,
        int runLevel)
    {
        if (!definition.Specializations.Contains(specialization) ||
            HasSpecialization(specialization.Id) ||
            runLevel < specialization.MinimumRunLevel ||
            GetRank(definition.Id) < specialization.RequiredRank)
        {
            return false;
        }

        return !specialization.ExcludedSpecializationIds.Any(HasSpecialization) &&
            !definition.Specializations.Any(item => HasSpecialization(item.Id));
    }

    /// <summary>
    /// 应用一项已解锁特化；失败不会改变构筑，成功后同组与显式互斥分支均被阻断。
    /// </summary>
    public bool ApplySpecialization(
        RunUpgradeDefinition definition,
        RunUpgradeSpecialization specialization,
        int runLevel)
    {
        if (!CanSpecialize(definition, specialization, runLevel))
        {
            return false;
        }

        return _specializations.Add(specialization.Id);
    }

    /// <summary>
    /// 应用普通升重或特化候选，使协调器无需分辨候选的具体数据形态。
    /// </summary>
    public bool Apply(RunUpgradeChoice choice, int runLevel) =>
        choice.Specialization is null
            ? Apply(choice.Definition)
            : ApplySpecialization(choice.Definition, choice.Specialization, runLevel);

    /// <summary>
    /// 按目录顺序生成紧凑中文构筑摘要，未取得升级时明确显示尚未修习。
    /// </summary>
    public string Describe()
    {
        var entries = RunUpgradeCatalog.All
            .Where(definition => GetRank(definition.Id) > 0)
            .Select(definition => definition.Category == RunUpgradeCategory.SpellCard
                ? definition.DisplayName
                : $"{definition.DisplayName} {GetRank(definition.Id)}重")
            .Concat(RunUpgradeCatalog.All.SelectMany(definition => definition.Specializations)
                .Where(item => HasSpecialization(item.Id))
                .Select(item => $"{item.DisplayName}·特化"))
            .ToArray();
        return entries.Length == 0 ? "尚未修习" : string.Join("、", entries);
    }
}
