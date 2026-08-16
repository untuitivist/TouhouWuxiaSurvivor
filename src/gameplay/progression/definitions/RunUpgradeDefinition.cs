namespace TouhouWuxiaSurvivor.Gameplay.Progression.Definitions;

/// <summary>
/// 保存一种局内升级的稳定 ID、显示信息、分类和重数上限。
/// </summary>
public sealed class RunUpgradeDefinition
{
    public string Id { get; }
    public string DisplayName { get; }
    public RunUpgradeKind Kind { get; }
    public RunUpgradeCategory Category { get; }
    public int MaxRank { get; }
    public string EffectText { get; }
    public RunUpgradeRequirement? Requirement { get; }
    public IReadOnlyList<RunUpgradeRequirement> Requirements { get; }
    public string? RequiredContentPack { get; }
    public string? SpellCardId { get; }
    public bool IsRepeatable { get; }
    public float BaseOfferWeight { get; }
    public IReadOnlyList<RunUpgradeAffinity> Affinities { get; }
    public IReadOnlySet<string> ExcludedUpgradeIds { get; }
    public IReadOnlyList<RunUpgradeSpecialization> Specializations { get; }

    /// <summary>
    /// 构造不可变升级定义，并限制至少一重，避免目录产生永远不可选择的项目。
    /// </summary>
    public RunUpgradeDefinition(
        string id,
        string displayName,
        RunUpgradeKind kind,
        RunUpgradeCategory category,
        int maxRank,
        string effectText,
        RunUpgradeRequirement? requirement = null,
        string? requiredContentPack = null,
        string? spellCardId = null,
        bool isRepeatable = false,
        float baseOfferWeight = 1.0f,
        IEnumerable<RunUpgradeAffinity>? affinities = null,
        IEnumerable<RunUpgradeRequirement>? requirements = null,
        IEnumerable<string>? excludedUpgradeIds = null,
        IEnumerable<RunUpgradeSpecialization>? specializations = null)
    {
        Id = id;
        DisplayName = displayName;
        Kind = kind;
        Category = category;
        MaxRank = Math.Max(1, maxRank);
        EffectText = effectText;
        Requirement = requirement;
        Requirements = MergeRequirements(requirement, requirements);
        RequiredContentPack = requiredContentPack;
        SpellCardId = spellCardId;
        IsRepeatable = isRepeatable;
        BaseOfferWeight = Math.Max(0.01f, baseOfferWeight);
        Affinities = (affinities ?? []).Distinct().ToArray();
        ExcludedUpgradeIds = new HashSet<string>(
            excludedUpgradeIds ?? [], StringComparer.Ordinal);
        Specializations = (specializations ?? []).ToArray();
    }

    /// <summary>
    /// 返回武学或心法的中文分类名，供升级界面保持统一措辞。
    /// </summary>
    public string GetCategoryName() => Category switch
    {
        RunUpgradeCategory.MartialArt => "武学",
        RunUpgradeCategory.InnerArt => "心法",
        RunUpgradeCategory.SpellCard => "符卡奥义",
        _ => "修行",
    };

    /// <summary>
    /// 按当前重数生成下一重选择文本，升级界面无需理解具体倍率公式。
    /// </summary>
    public string FormatChoice(int currentRank)
    {
        string rankText = Category == RunUpgradeCategory.SpellCard
            ? currentRank <= 0 ? $"1/{MaxRank} · 悟得" : $"{currentRank + 1}/{MaxRank} · 化境"
            : IsRepeatable ? $"第 {currentRank + 1} 重" : $"{currentRank + 1}/{MaxRank}";
        return $"{GetCategoryName()} · {DisplayName}    {rankText}\n{EffectText}";
    }

    /// <summary>
    /// 合并兼容单前置与新多前置集合，并按升级 ID 去重保留最高重数要求。
    /// </summary>
    private static IReadOnlyList<RunUpgradeRequirement> MergeRequirements(
        RunUpgradeRequirement? requirement,
        IEnumerable<RunUpgradeRequirement>? requirements)
    {
        IEnumerable<RunUpgradeRequirement> merged = requirement is null
            ? requirements ?? []
            : (requirements ?? []).Prepend(requirement);
        return merged.GroupBy(item => item.RequiredUpgradeId, StringComparer.Ordinal)
            .Select(group => new RunUpgradeRequirement(
                group.Key, group.Max(item => item.MinimumRank)))
            .ToArray();
    }
}
