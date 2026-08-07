using Godot;
using TouhouWuxiaSurvivor.Gameplay.Meta.Persistence;
using TouhouWuxiaSurvivor.Gameplay.Meta.Runtime;
using TouhouWuxiaSurvivor.Ui.Meta;
using TouhouWuxiaSurvivor.Ui.Menu;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 验证博丽神社整备页无滚动展示、钱财购买、原著命名、解锁和二次确认重置流程。
/// </summary>
public partial class CultivationPanelSmokeTest : Node
{
    /// <summary>
    /// 以进程内档案驱动真实面板，检查四行排版、购买扣钱、锁定和重置状态。
    /// </summary>
    public override async void _Ready()
    {
        try
        {
            var initial = ProgressionProfileData.CreateDefault();
            initial.Money = 50;
            initial.LifetimeMoney = 50;
            var manager = new ProgressionProfileManager(
                new VolatileProgressionProfileStore(initial));
            var panel = GD.Load<PackedScene>("res://src/ui/meta/CultivationPanel.tscn")
                .Instantiate<CultivationPanel>();
            AddChild(panel);
            panel.Configure(manager);
            panel.Present();

            Require(panel.IsOpen && panel.FindChildren("*", "ScrollContainer").Count == 0,
                "Shrine preparation must be a visible non-scrolling single page.");
            Require(panel.GetNode<Label>("Panel/Padding/Layout/Header/Title").Text ==
                "博丽神社整备", "Panel title is not tied to Reimu's canonical home.");
            Require(panel.GetNode<Button>("Panel/Padding/Layout/Choices/Choice0").Text.Contains(
                "博丽护身结界", StringComparison.Ordinal),
                "Hakurei barrier preparation is missing.");
            Require(panel.GetNode<Button>("Panel/Padding/Layout/Choices/Choice3").Disabled,
                "Persuasion needle tuning must remain locked below one hundred lifetime money.");

            panel.PurchaseAt(0);
            Require(manager.Current.Money == 34 &&
                manager.Current.GetRank("hakurei_barrier") == 1 &&
                panel.StatusText.Contains("1 重", StringComparison.Ordinal),
                "Barrier purchase did not persist its cost and rank.");
            panel.RequestReset();
            Require(manager.Current.Money == 34 && panel.StatusText.Contains(
                "再次点击", StringComparison.Ordinal),
                "First reset click must only arm confirmation.");
            panel.RequestReset();
            Require(manager.Current.Money == 0 &&
                manager.Current.GetRank("hakurei_barrier") == 0,
                "Confirmed reset did not restore the default profile.");

            GD.Print("Cultivation panel smoke test passed.");
            panel.QueueFree();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await VerifyMainMenuEntry();
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    /// <summary>
    /// 实例化真实主菜单并操作神社整备与返回按钮，验证单活动面板切换链路完整。
    /// </summary>
    private async Task VerifyMainMenuEntry()
    {
        var mainMenu = GD.Load<PackedScene>("res://src/ui/menu/MainMenu.tscn")
            .Instantiate<MainMenu>();
        AddChild(mainMenu);
        mainMenu.GetNode<Button>("Menu/Panel/Padding/Layout/Cultivation")
            .EmitSignal(Button.SignalName.Pressed);
        var panel = mainMenu.GetNode<CultivationPanel>("CultivationPanel");
        Require(panel.IsOpen && !mainMenu.GetNode<Control>("Menu").Visible,
            "Main menu did not switch to shrine preparation.");
        panel.GetNode<Button>("Panel/Padding/Layout/Header/Back")
            .EmitSignal(Button.SignalName.Pressed);
        Require(!panel.IsOpen && mainMenu.GetNode<Control>("Menu").Visible,
            "Returning from shrine preparation did not restore main-menu commands.");
        mainMenu.QueueFree();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    /// <summary>
    /// 将神社整备界面契约失败转换为包含具体原因的测试异常。
    /// </summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
