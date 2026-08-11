using Godot;
using TouhouWuxiaSurvivor.Content;
using TouhouWuxiaSurvivor.Content.Characters;

namespace TouhouWuxiaSurvivor.Ui.Content;

/// <summary>
/// 显示本体与全部整数编号正作，并提供旧作过滤、独立折叠和本局内容选择。
/// </summary>
public partial class ContentPackSelectionPanel : Control
{
    private readonly Dictionary<string, ContentPackSelectionRow> _rows =
        new(StringComparer.Ordinal);
    private VBoxContainer? _packList;
    private CheckButton? _showOldWorks;
    private ScrollContainer? _scroll;
    private OptionButton? _characterChoice;

    public event Action? StartRequested;
    public event Action? BackRequested;
    public int ListedPackCount => _rows.Count;
    public int VisibleOfficialPackCount => _rows.Values.Count(row => row.Visible);
    public bool ShowOldWorks => _showOldWorks?.ButtonPressed == true;
    public string SelectedCharacterId => _characterChoice is not null &&
        _characterChoice.Selected >= 0
            ? _characterChoice.GetItemMetadata(_characterChoice.Selected).AsString()
            : CharacterCatalog.Default.CharacterId;

    /// <summary>
    /// 获取列表容器、按清单构建所有行，并连接开始和返回命令。
    /// </summary>
    public override void _Ready()
    {
        _packList = GetNode<VBoxContainer>("Panel/Padding/Layout/Scroll/PackList");
        _scroll = GetNode<ScrollContainer>("Panel/Padding/Layout/Scroll");
        _showOldWorks = GetNode<CheckButton>(
            "Panel/Padding/Layout/VisibilityFilters/ShowOldWorks");
        _characterChoice = GetNode<OptionButton>(
            "Panel/Padding/Layout/CharacterSelection/CharacterChoice");
        GetNode<Button>("Panel/Padding/Layout/Commands/Back").Pressed += RequestBack;
        GetNode<Button>("Panel/Padding/Layout/Commands/Start").Pressed += CommitAndStart;
        _showOldWorks.Toggled += OnShowOldWorksToggled;
        BuildPackList();
        SetProcessUnhandledInput(true);
        Hide();
    }

    /// <summary>
    /// 选择层可见时把界面取消操作解释为返回，并消费事件避免传递给底层主菜单。
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
    /// 使用当前服务快照同步所有可选勾选框，并显示内容选择层。
    /// </summary>
    public void Present()
    {
        ContentPackSelection current = ContentPackSelectionService.Current;
        foreach ((string id, ContentPackSelectionRow row) in _rows)
        {
            row.SetSelected(current.IsEnabled(id));
            row.SetExpanded(false);
        }

        RefreshCharacterChoices(CharacterSelectionService.Current.CharacterId);
        ApplyOldWorkVisibility();
        Show();
        _scroll!.ScrollVertical = 0;
        GetNode<Button>("Panel/Padding/Layout/Commands/Start").GrabFocus();
    }

    /// <summary>
    /// 依次添加始终启用的本体和全部正作行，目录新增项目会自动进入同一折叠列表。
    /// </summary>
    private void BuildPackList()
    {
        AddPackRow(ContentPackCatalog.Base, true);
        foreach (ContentPackDefinition definition in ContentPackCatalog.All)
        {
            AddPackRow(definition, false);
        }
    }

    /// <summary>
    /// 创建封装选择与详情状态的作品行，并记录全部正作的稳定 ID 映射。
    /// </summary>
    private void AddPackRow(ContentPackDefinition definition, bool isBase)
    {
        var row = new ContentPackSelectionRow();
        _packList!.AddChild(row);
        row.Configure(definition, isBase);
        row.SelectionChanged += OnPackSelectionChanged;

        if (!isBase)
        {
            _rows.Add(definition.Id, row);
        }
    }

    /// <summary>
    /// 返回指定内容包的行组件，供测试与后续键盘导航读取稳定状态而不依赖子节点索引。
    /// </summary>
    public ContentPackSelectionRow GetPackRow(string packId)
    {
        if (_rows.TryGetValue(packId, out ContentPackSelectionRow? row))
        {
            return row;
        }

        throw new KeyNotFoundException($"Unknown content pack row: {packId}");
    }

    /// <summary>
    /// 总开关变化时仅更新旧作行可见性，保留每一行的勾选与展开状态。
    /// </summary>
    private void OnShowOldWorksToggled(bool enabled) => ApplyOldWorkVisibility();

    /// <summary>
    /// 将当前显示旧作状态应用到全部正作行，本体行始终由自身规则保持可见。
    /// </summary>
    private void ApplyOldWorkVisibility()
    {
        foreach (ContentPackSelectionRow row in _rows.Values)
        {
            row.ApplyOldWorkVisibility(ShowOldWorks);
        }
    }

    /// <summary>
    /// 作品勾选变化时保留仍然可用的当前角色；若来源全部关闭则安全回到本体灵梦。
    /// </summary>
    private void OnPackSelectionChanged() => RefreshCharacterChoices(SelectedCharacterId);

    /// <summary>
    /// 按面板当前勾选构造角色下拉项，元数据保存稳定 ID，显示文本只承担本地化姓名。
    /// </summary>
    private void RefreshCharacterChoices(string preferredCharacterId)
    {
        if (_characterChoice is null)
        {
            return;
        }

        ContentPackSelection selection = CreateSelectionSnapshot();
        IReadOnlyList<CharacterDefinition> available = CharacterCatalog.GetAvailable(selection);
        _characterChoice.Clear();
        int selectedIndex = 0;
        for (int index = 0; index < available.Count; index++)
        {
            CharacterDefinition character = available[index];
            _characterChoice.AddItem(character.DisplayName);
            _characterChoice.SetItemMetadata(index, character.CharacterId);
            if (character.CharacterId == preferredCharacterId)
            {
                selectedIndex = index;
            }
        }

        _characterChoice.Select(selectedIndex);
    }

    /// <summary>
    /// 从当前行状态创建一次不可变内容选择，供角色筛选与最终提交复用同一规则。
    /// </summary>
    private ContentPackSelection CreateSelectionSnapshot() => new(_rows
        .Where(pair => pair.Value.IsSelected)
        .Select(pair => pair.Key));

    /// <summary>
    /// 收集所有已启用且可选择的内容包，保存为下一局快照并请求进入游戏。
    /// </summary>
    private void CommitAndStart()
    {
        ContentPackSelection selection = CreateSelectionSnapshot();
        ContentPackSelectionService.Apply(selection);
        CharacterSelectionService.Apply(SelectedCharacterId, selection);
        StartRequested?.Invoke();
    }

    /// <summary>
    /// 隐藏选择层并通知主菜单恢复命令区域，不修改当前内容选择。
    /// </summary>
    private void RequestBack()
    {
        Hide();
        BackRequested?.Invoke();
    }
}
