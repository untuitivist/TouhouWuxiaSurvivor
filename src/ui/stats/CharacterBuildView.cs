using Godot;
using TouhouWuxiaSurvivor.Content;
using TouhouWuxiaSurvivor.Gameplay.Progression.Definitions;
using TouhouWuxiaSurvivor.Ui.Stats.Build;

namespace TouhouWuxiaSurvivor.Ui.Stats;

/// <summary>
/// 协调构筑页的亲和总览、分类筛选、关系图和固定详情栏，不持有或修改运行时构筑。
/// </summary>
public partial class CharacterBuildView : Control
{
    private readonly Dictionary<Button, CharacterBuildFilter> _filters = [];
    private CharacterBuildViewModel? _model;
    private CharacterBuildFilter _filter = CharacterBuildFilter.Learned;
    private CharacterBuildGraph? _graph;
    private Label? _detailName;
    private Label? _detailMeta;
    private Label? _detailAffinity;
    private Label? _detailEffect;
    private Label? _detailTrigger;
    private Label? _detailLock;

    public CharacterBuildFilter CurrentFilter => _filter;
    public string? SelectedNodeId => _graph?.SelectedNodeId;
    public CharacterBuildGraph Graph => _graph!;

    /// <summary>
    /// 缓存图谱与详情标签，连接固定筛选按钮，并阻止构筑页的空白鼠标输入穿透。
    /// </summary>
    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Stop;
        _graph = GetNode<CharacterBuildGraph>("Body/GraphFrame/Graph");
        _graph.SelectionChanged += ShowDetails;
        _detailName = GetNode<Label>("Body/DetailsFrame/Details/Name");
        _detailMeta = GetNode<Label>("Body/DetailsFrame/Details/Meta");
        _detailAffinity = GetNode<Label>("Body/DetailsFrame/Details/Affinity");
        _detailEffect = GetNode<Label>("Body/DetailsFrame/Details/Effect");
        _detailTrigger = GetNode<Label>("Body/DetailsFrame/Details/Trigger");
        _detailLock = GetNode<Label>("Body/DetailsFrame/Details/Lock");
        GetNode<Label>("Filters/Legend").Text = CharacterBuildNodeStateText.GetLegend();
        RegisterFilter("Filters/All", CharacterBuildFilter.All);
        RegisterFilter("Filters/Learned", CharacterBuildFilter.Learned);
        RegisterFilter("Filters/Available", CharacterBuildFilter.Available);
        RegisterFilter("Filters/Martial", CharacterBuildFilter.MartialArt);
        RegisterFilter("Filters/Inner", CharacterBuildFilter.InnerArt);
        RegisterFilter("Filters/Spell", CharacterBuildFilter.SpellCard);
        RegisterFilter("Filters/Special", CharacterBuildFilter.Specialization);
    }

    /// <summary>
    /// 写入一次打开时冻结的构筑模型，刷新五亲和、筛选结果与默认详情。
    /// </summary>
    public void SetModel(CharacterBuildViewModel model)
    {
        _model = model;
        _filter = model.LearnedNodes.Count > 0
            ? CharacterBuildFilter.Learned
            : CharacterBuildFilter.Available;
        GetNode<Label>("Summary/Realm").Text = $"境界 {model.RunLevel}";
        GetNode<Label>("Summary/Ranks").Text = $"总重 {model.TotalRanks}";
        GetNode<Label>("Summary/Role").Text = $"定位 {model.CombatRoleName}";
        GetNode<Label>("Summary/Spells").Text =
            $"奥义 攻{model.OffensiveSpellCount}/{model.OffensiveSpellCapacity} " +
            $"护{model.SupportSpellCount}/{model.SupportSpellCapacity}";
        for (int index = 0; index < model.Affinities.Count; index++)
        {
            CharacterBuildAffinityView affinity = model.Affinities[index];
            GetNode<Label>($"Summary/Affinity{index}").Text =
                $"{affinity.DisplayName} {affinity.Value}";
            GetNode<ProgressBar>($"Summary/AffinityBar{index}").Value = affinity.Share * 100.0f;
        }

        ApplyFilter(_filter);
    }

    /// <summary>
    /// 对外提供稳定筛选入口，测试与未来手柄导航无需模拟按钮文本。
    /// </summary>
    public void SelectFilter(CharacterBuildFilter filter) => ApplyFilter(filter);

    /// <summary>
    /// 注册筛选按钮的稳定枚举并连接点击回调；按钮只改变可视集合，不修改抽取权重。
    /// </summary>
    private void RegisterFilter(string path, CharacterBuildFilter filter)
    {
        Button button = GetNode<Button>(path);
        _filters[button] = filter;
        button.Pressed += () => ApplyFilter(filter);
    }

    /// <summary>
    /// 查询当前筛选节点、更新按钮状态，并尽量保留先前选中节点。
    /// </summary>
    private void ApplyFilter(CharacterBuildFilter filter)
    {
        _filter = filter;
        foreach ((Button button, CharacterBuildFilter value) in _filters)
        {
            button.ButtonPressed = value == filter;
        }

        if (_model is null)
        {
            return;
        }

        IReadOnlyList<CharacterBuildNodeView> nodes = CharacterBuildQuery.Apply(
            _model.Nodes, filter, CharacterBuildSort.Catalog)
            .Where(node => node.State != CharacterBuildNodeState.LockedContent)
            .ToArray();
        string? previous = _graph!.SelectedNodeId;
        _graph.SetModel(_model, nodes, previous);
        GetNode<Label>("Filters/Count").Text = $"{nodes.Count}项";
        if (nodes.Count == 0)
        {
            ShowEmptyDetails();
        }
    }

    /// <summary>
    /// 把节点名称、等级、亲和、效果、定时规则和锁因填入固定高度详情栏。
    /// </summary>
    private void ShowDetails(CharacterBuildNodeView node)
    {
        _detailName!.Text = node.DisplayName;
        _detailMeta!.Text = $"{node.CategoryName} · {FormatRank(node)} · " +
            ResolveSourceName(node.SourcePackId);
        _detailAffinity!.Text = "亲和  " +
            RunUpgradeAffinityFormatter.FormatMany(node.Affinities);
        _detailEffect!.Text = $"{node.CurrentEffectText}\n{node.NextEffectText}";
        _detailTrigger!.Text = string.IsNullOrWhiteSpace(node.TriggerText)
            ? "被动常驻 · 无需施放"
            : node.TriggerText;
        _detailLock!.Text = node.IsLearned
            ? node.CanAdvance ? "已习得 · 后续仍可升重" : "已完成本路线当前上限"
            : node.IsAvailable ? "可在升级三选一中取得" : node.LockReason;
    }

    /// <summary>
    /// 当筛选结果为空时写入完整说明，避免详情栏保留上一筛选的幽灵节点。
    /// </summary>
    private void ShowEmptyDetails()
    {
        _detailName!.Text = "此栏暂无节点";
        _detailMeta!.Text = "切换上方分类继续查看";
        _detailAffinity!.Text = string.Empty;
        _detailEffect!.Text = "构筑页只负责查看与理解路线。";
        _detailTrigger!.Text = "升重与特化仍在境界突破时选择。";
        _detailLock!.Text = string.Empty;
    }

    /// <summary>
    /// 将有限、无尽与一次性节点重数转换为紧凑详情文字。
    /// </summary>
    private static string FormatRank(CharacterBuildNodeView node)
    {
        if (node.Kind == CharacterBuildNodeKind.Specialization)
        {
            return node.IsLearned ? "已定型" : "未定型";
        }

        return node.IsRepeatable
            ? $"{node.CurrentRank}重 · 无尽"
            : $"{node.CurrentRank}/{node.MaxRank}重";
    }

    /// <summary>
    /// 把内容包稳定 ID 转为显示名；本体及损坏条目都有明确回退。
    /// </summary>
    private static string ResolveSourceName(string sourcePackId)
    {
        if (sourcePackId == "base")
        {
            return ContentPackCatalog.Base.DisplayName;
        }

        return ContentPackCatalog.All.FirstOrDefault(pack => pack.Id == sourcePackId)
            ?.DisplayName ?? sourcePackId;
    }
}
