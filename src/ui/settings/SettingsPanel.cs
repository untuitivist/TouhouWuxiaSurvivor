using Godot;
using TouhouWuxiaSurvivor.Settings;

namespace TouhouWuxiaSurvivor.Ui.Settings;

/// <summary>
/// 组合音频、按键和视频三个设置页，并向主菜单或暂停菜单发送统一的返回请求。
/// </summary>
public partial class SettingsPanel : PanelContainer
{
    [Signal]
    public delegate void BackRequestedEventHandler();

    private ControlSettingsPanel? _controls;

    public bool IsCapturingKey => _controls?.IsCapturing ?? false;

    /// <summary>
    /// 初始化全局设置、缓存按键页并连接返回按钮。
    /// </summary>
    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        GameSettingsService.Initialize();
        _controls = GetNode<ControlSettingsPanel>("Padding/Layout/Tabs/按键/Controls");
        GetNode<Button>("Padding/Layout/Header/Back").Pressed += RequestBack;
    }

    /// <summary>
    /// 设置面板可见且没有捕获键位时，允许暂停动作作为返回快捷键。
    /// </summary>
    public override void _UnhandledInput(InputEvent inputEvent)
    {
        if (!Visible || IsCapturingKey || !inputEvent.IsActionPressed("pause_menu"))
        {
            return;
        }

        RequestBack();
        GetViewport().SetInputAsHandled();
    }

    /// <summary>
    /// 发出返回信号，由承载设置面板的菜单决定返回目标。
    /// </summary>
    private void RequestBack() => EmitSignal(SignalName.BackRequested);
}
