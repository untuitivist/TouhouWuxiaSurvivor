using Godot;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;
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
    private CharacterBuildView? _buildView;
    private CharacterStatsPage _page = CharacterStatsPage.Build;
    private CharacterStatsSnapshot? _snapshot;
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
        GetNode<Button>("Root/Panel/Padding/Layout/Header/StatsTab").Pressed +=
            () => ShowPage(CharacterStatsPage.Stats);
        GetNode<Button>("Root/Panel/Padding/Layout/Header/BuildTab").Pressed +=
            () => ShowPage(CharacterStatsPage.Build);
        _buildView = GetNode<CharacterBuildView>("Root/Panel/Padding/Layout/Pages/BuildPage");
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
        _snapshot = _snapshotProvider();
        Refresh(_snapshot);
        ShowPage(_page);
        _root.Show();
        if (_page == CharacterStatsPage.Build)
        {
            _buildView?.Graph.GrabFocus();
        }
        else
        {
            GetNode<Button>("Root/Panel/Padding/Layout/Header/StatsTab").GrabFocus();
        }
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
        SetText("Header/Title", snapshot.CharacterName);
        SetText("Header/Role", $"定位 {snapshot.CombatRoleName}");
        SetText("Pages/StatsPage/Sources/RoleValue",
            $"{snapshot.CombatRoleName} · {snapshot.CombatRoleDescription}");
        SetText("Pages/StatsPage/Status/HealthValue", $"{snapshot.CurrentHealth}/{snapshot.MaxHealth}");
        SetText("Pages/StatsPage/Status/LevelValue", $"境界 {snapshot.Level}");
        SetText("Pages/StatsPage/Status/ExperienceValue", $"{snapshot.Experience}/{snapshot.ExperienceToNext}");
        SetText("Pages/StatsPage/Status/TotalExperienceValue", snapshot.TotalExperience.ToString());
        SetText("Pages/StatsPage/Combat/DamageValue", snapshot.VolleyTotalDamage.ToString());
        SetText("Pages/StatsPage/Combat/VolleyValue",
            FormatVolleyDamage(snapshot));
        SetText("Pages/StatsPage/Combat/FireValue", $"{snapshot.FireInterval:0.000} 秒");
        SetText("Pages/StatsPage/Combat/MoveValue", $"{snapshot.MoveSpeed:0.0}");
        SetText("Pages/StatsPage/Combat/RangeValue", $"{snapshot.TargetRange:0}");
        SetText("Pages/StatsPage/Combat/ProjectileValue", $"{snapshot.ProjectileSpeed:0}");
        SetText("Pages/StatsPage/Combat/AttractionValue", $"{snapshot.AttractionRange:0}");
        SetText("Pages/StatsPage/Sources/PermanentValue", snapshot.PermanentSummary);
        SetText("Pages/StatsPage/Sources/SpellValue", FormatSpellSummary(snapshot));
        _buildView?.SetModel(snapshot.Build);
    }

    /// <summary>
    /// 把正式齐射压成普通弹与中心弹幕两段，以相同单弹数值展示数量和弹幕阵形。
    /// </summary>
    private static string FormatVolleyDamage(CharacterStatsSnapshot snapshot)
    {
        string barrage = snapshot.BarrageProjectileCount <= 0
            ? "弹幕 未修习"
            : $"弹幕 {snapshot.BarrageProjectileCount}×{snapshot.MinimumProjectileDamage}" +
                $" {snapshot.BarragePatternName}";
        string pierce = snapshot.SecondaryVolleyDamage > 0
            ? $" · 贯穿 +{snapshot.SecondaryVolleyDamage}"
            : string.Empty;
        return $"普通 {snapshot.OrdinaryProjectileCount}×{snapshot.MinimumProjectileDamage} · " +
            $"{barrage}{pierce}";
    }

    /// <summary>
    /// 切换属性与构筑页并同步页签状态；两个页面共享同一打开瞬间的冻结快照。
    /// </summary>
    public void ShowPage(CharacterStatsPage page)
    {
        _page = page;
        bool showStats = page == CharacterStatsPage.Stats;
        GetNode<Control>("Root/Panel/Padding/Layout/Pages/StatsPage").Visible = showStats;
        GetNode<Control>("Root/Panel/Padding/Layout/Pages/BuildPage").Visible = !showStats;
        GetNode<Button>("Root/Panel/Padding/Layout/Header/StatsTab").ButtonPressed = showStats;
        GetNode<Button>("Root/Panel/Padding/Layout/Header/BuildTab").ButtonPressed = !showStats;
        if (IsOpen && !showStats)
        {
            _buildView?.Graph.GrabFocus();
        }
    }

    /// <summary>
    /// 将四主攻二护持的占用量与下一次独立触发合并为紧凑摘要，不重复展示旧资源规则。
    /// </summary>
    private static string FormatSpellSummary(CharacterStatsSnapshot snapshot)
    {
        if (!snapshot.SpellCards.HasUnlockedCard)
        {
            return "主攻 0/4 · 护持 0/2 · 尚未悟得";
        }

        int offensive = snapshot.SpellCards.UnlockedCards.Count(card =>
            SpellCardSlotPolicy.Classify(card) == SpellCardSlotKind.Offensive);
        int support = snapshot.SpellCards.UnlockedCards.Count(card =>
            SpellCardSlotPolicy.Classify(card) == SpellCardSlotKind.Support);
        string state = snapshot.SpellCards.NextCardIsWaitingForCondition
            ? "周天就绪，等待条件"
            : $"{snapshot.SpellCards.NextCastRemaining:0.0}秒";
        return $"主攻 {offensive}/{SpellCardSlotPolicy.MaximumOffensiveSlots} · " +
            $"护持 {support}/{SpellCardSlotPolicy.MaximumSupportSlots} · 自动运转\n" +
            $"下一式 {snapshot.SpellCards.NextCardName} {state} · 随实效属性换算";
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
