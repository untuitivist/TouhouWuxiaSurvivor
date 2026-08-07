using Godot;
using TouhouWuxiaSurvivor.Actors.Player;
using TouhouWuxiaSurvivor.Demo;
using TouhouWuxiaSurvivor.Gameplay.Session;
using TouhouWuxiaSurvivor.Settings;
using TouhouWuxiaSurvivor.Ui.Compendium;
using TouhouWuxiaSurvivor.Ui.Death;
using TouhouWuxiaSurvivor.Ui.Map;
using TouhouWuxiaSurvivor.Ui.Pause;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 在真实场景树中验证暂停菜单作用域、双键输入、地图优先级和暂停状态恢复。
/// </summary>
public partial class PauseMenuSmokeTest : Node
{
    /// <summary>
    /// 执行暂停菜单场景用例，并保证成功或异常都能以明确退出码结束无头测试进程。
    /// </summary>
    public override async void _Ready()
    {
        try
        {
            await RunScenario();
            GD.Print("Pause menu smoke test passed.");
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
    /// 实例化游戏页面并模拟地图、暂停、图鉴和主动放弃，最后确认主菜单不包含暂停层。
    /// </summary>
    private async Task RunScenario()
    {
        var demo = GD.Load<PackedScene>("res://src/demo/WorldDemo.tscn")
            .Instantiate<WorldDemo>();
        demo.PersistMetaProgression = false;
        AddChild(demo);
        var map = demo.GetNode<WorldMapOverlay>("MapLayer/WorldMapOverlay");
        var pause = demo.GetNode<PauseMenuOverlay>("PauseMenuOverlay");
        VerifyDefaultBindings();

        var mapKey = new InputEventKey { PhysicalKeycode = Key.M, Pressed = true };
        var escapeKey = new InputEventKey { PhysicalKeycode = Key.Escape, Pressed = true };
        var secondaryPauseKey = new InputEventKey { PhysicalKeycode = Key.P, Pressed = true };

        map._UnhandledInput(mapKey);
        Require(map.Visible, "M did not open the map.");
        pause._UnhandledInput(escapeKey);
        Require(!map.Visible && !pause.IsOpen,
            "Escape did not close the map before opening pause menu.");

        pause._UnhandledInput(escapeKey);
        Require(pause.IsOpen && GetTree().Paused, "Escape did not pause the game.");
        map._UnhandledInput(mapKey);
        Require(!map.Visible, "Map opened while pause menu was active.");
        pause._UnhandledInput(escapeKey);
        Require(!pause.IsOpen && !GetTree().Paused,
            "Second Escape did not continue the game.");

        pause._UnhandledInput(secondaryPauseKey);
        Require(pause.IsOpen, "Secondary pause key did not open the pause menu.");
        demo.GetNode<Button>("PauseMenuOverlay/Root/PausePanel/Padding/Layout/Compendium")
            .EmitSignal(BaseButton.SignalName.Pressed);
        var compendium = demo.GetNode<CompendiumPanel>(
            "PauseMenuOverlay/Root/CompendiumPanel");
        Require(compendium.Visible && GetTree().Paused,
            "In-game compendium did not preserve the pause state.");
        compendium.GetNode<Button>("Panel/Padding/Layout/Header/Back")
            .EmitSignal(BaseButton.SignalName.Pressed);
        Require(!compendium.Visible && pause.IsOpen,
            "Returning from in-game compendium did not restore pause menu.");
        pause._UnhandledInput(secondaryPauseKey);
        Require(!pause.IsOpen, "Secondary pause key did not close the pause menu.");

        VerifyAbandonmentSettlement(demo, pause, secondaryPauseKey);

        GetTree().Paused = false;
        demo.QueueFree();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Node mainMenu = GD.Load<PackedScene>("res://src/ui/menu/MainMenu.tscn").Instantiate();
        AddChild(mainMenu);
        Require(mainMenu.GetNodeOrNull<PauseMenuOverlay>("PauseMenuOverlay") is null,
            "Pause menu must not exist on the main menu page.");
    }

    /// <summary>
    /// 从暂停菜单确认返回主菜单，验证场景未直接切换且本局以主动结束原因进入失败总结。
    /// </summary>
    private static void VerifyAbandonmentSettlement(
        WorldDemo demo,
        PauseMenuOverlay pause,
        InputEventKey pauseKey)
    {
        pause._UnhandledInput(pauseKey);
        demo.GetNode<Button>("PauseMenuOverlay/Root/PausePanel/Padding/Layout/MainMenu")
            .EmitSignal(BaseButton.SignalName.Pressed);
        string confirmation = demo.GetNode<Label>(
            "PauseMenuOverlay/Root/ConfirmPanel/Padding/Layout/Message").Text;
        Require(confirmation.Contains("失败结算", StringComparison.Ordinal),
            "Pause confirmation did not explain the failed-run settlement.");

        demo.GetNode<Button>("PauseMenuOverlay/Root/ConfirmPanel/Padding/Layout/Buttons/Confirm")
            .EmitSignal(BaseButton.SignalName.Pressed);
        var failure = demo.GetNode<DeathScreenOverlay>("DeathScreenOverlay");
        Require(!pause.IsOpen && demo.GetTree().Paused && failure.IsOpen,
            "Confirmed abandonment did not replace pause with the failure popup.");
        Require(failure.CurrentSummary?.EndReason == RunEndReason.Abandoned,
            "Confirmed abandonment did not settle with the abandoned end reason.");
        Require(failure.GetNode<Label>("Root/DeathPopup/Padding/Layout/Title").Text == "主动结束",
            "Abandonment popup reported the wrong outcome title.");
        RunSummary firstSummary = failure.CurrentSummary!;
        var health = demo.GetNode<PlayerHealth>("Player/Health");
        Require(health.ApplyDamage(health.MaxHealth),
            "Post-abandonment duplicate end signal could not be simulated.");
        Require(ReferenceEquals(firstSummary, failure.CurrentSummary),
            "A duplicate defeat signal replaced the settled abandonment summary.");
        failure.ShowSummary();
        Require(failure.IsShowingSummary,
            "Abandoned run could not enter the standard run-summary page.");
    }

    /// <summary>
    /// 验证地图、属性和调试覆盖层各有一个默认键，其余连续操作提供两个默认物理键。
    /// </summary>
    private static void VerifyDefaultBindings()
    {
        foreach (InputActionDefinition action in InputActionCatalog.All)
        {
            if (action.Id is "toggle_map" or "toggle_stats" or "toggle_debug")
            {
                Key expected = action.Id switch
                {
                    "toggle_map" => Key.M,
                    "toggle_stats" => Key.E,
                    _ => Key.F3,
                };
                Require(action.PrimaryKey == expected && action.SecondaryKey == Key.None,
                    $"Overlay action {action.Id} has an invalid default binding.");
            }
            else
            {
                Require(action.PrimaryKey != Key.None && action.SecondaryKey != Key.None,
                    $"Action {action.Id} must provide two default keys.");
            }
        }

    }

    /// <summary>
    /// 将测试条件失败转换为明确异常，使无头 Godot 返回失败状态。
    /// </summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
