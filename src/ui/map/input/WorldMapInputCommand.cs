namespace TouhouWuxiaSurvivor.Ui.Map.Input;

/// <summary>
/// 描述旅行地图能够执行的离散键盘命令，使动作绑定解析与地图渲染状态相互独立。
/// </summary>
public enum WorldMapInputCommand
{
    None,
    Toggle,
    Close,
    Recenter,
    PanLeft,
    PanRight,
    PanUp,
    PanDown,
}
