using Godot;
using TouhouWuxiaSurvivor.Actors.Player;
using TouhouWuxiaSurvivor.Settings;
using TouhouWuxiaSurvivor.Tests.Support;
using TouhouWuxiaSurvivor.Ui.Debug;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 验证游戏默认只显示窄状态栏，并能通过可重绑定的 F3 动作切换详细调试文字。
/// </summary>
public partial class WorldHudSmokeTest : Node
{
    /// <summary>
    /// 实例化真实游戏页，检查五格血条、受伤无敌、默认可见性、F3 默认键和调试层双向切换。
    /// </summary>
    public override async void _Ready()
    {
        try
        {
            Node demo = GD.Load<PackedScene>("res://src/demo/WorldDemo.tscn").Instantiate();
            AddChild(demo);
            var hud = demo.GetNode<WorldDebugHud>("WorldDebugHud");
            var status = hud.GetNode<Control>("StatusMargin");
            var healthBar = hud.GetNode<ProgressBar>(
                "StatusMargin/Panel/Padding/Layout/HealthBar");
            var healthValue = hud.GetNode<Label>(
                "StatusMargin/Panel/Padding/Layout/HealthValue");
            var levelValue = hud.GetNode<Label>(
                "StatusMargin/Panel/Padding/Layout/LevelValue");
            var experienceBar = hud.GetNode<ProgressBar>(
                "StatusMargin/Panel/Padding/Layout/ExperienceBar");
            var health = demo.GetNode<PlayerHealth>("Player/Health");
            Require(status.Visible && status.Size.Y <= 44.0f,
                "Default HUD is not a compact visible status bar.");
            Require(health.CurrentHealth == 5 && health.MaxHealth == 5 &&
                Mathf.IsEqualApprox((float)healthBar.MaxValue, 5.0f),
                "Player or compact health bar did not initialize with five health.");
            Require(!hud.IsDebugVisible, "Debug overlay must be hidden by default.");
            Require(!hud.StatusText.Contains('\n'), "Status bar must remain a single line.");
            Require(levelValue.Text == "境界 1" &&
                Mathf.IsEqualApprox((float)experienceBar.MaxValue, 8.0f),
                "Compact progression controls did not initialize at level one.");
            Require(hud.StatusText.Contains("击破", StringComparison.Ordinal) &&
                hud.StatusText.Contains("敌人", StringComparison.Ordinal),
                "Status bar is missing combat state.");
            Require(hud.DebugText.Contains("Seed", StringComparison.Ordinal) &&
                hud.DebugText.Contains("Tile", StringComparison.Ordinal),
                "Debug overlay is missing world diagnostics.");
            VerifyDefaultBinding();

            using var toggle = new InputEventAction { Action = "toggle_debug", Pressed = true };
            hud._UnhandledInput(toggle);
            Require(hud.IsDebugVisible, "Debug action did not open the overlay.");
            hud._UnhandledInput(toggle);
            Require(!hud.IsDebugVisible, "Second debug action did not close the overlay.");

            Require(health.ApplyDamage(1), "First player damage was not accepted.");
            Require(!health.ApplyDamage(1), "Invincibility did not reject immediate repeated damage.");
            await ToSignal(GetTree().CreateTimer(0.05), SceneTreeTimer.SignalName.Timeout);
            Require(health.CurrentHealth == 4 && Mathf.IsEqualApprox((float)healthBar.Value, 4.0f) &&
                healthValue.Text == "4/5", "Compact health bar did not follow player damage.");

            GD.Print("World HUD smoke test passed.");
            await WorldDemoTestCleanup.FreeAsync(this, demo);
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    /// <summary>
    /// 检查调试动作默认使用 F3 且保留可配置的第二键位槽。
    /// </summary>
    private static void VerifyDefaultBinding()
    {
        InputActionDefinition definition = InputActionCatalog.All.Single(
            action => action.Id == "toggle_debug");
        Require(definition.PrimaryKey == Key.F3 && definition.SecondaryKey == Key.None,
            "Debug action catalog must default to F3 plus an unbound slot.");
    }

    /// <summary>
    /// 将 HUD 契约失败转换为带有明确原因的测试异常。
    /// </summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
