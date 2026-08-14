using Godot;
using TouhouWuxiaSurvivor.Gameplay.Session;

namespace TouhouWuxiaSurvivor.Ui.Completion;

/// <summary>
/// 在首次最终Boss击破后暂停世界，并让玩家明确选择成功结算或保留构筑进入无尽游历。
/// </summary>
public partial class RunCompletionOverlay : CanvasLayer
{
    private Control? _root;
    private Label? _message;
    private Label? _stats;
    private Button? _settleButton;

    public bool IsOpen => _root?.Visible == true;
    public event Action? SettleRequested;
    public event Action? ContinueEndlessRequested;

    /// <summary>
    /// 缓存界面节点并连接两个互斥命令；本局开始时整个覆盖层保持隐藏。
    /// </summary>
    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        _root = GetNode<Control>("Root");
        _message = GetNode<Label>("Root/Panel/Padding/Layout/Message");
        _stats = GetNode<Label>("Root/Panel/Padding/Layout/Stats");
        _settleButton = GetNode<Button>("Root/Panel/Padding/Layout/Buttons/Settle");
        _settleButton.Pressed += RequestSettle;
        GetNode<Button>("Root/Panel/Padding/Layout/Buttons/Endless").Pressed +=
            RequestEndless;
        _root.Hide();
    }

    /// <summary>
    /// 写入被击破角色与完成时间，暂停场景树并把键盘焦点交给成功结算按钮。
    /// </summary>
    public void Present(string bossName, double elapsedSeconds)
    {
        string safeName = string.IsNullOrWhiteSpace(bossName) ? "异变核心" : bossName;
        _message!.Text = $"{safeName}已经退去，幻想乡暂归平静。";
        _stats!.Text = $"完成用时  {RunSummaryTextFormatter.FormatDuration(elapsedSeconds)}";
        _root!.Show();
        GetTree().Paused = true;
        _settleButton!.GrabFocus();
    }

    /// <summary>
    /// 在转交成功结算前仅隐藏自身并维持暂停，由统一终局协调器继续持有输入所有权。
    /// </summary>
    public void CloseForSettlement() => _root?.Hide();

    /// <summary>
    /// 在玩家选择无尽后隐藏覆盖层并恢复场景树，让既有构筑和世界状态继续运行。
    /// </summary>
    public void CloseAndResume()
    {
        _root?.Hide();
        GetTree().Paused = false;
    }

    /// <summary>把成功结算按钮转换为语义事件，场景跳转仍由外部运行时负责。</summary>
    private void RequestSettle() => SettleRequested?.Invoke();

    /// <summary>把继续游历按钮转换为语义事件，界面本身不决定后续Boss或难度规则。</summary>
    private void RequestEndless() => ContinueEndlessRequested?.Invoke();
}
