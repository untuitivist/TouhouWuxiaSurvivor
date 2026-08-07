using Godot;
using TouhouWuxiaSurvivor.Gameplay.Meta.Definitions;
using TouhouWuxiaSurvivor.Gameplay.Meta.Runtime;

namespace TouhouWuxiaSurvivor.Ui.Meta;

/// <summary>
/// 在单页无滚动面板中展示钱财、解锁条件、四项博丽整备和带确认的档案重置。
/// </summary>
public partial class CultivationPanel : Control
{
    private readonly List<Button> _choiceButtons = [];
    private ProgressionProfileManager? _profile;
    private Label? _currency;
    private Label? _history;
    private Label? _status;
    private Button? _reset;
    private bool _resetArmed;

    public bool IsOpen => Visible;
    public string StatusText => _status?.Text ?? string.Empty;
    public event Action? BackRequested;

    /// <summary>
    /// 缓存固定四行修行按钮及命令节点，并连接索引稳定的购买、重置和返回回调。
    /// </summary>
    public override void _Ready()
    {
        _currency = GetNode<Label>("Panel/Padding/Layout/Header/Currency");
        _history = GetNode<Label>("Panel/Padding/Layout/History");
        _status = GetNode<Label>("Panel/Padding/Layout/Status");
        _reset = GetNode<Button>("Panel/Padding/Layout/Footer/Reset");
        for (int index = 0; index < CultivationCatalog.All.Count; index++)
        {
            int capturedIndex = index;
            Button button = GetNode<Button>($"Panel/Padding/Layout/Choices/Choice{index}");
            button.Pressed += () => PurchaseAt(capturedIndex);
            _choiceButtons.Add(button);
        }

        GetNode<Button>("Panel/Padding/Layout/Header/Back").Pressed += RequestBack;
        _reset.Pressed += RequestReset;
        Hide();
    }

    /// <summary>
    /// 注入主菜单持有的档案管理器，使所有按钮只通过领域规则购买和持久化。
    /// </summary>
    public void Configure(ProgressionProfileManager profile)
    {
        _profile = profile;
        _profile.Changed += RefreshProfile;
    }

    /// <summary>
    /// 重置确认状态、刷新完整档案显示并把焦点放到首个可用修行。
    /// </summary>
    public void Present()
    {
        SetResetArmed(false);
        RefreshProfile();
        _status!.Text = "选择一项神社整备，永久效果将在下一局生效。";
        Show();
        _choiceButtons.FirstOrDefault(button => !button.Disabled)?.GrabFocus();
    }

    /// <summary>
    /// 按目录索引请求购买并把领域结果转换为简短中文反馈，越界索引安全忽略。
    /// </summary>
    public void PurchaseAt(int index)
    {
        if (_profile is null || index < 0 || index >= CultivationCatalog.All.Count)
        {
            return;
        }

        CultivationDefinition definition = CultivationCatalog.All[index];
        CultivationPurchaseResult result = _profile.Purchase(definition.Id);
        _status!.Text = FormatPurchaseResult(result);
        SetResetArmed(false);
        RefreshProfile();
    }

    /// <summary>
    /// 第一次调用只武装危险操作，第二次调用才覆盖为默认档案并立即刷新界面。
    /// </summary>
    public void RequestReset()
    {
        if (_profile is null)
        {
            return;
        }

        if (!_resetArmed)
        {
            SetResetArmed(true);
            _status!.Text = "再次点击确认重置全部钱财、战绩与整备。";
            return;
        }

        bool reset = _profile.Reset();
        SetResetArmed(false);
        _status!.Text = reset ? "神社整备记录已重置。" : "保存失败，原有整备未改变。";
        RefreshProfile();
    }

    /// <summary>
    /// 从当前档案重建货币、累计战绩和四行修行文本，并正确禁用未解锁或满重项目。
    /// </summary>
    private void RefreshProfile()
    {
        if (_profile is null || _currency is null || _history is null)
        {
            return;
        }

        var profile = _profile.Current;
        _currency.Text = $"钱  {profile.Money}";
        _history.Text = $"累计收入 {profile.LifetimeMoney}    出行次数 {profile.CompletedRuns}";
        for (int index = 0; index < CultivationCatalog.All.Count; index++)
        {
            CultivationDefinition definition = CultivationCatalog.All[index];
            int rank = profile.GetRank(definition.Id);
            bool unlocked = profile.LifetimeMoney >= definition.UnlockLifetimeMoney;
            bool maxed = rank >= definition.MaxRank;
            Button button = _choiceButtons[index];
            button.Disabled = !unlocked || maxed;
            button.Text = FormatChoice(definition, rank, unlocked, maxed);
        }
    }

    /// <summary>
    /// 生成单行修行文本，分别表达未解锁门槛、已圆满状态或下一重费用。
    /// </summary>
    private static string FormatChoice(
        CultivationDefinition definition,
        int rank,
        bool unlocked,
        bool maxed)
    {
        if (!unlocked)
        {
            return $"{definition.DisplayName}  未解锁    累计收入 {definition.UnlockLifetimeMoney}";
        }

        if (maxed)
        {
            return $"{definition.DisplayName}  {rank}/{definition.MaxRank}    已圆满";
        }

        return $"{definition.DisplayName}  {rank}/{definition.MaxRank}    " +
            $"{definition.Description}    需 {definition.GetCost(rank)} 钱";
    }

    /// <summary>
    /// 把强类型购买结果格式化为不会泄漏存储细节的中文状态信息。
    /// </summary>
    private string FormatPurchaseResult(CultivationPurchaseResult result)
    {
        CultivationDefinition? definition = result.Definition;
        return result.Status switch
        {
            CultivationPurchaseStatus.Purchased =>
                $"{definition!.DisplayName} 已提升至 {_profile!.Current.GetRank(definition.Id)} 重。",
            CultivationPurchaseStatus.Locked =>
                $"尚未解锁，需要累计收入 {definition!.UnlockLifetimeMoney}。",
            CultivationPurchaseStatus.MaxRank => $"{definition!.DisplayName} 已经圆满。",
            CultivationPurchaseStatus.InsufficientJade =>
                $"钱不够，还需 {definition!.GetCost(_profile!.Current.GetRank(definition.Id)) - _profile.Current.Money}。",
            CultivationPurchaseStatus.SaveFailed => "保存失败，本次整备没有扣钱。",
            _ => "未找到这门修行。",
        };
    }

    /// <summary>
    /// 同步重置确认标志与按钮文本，避免关闭再进入后保留危险操作状态。
    /// </summary>
    private void SetResetArmed(bool armed)
    {
        _resetArmed = armed;
        if (_reset is not null)
        {
            _reset.Text = armed ? "确认重置全部整备" : "重置整备";
        }
    }

    /// <summary>
    /// 取消重置确认、隐藏面板并请求主菜单恢复命令区域。
    /// </summary>
    private void RequestBack()
    {
        SetResetArmed(false);
        Hide();
        BackRequested?.Invoke();
    }
}
