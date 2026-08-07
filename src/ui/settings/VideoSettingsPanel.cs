using Godot;
using TouhouWuxiaSurvivor.Settings;

namespace TouhouWuxiaSurvivor.Ui.Settings;

/// <summary>
/// 管理窗口模式、分辨率、垂直同步和帧率上限，并将每项修改立即应用到 DisplayServer。
/// </summary>
public partial class VideoSettingsPanel : VBoxContainer
{
    private static readonly Vector2I[] Resolutions =
    [
        new(640, 360),
        new(960, 540),
        new(1280, 720),
        new(1600, 900),
        new(1920, 1080),
        new(2560, 1440),
        new(3840, 2160),
    ];

    private static readonly int[] FpsLimits = [30, 60, 120, 144, 0];
    private OptionButton? _windowMode;
    private OptionButton? _resolution;
    private OptionButton? _fpsLimit;
    private CheckButton? _vsync;

    /// <summary>
    /// 填充选项列表、选择当前值，再连接变化信号以避免初始化时重复应用设置。
    /// </summary>
    public override void _Ready()
    {
        GameSettingsService.Initialize();
        _windowMode = GetNode<OptionButton>("WindowMode/Option");
        _resolution = GetNode<OptionButton>("Resolution/Option");
        _fpsLimit = GetNode<OptionButton>("FpsLimit/Option");
        _vsync = GetNode<CheckButton>("Vsync/Toggle");

        FillOptions();
        SelectCurrentValues();
        UpdateResolutionAvailability();
        _windowMode.ItemSelected += OnWindowModeSelected;
        _resolution.ItemSelected += OnResolutionSelected;
        _fpsLimit.ItemSelected += OnFpsLimitSelected;
        _vsync.Toggled += OnVsyncToggled;
    }

    /// <summary>
    /// 为窗口、分辨率和帧率控件添加稳定顺序的可选项。
    /// </summary>
    private void FillOptions()
    {
        _windowMode!.AddItem("窗口化");
        _windowMode.AddItem("无边框窗口");
        _windowMode.AddItem("全屏");
        foreach (Vector2I resolution in Resolutions)
        {
            _resolution!.AddItem($"{resolution.X} × {resolution.Y}");
        }

        foreach (int fps in FpsLimits)
        {
            _fpsLimit!.AddItem(fps == 0 ? "不限制" : $"{fps} FPS");
        }
    }

    /// <summary>
    /// 将持久化设置映射回各 OptionButton 的索引和 VSync 开关状态。
    /// </summary>
    private void SelectCurrentValues()
    {
        GameSettingsData settings = GameSettingsService.Current;
        _windowMode!.Select(Math.Clamp(settings.WindowMode, 0, 2));
        int resolutionIndex = Array.FindIndex(Resolutions, size =>
            size.X == settings.ResolutionWidth && size.Y == settings.ResolutionHeight);
        _resolution!.Select(Math.Max(0, resolutionIndex));
        int fpsIndex = Array.IndexOf(FpsLimits, settings.MaxFps);
        _fpsLimit!.Select(Math.Max(0, fpsIndex));
        _vsync!.ButtonPressed = settings.VsyncEnabled;
    }

    /// <summary>
    /// 更新窗口模式索引并应用所有视频设置。
    /// </summary>
    private void OnWindowModeSelected(long index)
    {
        GameSettingsService.Current.WindowMode = (int)index;
        UpdateResolutionAvailability();
        ApplyAndSave();
    }

    /// <summary>
    /// 更新窗口化分辨率；全屏与无边框模式仍会保留该值供下次窗口化使用。
    /// </summary>
    private void OnResolutionSelected(long index)
    {
        Vector2I size = Resolutions[(int)index];
        GameSettingsService.Current.ResolutionWidth = size.X;
        GameSettingsService.Current.ResolutionHeight = size.Y;
        ApplyAndSave();
    }

    /// <summary>
    /// 更新帧率上限，零表示不限制。
    /// </summary>
    private void OnFpsLimitSelected(long index)
    {
        GameSettingsService.Current.MaxFps = FpsLimits[(int)index];
        ApplyAndSave();
    }

    /// <summary>
    /// 更新垂直同步开关并立即交给 DisplayServer 应用。
    /// </summary>
    private void OnVsyncToggled(bool enabled)
    {
        GameSettingsService.Current.VsyncEnabled = enabled;
        ApplyAndSave();
    }

    /// <summary>
    /// 仅在窗口化模式开放尺寸选择；无边框与全屏始终采用当前显示器尺寸。
    /// </summary>
    private void UpdateResolutionAvailability()
    {
        if (_resolution is not null)
        {
            _resolution.Disabled = GameSettingsService.Current.WindowMode != 0;
        }
    }

    /// <summary>
    /// 统一应用并保存视频设置，确保界面状态与运行时状态同步。
    /// </summary>
    private static void ApplyAndSave()
    {
        GameSettingsService.ApplyVideo();
        GameSettingsService.Save();
    }
}
