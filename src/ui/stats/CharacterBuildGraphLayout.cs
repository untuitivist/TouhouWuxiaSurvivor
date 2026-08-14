using Godot;
using TouhouWuxiaSurvivor.Gameplay.Progression.Definitions;
using TouhouWuxiaSurvivor.Ui.Stats.Build;

namespace TouhouWuxiaSurvivor.Ui.Stats;

/// <summary>
/// 将筛选后的构筑节点排入武学、心法、符卡与特化四条稳定泳道。
/// </summary>
public static class CharacterBuildGraphLayout
{
    private const float LaneHeight = 52.0f;
    private const float NodeWidth = 84.0f;
    private const float NodeHeight = 34.0f;
    private const float ColumnGap = 12.0f;
    private const float LeftInset = 44.0f;
    private const float TopInset = 8.0f;

    /// <summary>
    /// 按目录次序为节点分配横向槽位；特化优先放在父节点后一列以显露真实分支关系。
    /// </summary>
    public static IReadOnlyList<CharacterBuildGraphItem> Create(
        IReadOnlyList<CharacterBuildNodeView> nodes)
    {
        var laneColumns = new int[4];
        var positions = new Dictionary<string, CharacterBuildGraphItem>(StringComparer.Ordinal);
        foreach (CharacterBuildNodeView node in nodes)
        {
            int lane = ResolveLane(node);
            int column = laneColumns[lane];
            if (node.ParentId is not null && positions.TryGetValue(node.ParentId, out var parent))
            {
                column = Math.Max(column, (int)MathF.Round(
                    (parent.Rect.Position.X - LeftInset) / (NodeWidth + ColumnGap)) + 1);
            }

            laneColumns[lane] = column + 1;
            var position = new Vector2(
                LeftInset + column * (NodeWidth + ColumnGap),
                TopInset + lane * LaneHeight);
            positions[node.Id] = new CharacterBuildGraphItem(
                node, new Rect2(position, new Vector2(NodeWidth, NodeHeight)), lane);
        }

        return nodes.Select(node => positions[node.Id]).ToArray();
    }

    /// <summary>
    /// 将节点分类映射到固定泳道；特化单独成行，避免和基础修行重叠。
    /// </summary>
    private static int ResolveLane(CharacterBuildNodeView node)
    {
        if (node.Kind == CharacterBuildNodeKind.Specialization)
        {
            return 3;
        }

        return node.Category switch
        {
            RunUpgradeCategory.MartialArt => 0,
            RunUpgradeCategory.InnerArt => 1,
            RunUpgradeCategory.SpellCard => 2,
            _ => 1,
        };
    }
}
