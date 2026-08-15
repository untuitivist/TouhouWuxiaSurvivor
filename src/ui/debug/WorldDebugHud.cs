using Godot;
using TouhouWuxiaSurvivor.Ui.Hud;
using TouhouWuxiaSurvivor.Ui.Hud.SpellCards;
using TouhouWuxiaSurvivor.Gameplay.Pacing;

namespace TouhouWuxiaSurvivor.Ui.Debug;

/// <summary>
/// 协调底部常驻状态栏和默认隐藏的 F3 世界调试文字层。
/// </summary>
public partial class WorldDebugHud : CanvasLayer
{
    private Label? _statusLabel;
    private Label? _debugLabel;
    private Control? _debugOverlay;
    private ProgressBar? _healthBar;
    private Label? _healthValue;
    private Label? _levelValue;
    private ProgressBar? _experienceBar;
    private Label? _experienceValue;
    private Label? _phaseName;
    private Label? _phaseRemaining;
    private Label? _phaseNotice;
    private RunPacingBar? _pacingBar;
    private SpellCardHudStrip? _spellCards;
    private double _statusAccumulator;
    private double _debugAccumulator;
    private double _phaseNoticeLeft;
    private RunPhaseId? _lastPhase;

    public bool IsDebugVisible => _debugOverlay?.Visible == true;
    public string StatusText => _statusLabel?.Text ?? string.Empty;
    public string DebugText => _debugLabel?.Text ?? string.Empty;
    public string PhaseText => _phaseName?.Text ?? string.Empty;
    public double PacingProgress => _pacingBar?.ProgressRatio ?? 0.0;
    public bool IsPhaseNoticeVisible => _phaseNotice?.Visible == true;
    public int SpellCardIconCount => _spellCards?.VisibleIconCount ?? 0;

    /// <summary>
    /// 缓存两类文字节点、启用暂停时输入，并确保进入游戏时调试层默认关闭。
    /// </summary>
    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        _statusLabel = GetNode<Label>("StatusMargin/Panel/Padding/Layout/Status");
        _healthBar = GetNode<ProgressBar>("StatusMargin/Panel/Padding/Layout/HealthBar");
        _healthValue = GetNode<Label>("StatusMargin/Panel/Padding/Layout/HealthValue");
        _levelValue = GetNode<Label>("StatusMargin/Panel/Padding/Layout/LevelValue");
        _experienceBar = GetNode<ProgressBar>("StatusMargin/Panel/Padding/Layout/ExperienceBar");
        _experienceValue = GetNode<Label>("StatusMargin/Panel/Padding/Layout/ExperienceValue");
        _phaseName = GetNode<Label>("PacingMargin/Panel/Padding/Layout/PhaseName");
        _phaseRemaining = GetNode<Label>("PacingMargin/Panel/Padding/Layout/Remaining");
        _phaseNotice = GetNode<Label>("PhaseNotice");
        _pacingBar = GetNode<RunPacingBar>("PacingMargin/Panel/Padding/Layout/PacingBar");
        _spellCards = GetNode<SpellCardHudStrip>("SpellCards");
        _debugOverlay = GetNode<Control>("DebugMargin");
        _debugLabel = GetNode<Label>("DebugMargin/Label");
        _debugOverlay.Hide();
        SetProcessUnhandledInput(true);
    }

    /// <summary>
    /// 仅处理可重绑定的调试开关动作，并切换左上角调试文字可见状态。
    /// </summary>
    public override void _UnhandledInput(InputEvent inputEvent)
    {
        if (!inputEvent.IsActionPressed("toggle_debug") || _debugOverlay is null)
        {
            return;
        }

        _debugOverlay.Visible = !_debugOverlay.Visible;
        GetViewport().SetInputAsHandled();
    }

    /// <summary>
    /// 使用当前世界状态一次性重建调试文本；节点尚未就绪时安全忽略调用。
    /// </summary>
    public void Refresh(WorldHudSnapshot snapshot, double deltaSeconds = 1.0 / 60.0)
    {
        if (_statusLabel is null || _debugLabel is null || _healthBar is null ||
            _healthValue is null || _levelValue is null || _experienceBar is null ||
            _experienceValue is null || _phaseName is null || _phaseRemaining is null ||
            _phaseNotice is null || _pacingBar is null)
        {
            return;
        }

        _statusAccumulator += Math.Max(0.0, deltaSeconds);
        _debugAccumulator += Math.Max(0.0, deltaSeconds);
        if (_statusAccumulator >= 0.1 || string.IsNullOrEmpty(_statusLabel.Text))
        {
            _statusAccumulator = 0.0;
            _statusLabel.Text = WorldHudTextFormatter.FormatStatus(snapshot);
        }

        if (_debugOverlay?.Visible == true &&
            (_debugAccumulator >= 0.5 || string.IsNullOrEmpty(_debugLabel.Text)))
        {
            _debugAccumulator = 0.0;
            _debugLabel.Text = WorldHudTextFormatter.FormatDebug(snapshot);
        }

        _healthBar.MaxValue = snapshot.MaxHealth;
        _healthBar.Value = snapshot.CurrentHealth;
        _healthValue.Text = $"{snapshot.CurrentHealth}/{snapshot.MaxHealth}";
        _levelValue.Text = $"境界 {snapshot.Level}";
        _experienceBar.MaxValue = snapshot.ExperienceToNext;
        _experienceBar.Value = snapshot.Experience;
        _experienceValue.Text = $"{snapshot.Experience}/{snapshot.ExperienceToNext}";
        _spellCards?.SetSnapshot(snapshot.SpellCards);
        RefreshPacing(snapshot.Pacing, deltaSeconds);
    }

    /// <summary>
    /// 刷新顶部进度带，并在阶段真正变化时短暂显示不遮挡战斗中心的文字提示。
    /// </summary>
    private void RefreshPacing(RunPacingSnapshot pacing, double deltaSeconds)
    {
        _phaseName!.Text = pacing.PhaseName;
        _phaseRemaining!.Text = pacing.IsEndless
            ? "无尽"
            : pacing.IsFinalEncounter
                ? "决战"
                : pacing.CanAdvanceByDominance
                    ? $"压制 {pacing.DominanceProgress:P0}"
                    : $"蓄势 {FormatRemaining(pacing.SecondsToNextPhase)}";
        _pacingBar!.SetSnapshot(pacing);
        if (_lastPhase != pacing.PhaseId)
        {
            _lastPhase = pacing.PhaseId;
            _phaseNoticeLeft = 2.4;
            _phaseNotice!.Text = $"{pacing.PhaseName} · {pacing.CueText}";
            _phaseNotice.Show();
        }

        _phaseNoticeLeft = Math.Max(0.0, _phaseNoticeLeft - Math.Max(0.0, deltaSeconds));
        if (_phaseNoticeLeft <= 0.0)
        {
            _phaseNotice!.Hide();
        }
    }

    /// <summary>把下一阶段倒计时格式化为固定宽度分秒，避免数值变化引起HUD横向跳动。</summary>
    private static string FormatRemaining(double seconds)
    {
        int total = Math.Max(0, (int)Math.Ceiling(seconds));
        return $"{total / 60:00}:{total % 60:00}";
    }
}
