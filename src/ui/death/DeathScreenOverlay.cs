using Godot;
using TouhouWuxiaSurvivor.Gameplay.Session;

namespace TouhouWuxiaSurvivor.Ui.Death;

/// <summary>
/// 管理不可关闭的失败弹出层和本局总结页，并把重新开始与返回主菜单意图交给场景根节点。
/// </summary>
public partial class DeathScreenOverlay : CanvasLayer
{
    private Control? _root;
    private Control? _deathPopup;
    private Control? _summaryPanel;
    private Label? _outcomeTitle;
    private Label? _outcomeMessage;
    private Label? _quickStats;
    private Label? _survivalValue;
    private Label? _defeatedValue;
    private Label? _locationValue;
    private Label? _contentValue;
    private Label? _seedValue;
    private Label? _levelValue;
    private Label? _experienceValue;
    private Label? _buildValue;
    private Label? _rewardValue;
    private Label? _jadeBalanceValue;
    private Button? _viewSummaryButton;

    public bool IsOpen => _root?.Visible ?? false;
    public bool IsShowingSummary => _summaryPanel?.Visible ?? false;
    public RunSummary? CurrentSummary { get; private set; }
    public event Action? RestartRequested;
    public event Action? MainMenuRequested;

    /// <summary>
    /// 缓存界面节点、连接按钮命令，并让失败界面在本局进行时完全隐藏。
    /// </summary>
    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        _root = GetNode<Control>("Root");
        _deathPopup = GetNode<Control>("Root/DeathPopup");
        _summaryPanel = GetNode<Control>("Root/SummaryPanel");
        _outcomeTitle = GetNode<Label>("Root/DeathPopup/Padding/Layout/Title");
        _outcomeMessage = GetNode<Label>("Root/DeathPopup/Padding/Layout/Message");
        _quickStats = GetNode<Label>("Root/DeathPopup/Padding/Layout/QuickStats");
        _survivalValue = GetNode<Label>("Root/SummaryPanel/Padding/Layout/StatsColumns/LeftStats/SurvivalValue");
        _defeatedValue = GetNode<Label>("Root/SummaryPanel/Padding/Layout/StatsColumns/LeftStats/DefeatedValue");
        _levelValue = GetNode<Label>("Root/SummaryPanel/Padding/Layout/StatsColumns/LeftStats/LevelValue");
        _experienceValue = GetNode<Label>("Root/SummaryPanel/Padding/Layout/StatsColumns/LeftStats/ExperienceValue");
        _rewardValue = GetNode<Label>("Root/SummaryPanel/Padding/Layout/StatsColumns/LeftStats/RewardValue");
        _locationValue = GetNode<Label>("Root/SummaryPanel/Padding/Layout/StatsColumns/RightStats/LocationValue");
        _contentValue = GetNode<Label>("Root/SummaryPanel/Padding/Layout/StatsColumns/RightStats/ContentValue");
        _seedValue = GetNode<Label>("Root/SummaryPanel/Padding/Layout/StatsColumns/RightStats/SeedValue");
        _jadeBalanceValue = GetNode<Label>("Root/SummaryPanel/Padding/Layout/StatsColumns/RightStats/JadeBalanceValue");
        _buildValue = GetNode<Label>("Root/SummaryPanel/Padding/Layout/BuildRow/BuildValue");
        _viewSummaryButton = GetNode<Button>("Root/DeathPopup/Padding/Layout/Buttons/ViewSummary");

        _viewSummaryButton.Pressed += ShowSummary;
        GetNode<Button>("Root/DeathPopup/Padding/Layout/Buttons/MainMenu").Pressed += RequestMainMenu;
        GetNode<Button>("Root/SummaryPanel/Padding/Layout/Buttons/Restart").Pressed += RequestRestart;
        GetNode<Button>("Root/SummaryPanel/Padding/Layout/Buttons/MainMenu").Pressed += RequestMainMenu;
        _root.Hide();
    }

    /// <summary>
    /// 写入失败瞬间的统计快照，暂停整棵场景树并首先展示带终局原因的精简弹窗。
    /// </summary>
    public void Present(RunSummary summary)
    {
        CurrentSummary = summary;
        _outcomeTitle!.Text = RunSummaryTextFormatter.FormatOutcomeTitle(summary.EndReason);
        _outcomeMessage!.Text = RunSummaryTextFormatter.FormatOutcomeMessage(summary.EndReason);
        _quickStats!.Text = RunSummaryTextFormatter.FormatQuickStats(summary);
        _survivalValue!.Text = RunSummaryTextFormatter.FormatDuration(summary.SurvivalSeconds);
        _defeatedValue!.Text = summary.DefeatedEnemies.ToString();
        _locationValue!.Text = $"{summary.BiomeName}  ({summary.TileX}, {summary.TileY})";
        _contentValue!.Text = summary.ActiveContent;
        _seedValue!.Text = summary.WorldSeed.ToString();
        _levelValue!.Text = summary.FinalLevel.ToString();
        _experienceValue!.Text = summary.TotalExperience.ToString();
        _buildValue!.Text = summary.BuildSummary;
        _buildValue.TooltipText = summary.BuildSummary;
        _rewardValue!.Text = $"+{summary.RewardEarned}";
        _jadeBalanceValue!.Text = summary.MoneyBalance.ToString();
        _summaryPanel!.Hide();
        _deathPopup!.Show();
        _root!.Show();
        GetTree().Paused = true;
        _viewSummaryButton!.GrabFocus();
    }

    /// <summary>
    /// 从失败弹窗进入详细总结页，保留暂停状态并把焦点移到重新开始按钮。
    /// </summary>
    public void ShowSummary()
    {
        if (!IsOpen)
        {
            return;
        }

        _deathPopup!.Hide();
        _summaryPanel!.Show();
        GetNode<Button>("Root/SummaryPanel/Padding/Layout/Buttons/Restart").GrabFocus();
    }

    /// <summary>
    /// 广播重新开始命令，场景根节点负责恢复暂停并重载当前世界。
    /// </summary>
    private void RequestRestart() => RestartRequested?.Invoke();

    /// <summary>
    /// 广播返回主菜单命令，场景根节点负责恢复暂停并执行场景切换。
    /// </summary>
    private void RequestMainMenu() => MainMenuRequested?.Invoke();
}
