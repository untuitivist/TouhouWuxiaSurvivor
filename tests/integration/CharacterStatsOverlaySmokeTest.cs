using Godot;
using TouhouWuxiaSurvivor.Demo;
using TouhouWuxiaSurvivor.Settings;
using TouhouWuxiaSurvivor.Tests.Support;
using TouhouWuxiaSurvivor.Ui.Map;
using TouhouWuxiaSurvivor.Ui.Pause;
using TouhouWuxiaSurvivor.Ui.Stats;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 在真实无限世界中验证 E 属性页数值、暂停所有权以及与 M 地图和 ESC 菜单的互斥切换。
/// </summary>
public partial class CharacterStatsOverlaySmokeTest : Node
{
    /// <summary>
    /// 依次操作 E、M、ESC 相关状态并确认属性页始终是无滚动的单一暂停覆盖层。
    /// </summary>
    public override async void _Ready()
    {
        try
        {
            var demo = GD.Load<PackedScene>("res://src/demo/WorldDemo.tscn")
                .Instantiate<WorldDemo>();
            demo.PersistMetaProgression = false;
            AddChild(demo);
            var stats = demo.GetNode<CharacterStatsOverlay>("CharacterStatsOverlay");
            var map = demo.GetNode<WorldMapOverlay>("MapLayer/WorldMapOverlay");
            var pause = demo.GetNode<PauseMenuOverlay>("PauseMenuOverlay");
            VerifyDefaultBinding();

            using var toggleStats = new InputEventAction
            {
                Action = "toggle_stats",
                Pressed = true,
            };
            stats._UnhandledInput(toggleStats);
            Require(stats.IsOpen && GetTree().Paused &&
                map.InputBlocked && pause.InputBlocked,
                "E did not open one pause-owning stats overlay.");
            Require(stats.FindChildren("*", "ScrollContainer").Count == 0,
                "Character stats must fit without a scroll container.");
            Require(stats.GetNode<Label>(
                "Root/Panel/Padding/Layout/Header/Title").Text.Contains(
                "博丽灵梦", StringComparison.Ordinal),
                "Stats panel omitted the active character identity.");
            Require(stats.GetNode<Label>(
                "Root/Panel/Padding/Layout/Combat/DamageValue").Text == "1",
                "Stats panel did not display effective opening damage.");

            stats._UnhandledInput(toggleStats);
            Require(!stats.IsOpen && !GetTree().Paused &&
                !map.InputBlocked && !pause.InputBlocked,
                "Second E did not close stats and restore gameplay.");

            map.Open();
            stats._UnhandledInput(toggleStats);
            Require(stats.IsOpen && !map.Visible,
                "E did not switch directly from map to character stats.");
            using var toggleMap = new InputEventAction
            {
                Action = "toggle_map",
                Pressed = true,
            };
            stats._UnhandledInput(toggleMap);
            Require(!stats.IsOpen && map.Visible && GetTree().Paused,
                "M did not switch directly from character stats to map.");
            map.Close();

            pause.Open();
            stats._UnhandledInput(toggleStats);
            Require(!stats.IsOpen && pause.IsOpen,
                "E must not cover an active pause menu.");
            pause.Close();

            GD.Print("Character stats overlay smoke test passed.");
            await WorldDemoTestCleanup.FreeAsync(this, demo);
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GetTree().Paused = false;
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    /// <summary>
    /// 确认属性动作默认使用 E 且保留一个可在设置中配置的空闲第二槽。
    /// </summary>
    private static void VerifyDefaultBinding()
    {
        InputActionDefinition definition = InputActionCatalog.All.Single(
            action => action.Id == "toggle_stats");
        Require(definition.PrimaryKey == Key.E && definition.SecondaryKey == Key.None,
            "Stats action must default to E plus an unbound second slot.");
    }

    /// <summary>
    /// 将属性页契约失败转换为包含具体原因的测试异常。
    /// </summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
