using Godot;
using TouhouWuxiaSurvivor.Settings;
using TouhouWuxiaSurvivor.Ui.Settings;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 验证拆分后的音频、按键和视频分页完整实例化，并遵守紧凑界面尺寸契约。
/// </summary>
public partial class SettingsPanelSmokeTest : Node
{
    /// <summary>
    /// 从真实主菜单打开设置，检查分页内容和返回流程，不改写任何持久化选项。
    /// </summary>
    public override void _Ready()
    {
        try
        {
            VerifyVideoMigration();
            Node menu = GD.Load<PackedScene>("res://src/ui/menu/MainMenu.tscn").Instantiate();
            AddChild(menu);
            var settings = menu.GetNode<SettingsPanel>("SettingsPanel");
            menu.GetNode<Button>("Menu/Panel/Padding/Layout/Settings")
                .EmitSignal(BaseButton.SignalName.Pressed);
            Require(settings.Visible && !menu.GetNode<Control>("Menu").Visible,
                "Settings did not replace the main menu commands.");

            VerifyAudioTab(settings);
            VerifyControlTab(settings);
            VerifyVideoTab(settings);
            settings.GetNode<Button>("Padding/Layout/Header/Back")
                .EmitSignal(BaseButton.SignalName.Pressed);
            Require(!settings.Visible && menu.GetNode<Control>("Menu").Visible,
                "Returning from settings did not restore the main menu.");

            menu.QueueFree();
            GD.Print("Settings panel smoke test passed.");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    /// <summary>
    /// 确认三个音量滑块和读数仍位于拆分后的音频页，并共享紧凑标签宽度。
    /// </summary>
    private static void VerifyAudioTab(SettingsPanel settings)
    {
        foreach (string section in new[] { "Master", "Music", "Sfx" })
        {
            string root = $"Padding/Layout/Tabs/音频/Audio/{section}";
            Require(settings.GetNode<HSlider>($"{root}/Slider") is not null,
                $"Audio slider is missing: {section}");
            Require(settings.GetNode<Label>($"{root}/Label").CustomMinimumSize.X <= 90.0f,
                $"Audio label is too wide: {section}");
        }
    }

    /// <summary>
    /// 确认全部输入动作生成双键位行，且按钮高度不会重新撑大滚动列表。
    /// </summary>
    private static void VerifyControlTab(SettingsPanel settings)
    {
        VBoxContainer rows = settings.GetNode<VBoxContainer>(
            "Padding/Layout/Tabs/按键/Controls/Scroll/Rows");
        Require(rows.GetChildCount() == InputActionCatalog.All.Count,
            "Control tab did not build every input action row.");
        var firstRow = (HBoxContainer)rows.GetChild(0);
        Require(((Button)firstRow.GetChild(1)).CustomMinimumSize.Y <= 26.0f,
            "Control binding buttons are too tall for the compact list.");
    }

    /// <summary>
    /// 确认视频页四类设置控件都存在，选项框高度保持在 28 像素以内。
    /// </summary>
    private static void VerifyVideoTab(SettingsPanel settings)
    {
        foreach (string section in new[] { "WindowMode", "Resolution", "FpsLimit" })
        {
            OptionButton option = settings.GetNode<OptionButton>(
                $"Padding/Layout/Tabs/视频/Video/{section}/Option");
            Require(option.ItemCount > 0 && option.CustomMinimumSize.Y <= 28.0f,
                $"Video option is missing or oversized: {section}");
        }

        OptionButton resolution = settings.GetNode<OptionButton>(
            "Padding/Layout/Tabs/视频/Video/Resolution/Option");
        string[] choices = Enumerable.Range(0, resolution.ItemCount)
            .Select(resolution.GetItemText)
            .ToArray();
        Require(choices.Length >= 7 && choices.Contains("640 × 360") &&
            choices.Contains("1920 × 1080") && choices.Contains("3840 × 2160"),
            "Resolution list does not cover the supported 16:9 range.");

        Require(settings.GetNode<CheckButton>(
            "Padding/Layout/Tabs/视频/Video/Vsync/Toggle") is not null,
            "VSync toggle is missing from the video tab.");

        OptionButton fps = settings.GetNode<OptionButton>(
            "Padding/Layout/Tabs/视频/Video/FpsLimit/Option");
        string[] fpsChoices = Enumerable.Range(0, fps.ItemCount)
            .Select(fps.GetItemText)
            .ToArray();
        Require(fpsChoices.SequenceEqual(new[]
            { "30 FPS", "60 FPS", "120 FPS", "144 FPS", "不限制" }),
            "FPS options diverged from the shared video catalog.");
        Require(VideoSettingsCatalog.GetFpsLimit(fps.ItemCount - 1) == 0,
            "The visible unlimited option does not map to Engine.MaxFps zero.");
        Label effectiveState = settings.GetNode<Label>(
            "Padding/Layout/Tabs/视频/Video/EffectiveState");
        Require(!string.IsNullOrWhiteSpace(effectiveState.Text) &&
            effectiveState.Text.Contains("软件上限") && effectiveState.Text.Contains("VSync"),
            "Video settings do not expose the effective frame-pacing state.");
    }

    /// <summary>
    /// 验证旧存档中的三帧锁定和非法尺寸会被审计并修复，同时合法值与无限制保持不变。
    /// </summary>
    private static void VerifyVideoMigration()
    {
        GameSettingsData invalid = GameSettingsData.CreateDefault();
        invalid.MaxFps = 3;
        invalid.ResolutionWidth = 7680;
        invalid.ResolutionHeight = 4320;
        GameSettingsRepairReport repair = VideoSettingsCatalog.Normalize(invalid);
        Require(repair.Changed && repair.OriginalMaxFps == 3 &&
            repair.AppliedMaxFps == 30 && invalid.MaxFps == 30,
            "Legacy three-FPS settings were not migrated to the nearest supported limit.");
        Require(invalid.ResolutionWidth == 1280 && invalid.ResolutionHeight == 720,
            "An unsupported legacy resolution was not restored to the visible default.");

        foreach (int expected in new[] { 30, 60, 120, 144, 0 })
        {
            GameSettingsData valid = GameSettingsData.CreateDefault();
            valid.MaxFps = expected;
            Require(!VideoSettingsCatalog.Normalize(valid).Changed && valid.MaxFps == expected,
                $"Supported FPS limit changed during migration: {expected}.");
        }

        Require(VideoSettingsCatalog.NormalizeFpsLimit(90) == 60 &&
            VideoSettingsCatalog.NormalizeFpsLimit(360) == 144 &&
            VideoSettingsCatalog.NormalizeFpsLimit(-1) == 30,
            "Invalid FPS limits do not follow nearest-finite migration rules.");
    }

    /// <summary>
    /// 将设置布局契约失败转换为带明确原因的测试异常。
    /// </summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
