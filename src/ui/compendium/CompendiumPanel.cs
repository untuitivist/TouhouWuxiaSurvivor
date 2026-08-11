using Godot;
using TouhouWuxiaSurvivor.Content;

namespace TouhouWuxiaSurvivor.Ui.Compendium;

/// <summary>
/// 提供按作品筛选和按内容分类浏览的主菜单图鉴界面。
/// </summary>
public partial class CompendiumPanel : Control
{
    private readonly List<string> _sourceIds = [];
    private readonly InternalPreviewCatalog _internalPreviews = new();
    private CompendiumEntry[] _visibleEntries = [];
    private OptionButton? _sourceFilter;
    private TabBar? _tabs;
    private ItemList? _entryList;
    private Label? _entryTitle;
    private Label? _entrySource;
    private VBoxContainer? _entryFacts;
    private Label? _count;
    private CompendiumPreview? _preview;
    public event Action? BackRequested;
    public int VisibleEntryCount => _visibleEntries.Length;
    public int SourceOptionCount => _sourceIds.Count;
    public int CategoryCount => _tabs?.TabCount ?? 0;
    public string CurrentDetailsText { get; private set; } = string.Empty;

    /// <summary>
    /// 获取界面节点，建立来源和分类选项，并连接筛选、选择与返回事件。
    /// </summary>
    public override void _Ready()
    {
        _sourceFilter = GetNode<OptionButton>("Panel/Padding/Layout/Filters/SourceFilter");
        _tabs = GetNode<TabBar>("Panel/Padding/Layout/CategoryTabs");
        _entryList = GetNode<ItemList>("Panel/Padding/Layout/Browser/EntryList");
        _entryTitle = GetNode<Label>(
            "Panel/Padding/Layout/Browser/Details/Layout/Identity/Heading/EntryTitle");
        _entrySource = GetNode<Label>(
            "Panel/Padding/Layout/Browser/Details/Layout/Identity/Heading/EntrySource");
        _entryFacts = GetNode<VBoxContainer>(
            "Panel/Padding/Layout/Browser/Details/Layout/EntryFacts");
        _preview = GetNode<CompendiumPreview>(
            "Panel/Padding/Layout/Browser/Details/Layout/Identity/PreviewFrame/Preview");
        _count = GetNode<Label>("Panel/Padding/Layout/Filters/Count");
        GetNode<Button>("Panel/Padding/Layout/Header/Back").Pressed += RequestBack;
        _sourceFilter.ItemSelected += OnSourceSelected;
        _tabs.TabChanged += OnTabChanged;
        _entryList.ItemSelected += OnEntrySelected;
        BuildSourceFilter();
        BuildCategoryTabs();
        SetProcessUnhandledInput(true);
        Hide();
    }

    /// <summary>
    /// 打开图鉴并恢复到全部来源的地区分页，确保每次进入都有明确起点。
    /// </summary>
    public void Present()
    {
        _sourceFilter!.Select(0);
        _tabs!.CurrentTab = 0;
        RebuildEntryList();
        Show();
        _entryList!.GrabFocus();
    }

    /// <summary>
    /// 图鉴可见时响应界面取消操作，关闭图鉴并阻止事件落到主菜单。
    /// </summary>
    public override void _UnhandledInput(InputEvent inputEvent)
    {
        if (!Visible || !inputEvent.IsActionPressed("ui_cancel"))
        {
            return;
        }

        RequestBack();
        GetViewport().SetInputAsHandled();
    }

    /// <summary>
    /// 依次加入全部内容、本体以及 TH01 至 TH20，使来源顺序与选择菜单一致。
    /// </summary>
    private void BuildSourceFilter()
    {
        AddSourceOption("全部内容", string.Empty);
        AddSourceOption(ContentPackCatalog.Base.DisplayName, CompendiumCatalog.BaseSourceId);
        foreach (ContentPackDefinition pack in ContentPackCatalog.All)
        {
            AddSourceOption($"TH{pack.Number:00} {pack.DisplayName}", pack.Id);
        }
    }

    /// <summary>
    /// 同时添加来源显示名和稳定 ID，避免筛选逻辑依赖中文标题。
    /// </summary>
    private void AddSourceOption(string label, string sourceId)
    {
        _sourceFilter!.AddItem(label);
        _sourceIds.Add(sourceId);
    }

    /// <summary>
    /// 建立地区、结构、敌人、角色和符卡五个固定标签页，顺序与分类枚举严格一致。
    /// </summary>
    private void BuildCategoryTabs()
    {
        _tabs!.AddTab("地区");
        _tabs.AddTab("结构");
        _tabs.AddTab("敌人");
        _tabs.AddTab("角色");
        _tabs.AddTab("符卡");
    }

