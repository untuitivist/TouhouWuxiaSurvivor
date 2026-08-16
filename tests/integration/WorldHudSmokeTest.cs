using Godot;
using TouhouWuxiaSurvivor.Actors.Player;
using TouhouWuxiaSurvivor.Demo;
using TouhouWuxiaSurvivor.Settings;
using TouhouWuxiaSurvivor.Tests.Support;
using TouhouWuxiaSurvivor.Ui.Debug;
using TouhouWuxiaSurvivor.Ui.Hud;
using TouhouWuxiaSurvivor.Ui.Hud.SpellCards;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 验证游戏默认只显示窄状态栏，并能通过可重绑定的 F3 动作切换详细调试文字。
/// </summary>
public partial class WorldHudSmokeTest : Node
{
    /// <summary>
    /// 实例化真实游戏页，检查角色血量、受伤无敌、默认可见性、F3 默认键和调试层双向切换。
    /// </summary>
    public override async void _Ready()
    {
        WorldDemo? demo = null;
        int exitCode = 0;
        try
        {
            demo = GD.Load<PackedScene>("res://src/demo/WorldDemo.tscn")
                .Instantiate<WorldDemo>();
            demo.PersistMetaProgression = false;
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
            var pacingMargin = hud.GetNode<Control>("PacingMargin");
            var pacingBar = hud.GetNode<RunPacingBar>(
                "PacingMargin/Panel/Padding/Layout/PacingBar");
            var phaseRemaining = hud.GetNode<Label>(
                "PacingMargin/Panel/Padding/Layout/Remaining");
            var spellCards = hud.GetNode<SpellCardHudStrip>("SpellCards");
            int expectedHealth = (int)MathF.Round(
                demo.RunContext.CharacterSelection.Current.PlayableProfile.MaxHealth);
            Require(status.Visible && status.Size.Y <= 44.0f,
                "Default HUD is not a compact visible status bar.");
            Require(health.CurrentHealth == expectedHealth &&
                health.MaxHealth == expectedHealth &&
                Mathf.IsEqualApprox((float)healthBar.MaxValue, expectedHealth),
                "Player or compact health bar did not initialize from the character profile.");
            Require(!spellCards.Visible && hud.SpellCardIconCount == 0 &&
                spellCards.Position.X >= 0.0f && spellCards.Position.Y >= 0.0f &&
                spellCards.Position.X + spellCards.Size.X <= 640.0f &&
                spellCards.Position.Y + spellCards.Size.Y <= status.Position.Y,
                "Empty spell-card strip was visible or escaped the compact HUD bounds.");
            Require(!hud.IsDebugVisible, "Debug overlay must be hidden by default.");
            Require(pacingMargin.Visible && pacingMargin.Size.Y <= 28.0f &&
                hud.PhaseText == "异变初兆" && phaseRemaining.Text == "蓄势 00:30" &&
                pacingBar.ProgressRatio >= 0.0 && pacingBar.ProgressRatio < 0.01,
                "Compact five-minute pacing bar did not initialize at the opening phase.");
            Require(hud.IsPhaseNoticeVisible,
                "Opening phase cue did not appear without enlarging the status bar.");
            Require(!hud.StatusText.Contains('\n'), "Status bar must remain a single line.");
            Require(levelValue.Text == "境界 1" &&
                Mathf.IsEqualApprox((float)experienceBar.MaxValue, 8.0f),
                "Compact progression controls did not initialize at level one.");
            Require(hud.StatusText.Contains("击破", StringComparison.Ordinal) &&
                hud.StatusText.Contains("敌人", StringComparison.Ordinal),
                "Status bar is missing combat state.");
            VerifyDefaultBinding();

            using var toggle = new InputEventAction { Action = "toggle_debug", Pressed = true };
            hud._UnhandledInput(toggle);
            Require(hud.IsDebugVisible, "Debug action did not open the overlay.");
            await ToSignal(GetTree().CreateTimer(0.55), SceneTreeTimer.SignalName.Timeout);
            Require(hud.DebugText.Contains("Seed", StringComparison.Ordinal) &&
                hud.DebugText.Contains("Tile", StringComparison.Ordinal) &&
                hud.DebugText.Contains("难度", StringComparison.Ordinal) &&
                hud.DebugText.Contains("30秒窗", StringComparison.Ordinal) &&
                hud.DebugText.Contains("击破", StringComparison.Ordinal) &&
                hud.DebugText.Contains("刷新", StringComparison.Ordinal),
                "Visible debug overlay is missing pressure-window diagnostics.");
            hud._UnhandledInput(toggle);
            Require(!hud.IsDebugVisible, "Second debug action did not close the overlay.");

            Require(health.ApplyDamage(1), "First player damage was not accepted.");
            Require(!health.ApplyDamage(1), "Invincibility did not reject immediate repeated damage.");
            await ToSignal(GetTree().CreateTimer(0.05), SceneTreeTimer.SignalName.Timeout);
            int damagedHealth = expectedHealth - 1;
            Require(health.CurrentHealth == damagedHealth &&
                Mathf.IsEqualApprox((float)healthBar.Value, damagedHealth) &&
                healthValue.Text == $"{damagedHealth}/{expectedHealth}",
                "Compact health bar did not follow player damage.");

            GD.Print("World HUD smoke test passed.");
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            exitCode = 1;
        }
        finally
        {
            if (demo is not null)
            {
                await WorldDemoTestCleanup.FreeAsync(this, demo);
            }

            GetTree().Quit(exitCode);
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
