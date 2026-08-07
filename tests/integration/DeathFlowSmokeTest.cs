using Godot;
using TouhouWuxiaSurvivor.Actors.Player;
using TouhouWuxiaSurvivor.Gameplay.Session;
using TouhouWuxiaSurvivor.Ui.Death;
using TouhouWuxiaSurvivor.Demo;
using TouhouWuxiaSurvivor.Ui.Stats;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 在真实游戏场景中验证生命归零、死亡弹窗、总结页和两个离场命令的完整状态流。
/// </summary>
public partial class DeathFlowSmokeTest : Node
{
    /// <summary>
    /// 杀死玩家并依次操作死亡界面，确认暂停、统计快照和导航命令都只在正确阶段生效。
    /// </summary>
    public override async void _Ready()
    {
        try
        {
            var demo = GD.Load<PackedScene>("res://src/demo/WorldDemo.tscn")
                .Instantiate<WorldDemo>();
            demo.PersistMetaProgression = false;
            AddChild(demo);
            var health = demo.GetNode<PlayerHealth>("Player/Health");
            var death = demo.GetNode<DeathScreenOverlay>("DeathScreenOverlay");
            var stats = demo.GetNode<CharacterStatsOverlay>("CharacterStatsOverlay");

            Require(health.ApplyDamage(health.MaxHealth), "Lethal damage was rejected.");
            Require(health.IsDead, "Player did not enter the dead state.");
            Require(GetTree().Paused && death.IsOpen && !death.IsShowingSummary,
                "Death did not open the popup and pause gameplay.");
            Require(death.CurrentSummary?.EndReason == RunEndReason.Defeated,
                "Lethal damage did not record the defeated end reason.");
            Require(death.GetNode<Label>("Root/DeathPopup/Padding/Layout/Title").Text == "符力耗尽",
                "Defeat popup did not retain the canonical outcome title.");
            Require(stats.InputBlocked && !stats.IsOpen,
                "Death did not block and dismiss the character stats overlay.");

            death.ShowSummary();
            Require(death.IsShowingSummary, "Death popup did not enter the run summary page.");
            string survival = death.GetNode<Label>(
                "Root/SummaryPanel/Padding/Layout/StatsColumns/LeftStats/SurvivalValue").Text;
            Require(!string.IsNullOrWhiteSpace(survival), "Run summary omitted survival time.");
            string finalLevel = death.GetNode<Label>(
                "Root/SummaryPanel/Padding/Layout/StatsColumns/LeftStats/LevelValue").Text;
            string build = death.GetNode<Label>(
                "Root/SummaryPanel/Padding/Layout/BuildRow/BuildValue").Text;
            string reward = death.GetNode<Label>(
                "Root/SummaryPanel/Padding/Layout/StatsColumns/LeftStats/RewardValue").Text;
            Require(finalLevel == "1" && build == "尚未修习",
                "Run summary omitted the initial progression state.");
            Require(reward == "+0", "Immediate death should not award money.");

            GetTree().Paused = false;
            demo.QueueFree();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await VerifyNavigationCommands();
            GD.Print("Death flow smoke test passed.");
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
    /// 独立实例化死亡界面并按下两个离场按钮，验证 UI 通过事件请求导航而不直接耦合游戏场景。
    /// </summary>
    private async Task VerifyNavigationCommands()
    {
        var scene = GD.Load<PackedScene>("res://src/ui/death/DeathScreenOverlay.tscn");
        var death = scene.Instantiate<DeathScreenOverlay>();
        AddChild(death);
        bool restartRequested = false;
        bool mainMenuRequested = false;
        death.RestartRequested += () => restartRequested = true;
        death.MainMenuRequested += () => mainMenuRequested = true;

        death.GetNode<Button>(
            "Root/SummaryPanel/Padding/Layout/Buttons/Restart").EmitSignal(Button.SignalName.Pressed);
        death.GetNode<Button>(
            "Root/SummaryPanel/Padding/Layout/Buttons/MainMenu").EmitSignal(Button.SignalName.Pressed);
        Require(restartRequested && mainMenuRequested,
            "Death screen did not publish both navigation commands.");
        GetTree().Paused = false;
        death.QueueFree();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    /// <summary>
    /// 将死亡流程中的失败条件转换为带有明确原因的测试异常。
    /// </summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
