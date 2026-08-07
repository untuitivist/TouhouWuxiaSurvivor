using Godot;
using TouhouWuxiaSurvivor.Ui.Hud;

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

    public bool IsDebugVisible => _debugOverlay?.Visible == true;
    public string StatusText => _statusLabel?.Text ?? string.Empty;
    public string DebugText => _debugLabel?.Text ?? string.Empty;

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
    public void Refresh(WorldHudSnapshot snapshot)
    {
        if (_statusLabel is null || _debugLabel is null || _healthBar is null ||
            _healthValue is null || _levelValue is null || _experienceBar is null ||
            _experienceValue is null)
        {
            return;
        }

        _statusLabel.Text = WorldHudTextFormatter.FormatStatus(snapshot);
        _debugLabel.Text = WorldHudTextFormatter.FormatDebug(snapshot);
        _healthBar.MaxValue = snapshot.MaxHealth;
        _healthBar.Value = snapshot.CurrentHealth;
        _healthValue.Text = $"{snapshot.CurrentHealth}/{snapshot.MaxHealth}";
        _levelValue.Text = $"境界 {snapshot.Level}";
        _experienceBar.MaxValue = snapshot.ExperienceToNext;
        _experienceBar.Value = snapshot.Experience;
        _experienceValue.Text = $"{snapshot.Experience}/{snapshot.ExperienceToNext}";
    }
}
