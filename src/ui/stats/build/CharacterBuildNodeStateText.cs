namespace TouhouWuxiaSurvivor.Ui.Stats.Build;

/// <summary>
/// 将细分锁因归并为图谱可读的四种视觉状态，同时保留节点模型中的具体阻断原因供详情展示。
/// </summary>
public static class CharacterBuildNodeStateText
{
    /// <summary>返回节点内的短标记：可达、已得、圆满和全部封锁原因分别使用唯一符号。</summary>
    public static string GetMarker(CharacterBuildNodeView node) => node.State switch
    {
        CharacterBuildNodeState.Available => "+",
        CharacterBuildNodeState.Learned => FormatLearnedRank(node),
        CharacterBuildNodeState.Mastered => "圆",
        _ => "锁",
    };

    /// <summary>返回固定图例文字，使颜色、符号和状态含义同时可见而不只依赖色觉。</summary>
    public static string GetLegend() => "+可达 · 重数已得 · 圆满 · 锁封锁";

    /// <summary>为有限、无尽和一次性已得节点生成紧凑重数标记。</summary>
    private static string FormatLearnedRank(CharacterBuildNodeView node) =>
        node.IsRepeatable ? $"{node.CurrentRank}重" : $"{node.CurrentRank}/{node.MaxRank}";
}
