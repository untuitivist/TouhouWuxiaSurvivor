namespace TouhouWuxiaSurvivor.Ui.Progression;

/// <summary>
/// 保存一张升级卡片的纯展示数据，使控件不直接理解符卡、重数与亲和目录。
/// </summary>
public sealed class RunUpgradeChoicePresentation
{
    public string RoleText { get; }
    public string CategoryText { get; }
    public string TitleText { get; }
    public string EffectText { get; }
    public string AffinityText { get; }
    public string RankText { get; }
    public string TooltipText { get; }
    public int CurrentRank { get; }
    public int PreviewRank { get; }
    public int TrackLength { get; }

    /// <summary>
    /// 建立固定字段的不可变卡片投影；重数轨迹允许用零长度表示无尽修行。
    /// </summary>
    public RunUpgradeChoicePresentation(
        string roleText,
        string categoryText,
        string titleText,
        string effectText,
        string affinityText,
        string rankText,
        string tooltipText,
        int currentRank,
        int previewRank,
        int trackLength)
    {
        RoleText = roleText;
        CategoryText = categoryText;
        TitleText = titleText;
        EffectText = effectText;
        AffinityText = affinityText;
        RankText = rankText;
        TooltipText = tooltipText;
        CurrentRank = Math.Max(0, currentRank);
        PreviewRank = Math.Max(CurrentRank, previewRank);
        TrackLength = Math.Max(0, trackLength);
    }
}
