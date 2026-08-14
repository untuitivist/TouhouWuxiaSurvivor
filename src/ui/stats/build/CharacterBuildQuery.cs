using TouhouWuxiaSurvivor.Gameplay.Progression.Definitions;

namespace TouhouWuxiaSurvivor.Ui.Stats.Build;

/// <summary>
/// 对不可变构筑节点执行统一筛选、搜索与排序，保证图形页和紧凑列表返回相同结果。
/// </summary>
public static class CharacterBuildQuery
{
    /// <summary>
    /// 按类型和可见状态过滤，再以中文搜索索引匹配，最后应用稳定的次级目录顺序。
    /// </summary>
    public static IReadOnlyList<CharacterBuildNodeView> Apply(
        IEnumerable<CharacterBuildNodeView> nodes,
        CharacterBuildFilter filter,
        CharacterBuildSort sort,
        string? searchText = null)
    {
        IEnumerable<CharacterBuildNodeView> result = nodes.Where(item => Matches(item, filter));
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            result = result.Where(item => item.SearchText.Contains(
                searchText.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        return Sort(result, sort).ThenBy(item => item.SortGroup)
            .ThenBy(item => item.SortOrder).ThenBy(item => item.Id, StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>
    /// 判断单个节点是否属于指定筛选集合，锁定集合明确排除当前可选与已习得节点。
    /// </summary>
    private static bool Matches(CharacterBuildNodeView item, CharacterBuildFilter filter) =>
        filter switch
        {
            CharacterBuildFilter.Learned => item.IsLearned,
            CharacterBuildFilter.Available => item.IsAvailable,
            CharacterBuildFilter.Locked => !item.IsLearned && !item.IsAvailable,
            CharacterBuildFilter.MartialArt =>
                item.Kind == CharacterBuildNodeKind.Upgrade &&
                item.Category == RunUpgradeCategory.MartialArt,
            CharacterBuildFilter.InnerArt =>
                item.Kind == CharacterBuildNodeKind.Upgrade &&
                item.Category == RunUpgradeCategory.InnerArt,
            CharacterBuildFilter.SpellCard =>
                item.Kind == CharacterBuildNodeKind.Upgrade &&
                item.Category == RunUpgradeCategory.SpellCard,
            CharacterBuildFilter.Specialization =>
                item.Kind == CharacterBuildNodeKind.Specialization,
            _ => true,
        };

    /// <summary>
    /// 返回带稳定主键的一级排序；调用方随后追加目录次序以消除相同值的抖动。
    /// </summary>
    private static IOrderedEnumerable<CharacterBuildNodeView> Sort(
        IEnumerable<CharacterBuildNodeView> nodes,
        CharacterBuildSort sort) => sort switch
        {
            CharacterBuildSort.Name => nodes.OrderBy(item => item.DisplayName,
                StringComparer.Ordinal),
            CharacterBuildSort.Rank => nodes.OrderByDescending(item => item.CurrentRank),
            CharacterBuildSort.State => nodes.OrderBy(item => item.State),
            _ => nodes.OrderBy(item => item.SortGroup).ThenBy(item => item.SortOrder),
        };
}
