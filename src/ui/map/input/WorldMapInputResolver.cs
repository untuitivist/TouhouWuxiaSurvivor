using Godot;

namespace TouhouWuxiaSurvivor.Ui.Map.Input;

/// <summary>
/// 把可重绑定的 Godot 动作转换为旅行地图命令，不持有控件、暂停状态或探索数据。
/// </summary>
public static class WorldMapInputResolver
{
    /// <summary>
    /// 地图关闭时只响应开关动作；地图打开后再解析关闭、回中和四向平移，并按固定优先级返回。
    /// </summary>
    public static WorldMapInputCommand Resolve(InputEvent inputEvent, bool mapVisible)
    {
        if (inputEvent.IsActionPressed("toggle_map"))
        {
            return WorldMapInputCommand.Toggle;
        }

        if (!mapVisible)
        {
            return WorldMapInputCommand.None;
        }

        if (inputEvent.IsActionPressed("pause_menu")) return WorldMapInputCommand.Close;
        if (inputEvent.IsActionPressed("map_recenter")) return WorldMapInputCommand.Recenter;
        if (inputEvent.IsActionPressed("ui_left")) return WorldMapInputCommand.PanLeft;
        if (inputEvent.IsActionPressed("ui_right")) return WorldMapInputCommand.PanRight;
        if (inputEvent.IsActionPressed("ui_up")) return WorldMapInputCommand.PanUp;
        if (inputEvent.IsActionPressed("ui_down")) return WorldMapInputCommand.PanDown;
        return WorldMapInputCommand.None;
    }
}
