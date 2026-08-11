using Godot;
using TouhouWuxiaSurvivor.Tests.Support;
using TouhouWuxiaSurvivor.Ui.Map;
using TouhouWuxiaSurvivor.Ui.Map.Input;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 在真实 Godot 场景树中验证地图打开、暂停、Tile 纹理、缩放和关闭恢复行为。
/// </summary>
public partial class WorldMapSmokeTest : Node
{
    /// <summary>
    /// 实例化主场景并模拟 M 键与滚轮输入；任一地图契约不满足都会让测试进程失败。
    /// </summary>
    public override async void _Ready()
    {
        PackedScene demoScene = GD.Load<PackedScene>("res://src/demo/WorldDemo.tscn");
        Node demo = demoScene.Instantiate();
        AddChild(demo);

        var map = demo.GetNode<WorldMapOverlay>("MapLayer/WorldMapOverlay");
        Require(InputMap.HasAction("toggle_map"), "Map input action is missing.");
        VerifyCommandRouting();
        using var mapKey = new InputEventKey
        {
            PhysicalKeycode = Key.M,
            Pressed = true
        };

        map._UnhandledInput(mapKey);
        Require(map.Visible, "Map did not open.");
        Require(GetTree().Paused, "Opening the map did not pause the world.");
        Require(map.HasRenderedTexture, "Map did not render explored tiles.");
        using var mouseMotion = new InputEventMouseMotion { Position = map.Size * 0.5f };
        map._GuiInput(mouseMotion);
        Require(map.VisibleBiomeLabelCount == 1, "Map did not create hovered biome label.");
        Require(map.VisibleStructureLabelCount > 0, "Map did not create structure labels.");

        float previousZoom = map.PixelsPerTile;
        using var mouseWheel = new InputEventMouseButton
        {
            ButtonIndex = MouseButton.WheelUp,
            Pressed = true
        };
        map._GuiInput(mouseWheel);
        Require(map.PixelsPerTile > previousZoom, "Mouse wheel did not zoom the map.");

        map._UnhandledInput(mapKey);
        Require(!map.Visible, "Map did not close.");
        Require(!GetTree().Paused, "Closing the map did not restore pause state.");

        GD.Print("World map smoke test passed.");
        await WorldDemoTestCleanup.FreeAsync(this, demo);
        GetTree().Quit();
    }

    /// <summary>
    /// 覆盖地图关闭态屏蔽与打开态全部键盘命令，保证抽离后的动作解析没有改变既有控制语义。
    /// </summary>
    private static void VerifyCommandRouting()
    {
        using var pauseWhenClosed = new InputEventAction { Action = "pause_menu", Pressed = true };
        Require(WorldMapInputResolver.Resolve(pauseWhenClosed, false) == WorldMapInputCommand.None,
            "Closed map accepted a command other than toggle.");
        (string action, WorldMapInputCommand command)[] expected =
        [
            ("toggle_map", WorldMapInputCommand.Toggle),
            ("pause_menu", WorldMapInputCommand.Close),
            ("map_recenter", WorldMapInputCommand.Recenter),
            ("ui_left", WorldMapInputCommand.PanLeft),
            ("ui_right", WorldMapInputCommand.PanRight),
            ("ui_up", WorldMapInputCommand.PanUp),
            ("ui_down", WorldMapInputCommand.PanDown),
        ];
        foreach ((string action, WorldMapInputCommand command) in expected)
        {
            using var input = new InputEventAction { Action = action, Pressed = true };
            Require(WorldMapInputResolver.Resolve(input, true) == command,
                $"Map action {action} did not resolve to {command}.");
        }
    }

    /// <summary>
    /// 将布尔断言转换为带有明确失败原因的异常，供无头 Godot 返回非零结果。
    /// </summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
