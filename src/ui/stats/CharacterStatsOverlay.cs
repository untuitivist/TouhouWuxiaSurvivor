using Godot;
using TouhouWuxiaSurvivor.Ui.Map;
using TouhouWuxiaSurvivor.Ui.Pause;

namespace TouhouWuxiaSurvivor.Ui.Stats;

/// <summary>
/// 管理 E 键属性页、暂停所有权及其与地图、暂停菜单、升级和死亡层的互斥输入。
/// </summary>
public partial class CharacterStatsOverlay : CanvasLayer
{
    private Control? _root;
    private Func<CharacterStatsSnapshot>? _snapshotProvider;
    private WorldMapOverlay? _map;
    private PauseMenuOverlay? _pauseMenu;
    private bool _wasPaused;

    public bool IsOpen => _root?.Visible == true;
    public bool InputBlocked { get; set; }

    /// <summary>
    /// 缓存根节点、连接关闭按钮、启用暂停输入处理，并让属性页默认隐藏。
    /// </summary>
    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        _root = GetNode<Control>("Root");
        GetNode<Button>("Root/Panel/Padding/Layout/Header/Close").Pressed += Close;
        _root.Hide();
        SetProcessUnhandledInput(true);
    }

    /// <summary>
    /// 注入快照工厂与互斥覆盖层，使属性页只依赖稳定接口而不查找世界节点。
    /// </summary>
    public void Configure(
        Func<CharacterStatsSnapshot> snapshotProvider,
        WorldMapOverlay map,
        PauseMenuOverlay pauseMenu)
    {
        _snapshotProvider = snapshotProvider;
        _map = map;
        _pauseMenu = pauseMenu;
    }

    /// <summary>
    /// 处理 E 开关、ESC 关闭以及属性页到 M 地图的一步切换，并消费所有已处理事件。
    /// </summary>
    public override void _UnhandledInput(InputEvent inputEvent)
    {
        if (InputBlocked)
        {
            return;
        }

        if (inputEvent.IsActionPressed("toggle_stats"))
        {
            if (_pauseMenu?.IsOpen == true)
            {
                return;
            }

            if (IsOpen)
            {
                Close();
            }
            else
            {
                _map?.Close();
                Open();
            }

            GetViewport().SetInputAsHandled();
        }
        else if (IsOpen && inputEvent.IsActionPressed("toggle_map"))
        {
            Close();
            _map?.Open();
            GetViewport().SetInputAsHandled();
        }
        else if (IsOpen && inputEvent.IsActionPressed("pause_menu"))
        {
            Close();
            GetViewport().SetInputAsHandled();
        }
    }

    /// <summary>
    /// 关闭地图、记录此前暂停状态、阻断其他模态输入并以最新快照填充全部属性。
    /// </summary>
    public void Open()
    {
        if (IsOpen || _root is null || _snapshotProvider is null)
        {
            return;
        }

        _map?.Close();
        _wasPaused = GetTree().Paused;
        GetTree().Paused = true;
        SetOtherInputBlocked(true);
        Refresh(_snapshotProvider());
        _root.Show();
        GetNode<Button>("Root/Panel/Padding/Layout/Header/Close").GrabFocus();
    }

    /// <summary>
    /// 隐藏属性页、解除互斥输入并精确恢复打开前的暂停状态。
    /// </summary>
    public void Close()
    {
        if (!IsOpen || _root is null)
        {
            return;
        }

        _root.Hide();
        SetOtherInputBlocked(false);
        GetTree().Paused = _wasPaused;
    }

    /// <summary>
    /// 终局层抢占时仅隐藏属性页，不恢复暂停或解除由失败流程接管的输入阻断。
    /// </summary>
    public void CancelForRunEnd() => _root?.Hide();

    /// <summary>
    /// 场景退出时若属性页仍打开则恢复进入前暂停状态，避免污染后续场景。
    /// </summary>
    public override void _ExitTree()
    {
        if (IsOpen)
        {
            GetTree().Paused = _wasPaused;
        }
    }

    /// <summary>
    /// 把不可变快照写入固定无滚动节点，数值采用适合像素界面的紧凑格式。
    /// </summary>
    private void Refresh(CharacterStatsSnapshot snapshot)
    {
        SetText("Header/Title", $"{snapshot.CharacterName} · 属性");
        SetText("Status/HealthValue", $"{snapshot.CurrentHealth}/{snapshot.MaxHealth}");
        SetText("Status/LevelValue", $"境界 {snapshot.Level}");
        SetText("Status/ExperienceValue", $"{snapshot.Experience}/{snapshot.ExperienceToNext}");
        SetText("Status/TotalExperienceValue", snapshot.TotalExperience.ToString());
        SetText("Combat/DamageValue", snapshot.Damage.ToString());
        SetText("Combat/FireValue", $"{snapshot.FireInterval:0.000} 秒");
        SetText("Combat/MoveValue", $"{snapshot.MoveSpeed:0.0}");
        SetText("Combat/RangeValue", $"{snapshot.TargetRange:0}");
        SetText("Combat/ProjectileValue", $"{snapshot.ProjectileSpeed:0}");
        SetText("Combat/AttractionValue", $"{snapshot.AttractionRange:0}");
        SetText("Sources/PermanentValue", snapshot.PermanentSummary);
        SetText("Sources/BuildValue", snapshot.RunBuildSummary);
        SetText("Sources/SpellValue", FormatSpellSummary(snapshot));
    }

    /// <summary>
    /// 将已悟符卡的原作名、武侠流派和自动施放资源合并为一条可换行属性摘要。
    /// </summary>
    private static string FormatSpellSummary(CharacterStatsSnapshot snapshot)
    {
        if (!snapshot.SpellCards.HasUnlockedCard)
        {
            return $"尚未悟得 · 灵力 {snapshot.SpellCards.CurrentPower}/{snapshot.SpellCards.MaximumPower}";
        }

        string cards = string.Join("、", snapshot.SpellCards.UnlockedCards.Select(
            card => $"{card.FullName}（{card.WuxiaStyle}）"));
        return $"{cards} · 自动施放 · 灵力 " +
            $"{snapshot.SpellCards.CurrentPower}/{snapshot.SpellCards.MaximumPower}";
    }

    /// <summary>
    /// 按相对 Layout 路径写入标签，集中维护属性页固定节点前缀。
    /// </summary>
    private void SetText(string relativePath, string text) =>
        GetNode<Label>($"Root/Panel/Padding/Layout/{relativePath}").Text = text;

    /// <summary>
    /// 同步阻断地图和暂停菜单，确保属性页持有暂停时没有第二个覆盖层抢占输入。
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
    }
}
