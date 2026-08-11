using Godot;
using TouhouWuxiaSurvivor.Actors.Player;
using TouhouWuxiaSurvivor.Gameplay.Progression.Runtime;
using TouhouWuxiaSurvivor.Gameplay.Spawning;
using TouhouWuxiaSurvivor.Tests.Support;
using TouhouWuxiaSurvivor.Ui.Debug;
using TouhouWuxiaSurvivor.Ui.Map;
using TouhouWuxiaSurvivor.Ui.Pause;
using TouhouWuxiaSurvivor.Ui.Progression;
using TouhouWuxiaSurvivor.Ui.Stats;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 在真实无限世界中验证灵息拾取、升级暂停、构筑应用和紧凑 HUD 的完整局内成长闭环。
/// </summary>
public partial class RunProgressionSmokeTest : Node
{
    /// <summary>
    /// 生成刚好足够升级的灵息，执行首个选项，并检查状态、倍率及互斥界面是否一致恢复。
    /// </summary>
    public override async void _Ready()
    {
        try
        {
            Node demo = GD.Load<PackedScene>("res://src/demo/WorldDemo.tscn").Instantiate();
            AddChild(demo);
            var player = demo.GetNode<PlayerController>("Player");
            var spawner = demo.GetNode<SpiritDropSpawner>("SpiritDropSpawner");
            var progression = demo.GetNode<RunProgressionCoordinator>("RunProgressionCoordinator");
            var overlay = demo.GetNode<LevelUpOverlay>("LevelUpOverlay");
            var map = demo.GetNode<WorldMapOverlay>("MapLayer/WorldMapOverlay");
            var pause = demo.GetNode<PauseMenuOverlay>("PauseMenuOverlay");
            var hud = demo.GetNode<WorldDebugHud>("WorldDebugHud");
            var stats = demo.GetNode<CharacterStatsOverlay>("CharacterStatsOverlay");

            spawner.Spawn(player.GlobalPosition, 8);
            await WaitForLevel(progression, 2);
            Require(progression.State.Level == 2 && progression.State.Experience == 0,
                "Collecting eight spirit did not reach level two exactly.");
            Require(progression.State.PendingChoices == 1 && overlay.IsOpen &&
                overlay.ChoiceCount == 3 && GetTree().Paused,
                "Level-up did not open one mandatory three-choice modal.");
            Require(map.InputBlocked && pause.InputBlocked,
                "Level-up did not block map and pause-menu input.");
            Require(stats.InputBlocked,
                "Level-up did not block the character stats overlay.");

            overlay.SelectChoice(0);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Require(!overlay.IsOpen && !GetTree().Paused &&
                progression.State.PendingChoices == 0,
                "Choosing an upgrade did not close the modal and restore gameplay.");
            Require(!map.InputBlocked && !pause.InputBlocked,
                "Choosing an upgrade did not restore map and pause-menu input.");
            Require(!stats.InputBlocked,
                "Choosing an upgrade did not restore character stats input.");
            Require(progression.Build.TotalRanks == 1 && HasAppliedModifier(progression.Modifiers),
                "Chosen upgrade did not update both build state and runtime modifiers.");
            Require(hud.GetNode<Label>(
                "StatusMargin/Panel/Padding/Layout/LevelValue").Text == "境界 2",
                "Compact HUD did not refresh to the new level.");

            GD.Print("Run progression smoke test passed.");
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
    /// 判断六项运行时投影中是否至少一项离开初始基线，证明选择已被实际消费方读取。
    /// </summary>
    private static bool HasAppliedModifier(RunModifierState modifiers) =>
        modifiers.DamageBonus > 0 ||
        modifiers.FireRateMultiplier > 1.0f ||
        modifiers.MoveSpeedMultiplier > 1.0f ||
        modifiers.TargetRangeMultiplier > 1.0f ||
        modifiers.ProjectileSpeedMultiplier > 1.0f ||
        modifiers.SpiritAttractionMultiplier > 1.0f;

    /// <summary>
    /// 最多等待四个处理帧让新加入的灵息节点完成首次处理，避免信号恢复顺序造成过早断言。
    /// </summary>
    private async Task WaitForLevel(RunProgressionCoordinator progression, int expectedLevel)
    {
        for (int frame = 0; frame < 4 && progression.State.Level < expectedLevel; frame++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
    }

    /// <summary>
    /// 将成长闭环的契约失败转换为包含明确原因的测试异常。
    /// </summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
