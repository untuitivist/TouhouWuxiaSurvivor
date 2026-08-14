using TouhouWuxiaSurvivor.Gameplay.Progression.Definitions;

namespace TouhouWuxiaSurvivor.Ui.Stats.Build;

/// <summary>
/// 保存构筑图中一个可聚焦节点的完整展示状态，不向界面泄漏运行时可变对象。
/// </summary>
public sealed class CharacterBuildNodeView
{
    public string Id { get; }
    public string? ParentId { get; }
    public CharacterBuildNodeKind Kind { get; }
    public CharacterBuildNodeState State { get; }
    public string DisplayName { get; }
    public RunUpgradeCategory Category { get; }
    public string CategoryName { get; }
    public string EffectText { get; }
    public string CurrentEffectText { get; }
    public string NextEffectText { get; }
    public string TriggerText { get; }
    public string LockReason { get; }
    public int CurrentRank { get; }
    public int MaxRank { get; }
    public bool IsRepeatable { get; }
    public bool CanAdvance { get; }
    public string SourcePackId { get; }
    public IReadOnlyList<RunUpgradeAffinity> Affinities { get; }
    public string SearchText { get; }
    public int SortGroup { get; }
    public int SortOrder { get; }
    public bool IsLearned => State is CharacterBuildNodeState.Learned or
        CharacterBuildNodeState.Mastered;
    public bool IsAvailable => CanAdvance;

    /// <summary>
    /// 建立已完成判定的不可变节点；稳定分组与次序让列表和关系图采用完全一致的排序。
    /// </summary>
    public CharacterBuildNodeView(
        string id,
        string? parentId,
        CharacterBuildNodeKind kind,
        CharacterBuildNodeState state,
        string displayName,
        RunUpgradeCategory category,
        string categoryName,
        string effectText,
        string currentEffectText,
        string nextEffectText,
        string triggerText,
        string lockReason,
        int currentRank,
        int maxRank,
        bool isRepeatable,
        bool canAdvance,
        string sourcePackId,
        IReadOnlyList<RunUpgradeAffinity> affinities,
        int sortGroup,
        int sortOrder)
    {
        Id = id;
        ParentId = parentId;
        Kind = kind;
        State = state;
        DisplayName = displayName;
        Category = category;
        CategoryName = categoryName;
        EffectText = effectText;
        CurrentEffectText = currentEffectText;
        NextEffectText = nextEffectText;
        TriggerText = triggerText;
        LockReason = lockReason;
        CurrentRank = Math.Max(0, currentRank);
        MaxRank = Math.Max(1, maxRank);
        IsRepeatable = isRepeatable;
        CanAdvance = canAdvance;
        SourcePackId = sourcePackId;
        Affinities = affinities.ToArray();
        SortGroup = sortGroup;
        SortOrder = sortOrder;
        SearchText = string.Join(' ', new[]
        {
            displayName, categoryName, effectText, currentEffectText, nextEffectText,
            triggerText, lockReason,
            RunUpgradeAffinityFormatter.FormatMany(affinities), sourcePackId,
        }.Where(text => !string.IsNullOrWhiteSpace(text)));
    }
}
