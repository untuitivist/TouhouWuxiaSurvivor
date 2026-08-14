using Godot;
using TouhouWuxiaSurvivor.Gameplay.Progression.Definitions;
using TouhouWuxiaSurvivor.Gameplay.Progression.Runtime;
using TouhouWuxiaSurvivor.Ui.Map;
using TouhouWuxiaSurvivor.Ui.Pause;
using TouhouWuxiaSurvivor.Ui.Stats;

namespace TouhouWuxiaSurvivor.Ui.Progression;

/// <summary>
/// 显示不可跳过的三选一修行突破，并独占本次升级期间的暂停与输入状态。
/// </summary>
public partial class LevelUpOverlay : CanvasLayer
{
    private readonly List<RunUpgradeChoiceCard> _cards = [];
    private IReadOnlyList<RunUpgradeChoice> _choices = [];
    private Control? _root;
    private Label? _levelLabel;
    private Label? _routeLabel;
    private WorldMapOverlay? _map;
    private PauseMenuOverlay? _pauseMenu;
    private CharacterStatsOverlay? _stats;
    private bool _wasPaused;

    public bool IsOpen => _root?.Visible == true;
    public int ChoiceCount => _choices.Count;
    public event Action<RunUpgradeChoice>? ChoiceSelected;

    /// <summary>
    /// 缓存标题和三个固定选择按钮，连接索引稳定的选择回调并默认隐藏界面。
    /// </summary>
    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        _root = GetNode<Control>("Root");
        _levelLabel = GetNode<Label>("Root/Panel/Padding/Layout/Level");
        _routeLabel = GetNode<Label>("Root/Panel/Padding/Layout/Route");
        for (int index = 0; index < 3; index++)
        {
            int capturedIndex = index;
            RunUpgradeChoiceCard card = GetNode<RunUpgradeChoiceCard>(
                $"Root/Panel/Padding/Layout/Choices/Choice{index}");
            card.Pressed += () => SelectChoice(capturedIndex);
            _cards.Add(card);
        }

        _root.Hide();
    }

    /// <summary>
    /// 注入地图、暂停菜单与属性页，使升级层能够在显示期间明确阻断其他模态输入。
    /// </summary>
    public void Configure(
        WorldMapOverlay map,
        PauseMenuOverlay pauseMenu,
        CharacterStatsOverlay stats)
    {
        _map = map;
        _pauseMenu = pauseMenu;
        _stats = stats;
    }

    /// <summary>
    /// 写入本次候选项；首次打开时记录暂停状态，多级连续选择时保持同一暂停所有权。
    /// </summary>
    public void Present(
        IReadOnlyList<RunUpgradeChoice> choices,
        RunBuildState build,
        int level)
    {
        if (!IsOpen)
        {
            _wasPaused = GetTree().Paused;
            GetTree().Paused = true;
            SetOtherInputBlocked(true);
        }

        _choices = choices;
        _levelLabel!.Text = $"境界 {level}";
        _routeLabel!.Text = RunUpgradeChoicePresentationFactory.FormatCurrentRoute(build);
        for (int index = 0; index < _cards.Count; index++)
        {
            bool available = index < choices.Count;
            _cards[index].Visible = available;
            if (available)
            {
                _cards[index].Present(choices[index], build);
            }
        }

        _root!.Show();
        _cards.FirstOrDefault(card => card.Visible)?.GrabFocus();
    }

    /// <summary>
    /// 将有效按钮索引转换为升级定义并广播，越界或隐藏状态不会产生重复选择。
    /// </summary>
    public void SelectChoice(int index)
    {
        if (!IsOpen || index < 0 || index >= _choices.Count)
        {
            return;
        }

        RunUpgradeChoice choice = _choices[index];
        _choices = [];
        ChoiceSelected?.Invoke(choice);
    }

    /// <summary>
    /// 完成全部待选升级后关闭界面、解除模态阻断并恢复打开前的暂停状态。
    /// </summary>
    public void CloseAndRestore()
    {
        _choices = [];
        _root?.Hide();
        SetOtherInputBlocked(false);
        GetTree().Paused = _wasPaused;
    }

    /// <summary>
    /// 本局结束并抢占模态状态时只隐藏升级层，保留暂停和输入阻断交给终局流程接管。
    /// </summary>
    public void CancelForRunEnd()
    {
        _choices = [];
        _root?.Hide();
    }

    /// <summary>
    /// 同步设置地图与暂停菜单的输入阻断状态，避免两个组件发生不一致。
    /// </summary>
    private void SetOtherInputBlocked(bool blocked)
    {
        if (_map is not null)
        {
            _map.InputBlocked = blocked;
        }

        if (_pauseMenu is not null)
        {
            _pauseMenu.InputBlocked = blocked;
        }

        if (_stats is not null)
        {
            _stats.InputBlocked = blocked;
        }
    }
}
