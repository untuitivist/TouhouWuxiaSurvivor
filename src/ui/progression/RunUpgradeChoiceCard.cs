using Godot;
using TouhouWuxiaSurvivor.Gameplay.Progression.Definitions;
using TouhouWuxiaSurvivor.Gameplay.Progression.Runtime;

namespace TouhouWuxiaSurvivor.Ui.Progression;

/// <summary>
/// 显示一项可点击升级卡片，并用职责色条和重数段表达本轮选择的构筑意义。
/// </summary>
public partial class RunUpgradeChoiceCard : Button
{
    private Label? _role;
    private Label? _category;
    private Label? _title;
    private Label? _effect;
    private Label? _affinity;
    private Label? _rank;
    private ColorRect? _accent;
    private HBoxContainer? _track;

    public RunUpgradeOfferRole OfferRole { get; private set; }
    public string DisplayTitle => _title?.Text ?? string.Empty;

    /// <summary>
    /// 缓存固定子控件并清空 Button 自带文字，所有信息由可独立测量的标签呈现。
    /// </summary>
    public override void _Ready()
    {
        _role = GetNode<Label>("Content/Row/Role/RoleName");
        _category = GetNode<Label>("Content/Row/Role/Category");
        _title = GetNode<Label>("Content/Row/Details/Title");
        _effect = GetNode<Label>("Content/Row/Details/Effect");
        _affinity = GetNode<Label>("Content/Row/Meta/Affinity");
        _rank = GetNode<Label>("Content/Row/Meta/Rank");
        _accent = GetNode<ColorRect>("Content/Row/Accent");
        _track = GetNode<HBoxContainer>("Content/Row/Meta/Track");
        Text = string.Empty;
    }

    /// <summary>
    /// 刷新一项候选的职责、说明和下一重预览；重复调用会完整重建短轨迹。
    /// </summary>
    public void Present(RunUpgradeChoice choice, RunBuildState build)
    {
        RunUpgradeChoicePresentation presentation =
            RunUpgradeChoicePresentationFactory.Create(choice, build);
        OfferRole = choice.OfferRole;
        _role!.Text = presentation.RoleText;
        _category!.Text = presentation.CategoryText;
        _title!.Text = presentation.TitleText;
        _effect!.Text = presentation.EffectText;
        _affinity!.Text = presentation.AffinityText;
        _rank!.Text = presentation.RankText;
        _accent!.Color = GetRoleColor(choice.OfferRole);
        TooltipText = presentation.TooltipText;
        RebuildTrack(presentation);
    }

    /// <summary>
    /// 返回卡片上所有可见文字标签，供 640×360 验收逐项检查而不依赖按钮兼容文本。
    /// </summary>
    public IReadOnlyList<Label> GetVisibleTextLabels() =>
        [_role!, _category!, _title!, _effect!, _affinity!, _rank!];

    /// <summary>
    /// 为有限重数绘制当前、即将获得和未获得三种状态；无尽修行仅保留文字重数。
    /// </summary>
    private void RebuildTrack(RunUpgradeChoicePresentation presentation)
    {
        foreach (Node child in _track!.GetChildren())
        {
            child.Free();
        }

        _track.Visible = presentation.TrackLength > 0;
        for (int index = 0; index < presentation.TrackLength; index++)
        {
            var segment = new ColorRect
            {
                CustomMinimumSize = new Vector2(10.0f, 3.0f),
                MouseFilter = MouseFilterEnum.Ignore,
                Color = GetTrackColor(index, presentation),
            };
            _track.AddChild(segment);
        }
    }

    /// <summary>
    /// 当前重使用沉金，即将取得的一重使用职责色，其余保持低亮墨色。
    /// </summary>
    private Color GetTrackColor(int index, RunUpgradeChoicePresentation presentation)
    {
        if (index < presentation.CurrentRank)
        {
            return new Color(0.64f, 0.54f, 0.31f, 1.0f);
        }

        return index < presentation.PreviewRank
            ? GetRoleColor(OfferRole)
            : new Color(0.16f, 0.20f, 0.17f, 1.0f);
    }

    /// <summary>
    /// 职责色只帮助扫描卡片，不映射品质和强度。
    /// </summary>
    private static Color GetRoleColor(RunUpgradeOfferRole role) => role switch
    {
        RunUpgradeOfferRole.Momentum => new Color(0.78f, 0.22f, 0.17f, 1.0f),
        RunUpgradeOfferRole.Complement => new Color(0.53f, 0.64f, 0.35f, 1.0f),
        RunUpgradeOfferRole.Exploration => new Color(0.38f, 0.59f, 0.64f, 1.0f),
        _ => new Color(0.67f, 0.56f, 0.32f, 1.0f),
    };
}
