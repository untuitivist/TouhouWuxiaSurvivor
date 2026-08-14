namespace TouhouWuxiaSurvivor.Ui.Stats.Build;

/// <summary>
/// 保存构筑图中一条稳定有向关系及其当前满足状态，供连线高亮而不重复判定规则。
/// </summary>
public sealed class CharacterBuildRelationView
{
    public string FromNodeId { get; }
    public string ToNodeId { get; }
    public CharacterBuildRelationKind Kind { get; }
    public int MinimumRank { get; }
    public bool IsSatisfied { get; }
    public string Label { get; }

    /// <summary>
    /// 建立关系边并保留最低重数；非前置关系传入零即可避免伪造等级条件。
    /// </summary>
    public CharacterBuildRelationView(
        string fromNodeId,
        string toNodeId,
        CharacterBuildRelationKind kind,
        int minimumRank,
        bool isSatisfied,
        string label)
    {
        FromNodeId = fromNodeId;
        ToNodeId = toNodeId;
        Kind = kind;
        MinimumRank = Math.Max(0, minimumRank);
        IsSatisfied = isSatisfied;
        Label = label;
    }
}
