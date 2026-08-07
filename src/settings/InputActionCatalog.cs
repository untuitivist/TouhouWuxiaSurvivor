using Godot;

namespace TouhouWuxiaSurvivor.Settings;

/// <summary>
/// 集中声明所有可重绑定操作，确保项目输入配置和设置界面使用完全相同的动作集合。
/// </summary>
public static class InputActionCatalog
{
    /// <summary>
    /// 返回按设置界面显示顺序排列的操作定义；覆盖层与调试开关的第二槽默认未绑定。
    /// </summary>
    public static IReadOnlyList<InputActionDefinition> All { get; } =
    [
        new("move_up", "向上移动", Key.W, Key.Up),
        new("move_down", "向下移动", Key.S, Key.Down),
        new("move_left", "向左移动", Key.A, Key.Left),
        new("move_right", "向右移动", Key.D, Key.Right),
        new("toggle_map", "打开地图", Key.M, Key.None),
        new("toggle_stats", "角色属性", Key.E, Key.None),
        new("toggle_debug", "调试信息", Key.F3, Key.None),
        new("map_recenter", "地图回到玩家", Key.F, Key.Home),
        new("pause_menu", "暂停菜单", Key.Escape, Key.P),
    ];
}
