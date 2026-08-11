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
    public string? RequiredContentPack { get; }
    public string? SpellCardId { get; }
    public bool IsRepeatable { get; }

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
        bool isRepeatable = false)
    {
        Id = id;
        DisplayName = displayName;
        Kind = kind;
        Category = category;
        MaxRank = Math.Max(1, maxRank);
        EffectText = effectText;
        Requirement = requirement;
        RequiredContentPack = requiredContentPack;
        SpellCardId = spellCardId;
        IsRepeatable = isRepeatable;
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
            ? "悟得"
            : IsRepeatable ? $"第 {currentRank + 1} 重" : $"{currentRank + 1}/{MaxRank}";
        return $"{GetCategoryName()} · {DisplayName}    {rankText}\n{EffectText}";
    }
}