    /// <summary>
    /// 来源变化时重建当前分类列表；信号索引由控件保证处于有效范围。
    /// </summary>
    private void OnSourceSelected(long index) => RebuildEntryList();

    /// <summary>
    /// 分类标签变化时保留来源筛选并重建左侧条目列表。
    /// </summary>
    private void OnTabChanged(long index) => RebuildEntryList();

    /// <summary>
    /// 按当前分类和来源过滤目录，刷新条目数量并默认展示第一项详情。
    /// </summary>
    private void RebuildEntryList()
    {
        string sourceId = _sourceIds[_sourceFilter!.Selected];
        var category = (CompendiumCategory)_tabs!.CurrentTab;
        _visibleEntries = CompendiumCatalog.All
            .Where(entry => entry.Category == category &&
                (sourceId.Length == 0 || entry.SourceId == sourceId))
            .OrderBy(entry => entry.SourceId == CompendiumCatalog.BaseSourceId ? 0 : 1)
            .ThenBy(entry => entry.SourceId, StringComparer.Ordinal)
            .ThenBy(entry => entry.Name, StringComparer.Ordinal)
            .ToArray();
        _entryList!.Clear();
        foreach (CompendiumEntry entry in _visibleEntries)
        {
            int itemIndex = _entryList.AddItem(entry.Name);
            _entryList.SetItemTooltip(itemIndex, $"{entry.SourceName}\n{entry.Summary}");
        }

        _count!.Text = $"{_visibleEntries.Length} 项";
        if (_visibleEntries.Length > 0)
        {
            _entryList.Select(0);
            ShowEntry(0);
        }
        else
        {
            ClearDetails();
        }
    }

    /// <summary>
    /// 将列表信号索引转换为数组索引，并更新右侧详情。
    /// </summary>
    private void OnEntrySelected(long index) => ShowEntry((int)index);

    /// <summary>
    /// 显示指定条目的名称、来源、摘要和完整运行时信息。
    /// </summary>
    private void ShowEntry(int index)
    {
        if (index < 0 || index >= _visibleEntries.Length)
        {
            return;
        }

        CompendiumEntry entry = _visibleEntries[index];
        FitEntryTitle(entry.Name);
        _entrySource!.Text = BuildVisualSourceLabel(entry);
        CurrentDetailsText = entry.Details;
        CompendiumFactView.Rebuild(_entryFacts!, entry.Facts);
        _preview!.SetEntry(entry);
    }

    /// <summary>
    /// 在现有来源行内标明跨作视觉代用；内部目录存在但条目无图时明确显示中文图标回退。
    /// </summary>
    private string BuildVisualSourceLabel(CompendiumEntry entry)
    {
        if (_internalPreviews.TryGet(entry, out InternalPreviewDefinition definition) &&
            !string.IsNullOrWhiteSpace(definition.ProxySourceWork))
        {
            return $"{entry.SourceName} · 视觉代用 {definition.ProxySourceWork}";
        }

        return _internalPreviews.Count > 0 && !_internalPreviews.Contains(entry)
            ? $"{entry.SourceName} · 中文图标回退"
            : entry.SourceName;
    }

    /// <summary>
    /// 当前筛选没有条目时清空详情区域，避免残留上一个分类的信息。
    /// </summary>
    private void ClearDetails()
    {
        FitEntryTitle("暂无条目");
        _entrySource!.Text = string.Empty;
        CurrentDetailsText = string.Empty;
        CompendiumFactView.Rebuild(_entryFacts!, []);
        _preview!.SetEntry(null);
    }

    /// <summary>
    /// 以当前主题字体实测标题宽度，从 15px 逐级缩到 10px，确保长姓名保持单行且短姓名不变小。
    /// </summary>
    private void FitEntryTitle(string text)
    {
        const int largestSize = 15;
        const int smallestSize = 10;
        Label label = _entryTitle!;
        label.Text = text;
        float availableWidth = Math.Max(1.0f, ((Control)label.GetParent()).Size.X);
        Font font = label.GetThemeFont("font");
        int fontSize = largestSize;
        while (fontSize > smallestSize &&
            font.GetStringSize(text, HorizontalAlignment.Left, -1.0f, fontSize).X > availableWidth)
        {
            fontSize--;
        }

        label.AddThemeFontSizeOverride("font_size", fontSize);
    }

    /// <summary>
    /// 隐藏图鉴并通知主菜单恢复命令区域。
    /// </summary>
    private void RequestBack()
    {
        Hide();
        BackRequested?.Invoke();
    }
}
