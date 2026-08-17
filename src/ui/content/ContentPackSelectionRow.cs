using Godot;
using TouhouWuxiaSurvivor.Content;

namespace TouhouWuxiaSurvivor.Ui.Content;

/// <summary>
/// 表示一部作品的独立选择与折叠行，使启用状态、展开状态和旧作可见性互不干扰。
/// </summary>
public partial class ContentPackSelectionRow : VBoxContainer
{
    private CheckBox? _selection;
    private Button? _titleButton;
    private Label? _details;

    public ContentPackDefinition? Definition { get; private set; }
    public bool IsBase { get; private set; }
    public bool IsOldWork => !IsBase && Definition?.Number <= 5;
    public bool IsExpanded { get; private set; }
    public bool IsSelected => _selection?.ButtonPressed == true;
    public bool DetailsVisible => _details?.Visible == true;
    public string HeaderText => _titleButton?.Text ?? string.Empty;
    public string DetailsText => _details?.Text ?? string.Empty;
    public event Action? SelectionChanged;

    /// <summary>
    /// 根据内容定义构造复选框、可点击作品名和默认隐藏详情；本体保持勾选且不可取消。
    /// </summary>
    public void Configure(ContentPackDefinition definition, bool isBase)
    {
        Definition = definition;
        IsBase = isBase;
        AddThemeConstantOverride("separation", 1);

        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 3);
        AddChild(header);

        _selection = new CheckBox
        {
            ButtonPressed = isBase,
            Disabled = isBase || !definition.Selectable,
            CustomMinimumSize = new Vector2(22.0f, 24.0f),
            TooltipText = BuildSelectionTooltip(definition, isBase),
        };
        _selection.Toggled += OnSelectionToggled;
        header.AddChild(_selection);

        _titleButton = new Button
        {
            Flat = true,
            Alignment = HorizontalAlignment.Left,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0.0f, 24.0f),
            TooltipText = "展开内容清单",
        };
        _titleButton.AddThemeFontSizeOverride("font_size", 12);
        _titleButton.Pressed += ToggleExpanded;
        header.AddChild(_titleButton);

        _details = new Label
        {
            Text = BuildDetails(definition, isBase),
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Visible = false,
        };
        _details.AddThemeColorOverride("font_color", new Color("aeb9aa"));
        _details.AddThemeFontSizeOverride("font_size", 11);
        AddChild(_details);
        SetExpanded(false);
    }

    /// <summary>
    /// 同步本局是否启用该作品；禁用项和幻想乡本体始终保持原有固定状态。
    /// </summary>
    public void SetSelected(bool selected)
    {
        if (_selection is not null && !_selection.Disabled)
        {
            _selection.ButtonPressed = selected;
        }
    }

    /// <summary>
    /// 显式设置详情可见性并刷新箭头、名称和提示，不改变作品勾选状态。
    /// </summary>
    public void SetExpanded(bool expanded)
    {
        IsExpanded = expanded;
        if (_details is not null)
        {
            _details.Visible = expanded;
        }

        if (_titleButton is not null && Definition is not null)
        {
            _titleButton.Text = $"{(expanded ? "▼" : "▶")}  {BuildTitle(Definition, IsBase)}";
            _titleButton.TooltipText = expanded ? "收起内容清单" : "展开内容清单";
        }
    }

    /// <summary>
    /// 按当前状态独立展开或收起本行，供标题按钮和集成测试走同一交互路径。
    /// </summary>
    public void ToggleExpanded() => SetExpanded(!IsExpanded);

    /// <summary>
    /// 根据总开关显示或隐藏旧作；Windows 正作与本体不受该开关影响。
    /// </summary>
    public void ApplyOldWorkVisibility(bool showOldWorks) =>
        Visible = !IsOldWork || showOldWorks;

    /// <summary>
    /// 把复选框变化转发给父面板，使角色候选立即跟随启用作品刷新而不保存半成品快照。
    /// </summary>
    private void OnSelectionToggled(bool selected) => SelectionChanged?.Invoke();

    /// <summary>
    /// 生成折叠标题，只包含作品编号与名称，不泄漏状态或内容详情。
    /// </summary>
    private static string BuildTitle(ContentPackDefinition definition, bool isBase) =>
        isBase ? definition.DisplayName : $"TH{definition.Number:00}  {definition.DisplayName}";

    /// <summary>
    /// 将开发状态及各内容分类按行展开，空清单明确标记尚未规划。
    /// </summary>
    private static string BuildDetails(ContentPackDefinition definition, bool isBase)
    {
        string status = BuildStatusText(definition.Status);
        if (isBase)
        {
            status += " · 始终启用";
        }

        if (definition.Additions.Count == 0)
        {
            return $"状态：{status}\n内容清单：尚未规划";
        }

        string additions = string.Join("\n", definition.Additions
            .GroupBy(addition => addition.Category)
            .Select(group => $"{group.Key}：{string.Join("、", group.Select(item => item.Name))}"));
        return $"状态：{status}\n{additions}";
    }

    /// <summary>
    /// 把强类型完成度转换为稳定中文文案；只有通过完整验收的内容才能显示为已完成。
    /// </summary>
    private static string BuildStatusText(ContentPackStatus status) => status switch
    {
        ContentPackStatus.Inventory => "资料已登记 · 待迁移验收",
        ContentPackStatus.Development => "开发中",
        ContentPackStatus.Complete => "已完成",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };

    /// <summary>
    /// 根据包状态说明勾选的真实含义，避免把可试玩的迁移库存误解为已经完成。
    /// </summary>
    private static string BuildSelectionTooltip(ContentPackDefinition definition, bool isBase)
    {
        if (isBase)
        {
            return "幻想乡本体始终启用";
        }

        return definition.Status == ContentPackStatus.Inventory
            ? "资料已登记但仍待迁移验收；勾选后加入本局现有实验内容"
            : "勾选后加入本局内容";
    }
}
