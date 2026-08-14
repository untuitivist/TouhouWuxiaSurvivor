namespace TouhouWuxiaSurvivor.Ui.Stats.Build;

using TouhouWuxiaSurvivor.Content.Characters;
using TouhouWuxiaSurvivor.Gameplay.Progression.Definitions;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;
using TouhouWuxiaSurvivor.Ui;

/// <summary>
/// 汇总 E 键构筑页一次打开所需的亲和、节点和关系快照，并提供常用筛选结果。
/// </summary>
public sealed class CharacterBuildViewModel
{
    public int RunLevel { get; }
    public int TotalRanks { get; }
    public CharacterCombatRole CombatRole { get; }
    public string CombatRoleName => CharacterCombatRoleText.GetName(CombatRole);
    public int OffensiveSpellCount { get; }
    public int SupportSpellCount { get; }
    public int OffensiveSpellCapacity => SpellCardSlotPolicy.MaximumOffensiveSlots;
    public int SupportSpellCapacity => SpellCardSlotPolicy.MaximumSupportSlots;
    public IReadOnlyList<CharacterBuildAffinityView> Affinities { get; }
    public IReadOnlyList<CharacterBuildNodeView> Nodes { get; }
    public IReadOnlyList<CharacterBuildRelationView> Relations { get; }
    public IReadOnlyList<CharacterBuildNodeView> LearnedNodes { get; }
    public IReadOnlyList<CharacterBuildNodeView> AvailableNodes { get; }

    /// <summary>
    /// 冻结完整投影并派生已习得与当前可选集合，避免多个控件使用不同筛选语义。
    /// </summary>
    public CharacterBuildViewModel(
        int runLevel,
        int totalRanks,
        CharacterCombatRole combatRole,
        IEnumerable<CharacterBuildAffinityView> affinities,
        IEnumerable<CharacterBuildNodeView> nodes,
        IEnumerable<CharacterBuildRelationView> relations)
    {
        RunLevel = Math.Max(1, runLevel);
        TotalRanks = Math.Max(0, totalRanks);
        CombatRole = combatRole;
        Affinities = affinities.OrderBy(item => item.SortOrder).ToArray();
        Nodes = nodes.OrderBy(item => item.SortGroup)
            .ThenBy(item => item.SortOrder).ThenBy(item => item.Id, StringComparer.Ordinal)
            .ToArray();
        Relations = relations.OrderBy(item => item.Kind)
            .ThenBy(item => item.FromNodeId, StringComparer.Ordinal)
            .ThenBy(item => item.ToNodeId, StringComparer.Ordinal).ToArray();
        LearnedNodes = Nodes.Where(item => item.IsLearned).ToArray();
        AvailableNodes = Nodes.Where(item => item.IsAvailable).ToArray();
        OffensiveSpellCount = CountLearnedSpells(SpellCardSlotKind.Offensive);
        SupportSpellCount = CountLearnedSpells(SpellCardSlotKind.Support);
    }

    /// <summary>按节点已悟状态统计共享奥义槽占用，不读取可变构筑或场景节点。</summary>
    private int CountLearnedSpells(SpellCardSlotKind kind) => SpellCardCatalog.All.Count(card =>
        SpellCardSlotPolicy.Classify(card) == kind && Nodes.Any(node =>
            node.Id == card.UnlockUpgradeId && node.IsLearned &&
            node.Category == RunUpgradeCategory.SpellCard));
}
