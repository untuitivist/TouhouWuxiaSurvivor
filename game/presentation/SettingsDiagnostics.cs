using System.Text;
using Godot;
using Rebirth.Core;

namespace Rebirth.Presentation;

public partial class GameRoot
{
    private void TestFullSettings()
    {
        var initialVideo = profile.Data.Video.Copy();
        var kills = profile.Data.BestKills;
        PressButton("画面");
        AssertUiBounds();
        SelectSetting("window_mode", 1);
        Require(SettingOption("resolution").Disabled, "Borderless disables window resolution");
        SelectSetting("window_mode", 0);
        Require(!SettingOption("resolution").Disabled, "Windowed enables window resolution");
        SelectSetting("fps_limit", 2);
        SelectSetting("resolution", 1);
        PressButton("应用画面设置");
        AssertUiBounds();
        Require(currentScreen == "video_confirm" && Engine.MaxFps == 120 && profile.Data.Video.MaxFps == initialVideo.MaxFps, "Preview applies but does not persist");
        PressKey(Key.Escape);
        Require(currentScreen == "settings" && Engine.MaxFps == initialVideo.MaxFps, "Escape rolls back preview");
        SelectSetting("fps_limit", 3);
        PressButton("应用画面设置");
        TickVideoPreview(16);
        Require(videoPreview == null && Engine.MaxFps == initialVideo.MaxFps, "Timeout rolls back preview");
        SelectSetting("fps_limit", 2);
        SelectSetting("resolution", 1);
        PressButton("应用画面设置");
        PressButton("保留画面设置");
        Require(profile.Data.Video.MaxFps == 120 && profile.Data.Video.Width == 960, "Confirmed display persists");
        SelectSetting("fps_limit", 0);
        PressButton("声音");
        PressButton("画面");
        Require(videoDraft.MaxFps == 120, "Leaving video tab discards unapplied draft");
        PressButton("恢复本页默认");
        AssertUiBounds();
        PressButton("确认恢复");
        PressButton("保留画面设置");
        Require(profile.Data.Video.MaxFps == 60 && profile.Data.Video.Width == 1280 && profile.Data.MasterVolume == 0.37f, "Video reset preserves audio");

        PressButton("操作");
        AssertUiBounds();
        Require(bindingButtons.Count == GameControls.Actions.Length * 2, "All current actions have two slots");
        BeginBindingCapture(GameControls.Dash, 0);
        PressKey(Key.Tab);
        Require(settingsMessage.Contains("保留"), "UI navigation keys cannot be bound");
        BeginBindingCapture(GameControls.Dash, 0);
        PressKey(Key.J);
        Require(profile.Data.Bindings[GameControls.Dash][0] == (long)Key.J && GameControls.Hint(GameControls.Dash) == "J", "Binding applies and hint updates");
        BeginBindingCapture(GameControls.Focus, 0);
        PressKey(Key.J);
        Require(profile.Data.Bindings[GameControls.Focus][0] == (long)Key.Shift && settingsMessage.Contains("闪身"), "Cross-action conflict is rejected with owner");
        BeginBindingCapture(GameControls.Dash, 0);
        PressKey(Key.Delete);
        Require(profile.Data.Bindings[GameControls.Dash][0] == (long)Key.J && settingsMessage.Contains("至少"), "Cannot erase last binding");
        BeginBindingCapture(GameControls.Dash, 1);
        PressKey(Key.K);
        BeginBindingCapture(GameControls.Dash, 1);
        PressKey(Key.Delete);
        Require(profile.Data.Bindings[GameControls.Dash][1] == 0, "Secondary binding can be cleared");
        BeginBindingCapture(GameControls.Dash, 0);
        PressKey(Key.Escape);
        Require(currentScreen == "settings" && captureAction == null && profile.Data.Bindings[GameControls.Dash][0] == (long)Key.J, "Escape cancels capture, not page");
        BeginBindingCapture(GameControls.Dash, 0);
        PressKey(Key.F11);
        Require(currentScreen == "settings" && settingsMessage.Contains("全屏"), "Capture consumes fullscreen shortcut");
        BeginBindingCapture(GameControls.Inspect, 0);
        PressKey(Key.I);
        PressButton("返回");
        Require(currentScreen == "pause", "Settings returns to pause");
        PressKey(Key.I);
        Require(currentScreen == "build", "Rebound inspect works before GUI");
        PressKey(Key.I);
        PressButton("继续行走");
        PressKey(Key.J);
        Require(dashRequested, "Rebound dash reaches gameplay");
        dashRequested = false;
        PressKey(Key.Space);
        Require(!dashRequested, "Old dash no longer triggers gameplay");
        PressKey(Key.F11);
        Require(currentScreen == "video_confirm" && run!.Phase == RunPhase.Paused, "Fullscreen shortcut safely pauses combat");
        PressKey(Key.Escape);
        Require(currentScreen == "playing" && run!.Phase == RunPhase.Playing, "Fullscreen rollback returns to combat");
        PressKey(Key.P);
        ShowSettings();
        PressButton("操作");
        PressButton("恢复本页默认");
        PressButton("取消");
        Require(GameControls.Hint(GameControls.Dash) == "J", "Reset cancellation keeps bindings");
        PressButton("恢复本页默认");
        PressButton("确认恢复");
        Require(GameControls.Hint(GameControls.Dash) == "Space" && profile.Data.BestKills == kills, "Control reset preserves records");
        var preservedRun = run;
        var testRun = new RunState(HeroKind.Reimu, 42);
        run = testRun;
        testRun.AddExperience(30);
        testRun.Step(default);
        GameControls.SetBinding(profile.Data.Bindings, GameControls.ChoiceTwo, 0, Key.Enter);
        RefreshRunScreen();
        var selectedArt = testRun.Choices[1];
        var originalRank = testRun.Build.Rank(selectedArt);
        PressKey(Key.Enter);
        Require(testRun.Build.Rank(selectedArt) == originalRank + 1, "Rebound choice takes priority over focused first button");
        run = preservedRun;
        profile.Data.Bindings = GameControls.DefaultBindings();
        GameControls.Configure(profile.Data.Bindings);
        TestSettingsPersistence();
        ShowSettings();
        GD.Print("SETTINGS_PASS: video apply/confirm/timeout/cancel, dual-slot capture/conflict/clear/reset, dynamic hints, gameplay routing, persistence and defaults");
    }

    private OptionButton SettingOption(string name) => Descendants(screen!).OfType<OptionButton>().Single(option => option.Name == name);

    private void SelectSetting(string name, int selected)
    {
        var option = SettingOption(name);
        option.Select(selected);
        option.EmitSignal(OptionButton.SignalName.ItemSelected, selected);
    }

    private static void TestSettingsPersistence()
    {
        var directory = ProjectSettings.GlobalizePath($"user://rebirth/diagnostics/settings-tests/{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "settings.json");
        var store = new ProfileStore(path);
        store.Data.BestKills = 123;
        store.Data.Video = new() { WindowMode = 1, Width = 1920, Height = 1080, MaxFps = 144, Vsync = false };
        store.Data.Bindings[GameControls.Dash] = [(long)Key.J, (long)Key.K];
        store.Save();
        var restored = new ProfileStore(path);
        Require(restored.Data.Video.WindowMode == 1 && restored.Data.Video.Width == 1920 && restored.Data.Video.MaxFps == 144 && !restored.Data.Video.Vsync, "Video round trip");
        Require(restored.Data.Bindings[GameControls.Dash].SequenceEqual(new long[] { (long)Key.J, (long)Key.K }) && restored.Data.BestKills == 123, "Bindings and records round trip");
        var legacy = Path.Combine(directory, "legacy.json");
        System.IO.File.WriteAllText(legacy, "{\"Version\":1,\"BestKills\":87,\"MasterVolume\":0.25,\"ReducedMotion\":true}", new UTF8Encoding(false));
        var migrated = new ProfileStore(legacy);
        Require(migrated.Data.BestKills == 87 && migrated.Data.MasterVolume == 0.25f && migrated.Data.ReducedMotion && migrated.Data.Video.MaxFps == 60 && migrated.Data.Bindings.Count == GameControls.Actions.Length, "Old rebirth profile gains defaults without losing preferences");
        var invalid = Path.Combine(directory, "invalid.json");
        System.IO.File.WriteAllText(invalid, "{\"Version\":1,\"BestKills\":87,\"Video\":{\"WindowMode\":-5,\"Width\":3,\"Height\":2,\"MaxFps\":-100},\"Bindings\":null}", new UTF8Encoding(false));
        var repaired = new ProfileStore(invalid);
        Require(repaired.Data.BestKills == 87 && repaired.Data.Video.WindowMode == 0 && repaired.Data.Video.Width == 1280 && repaired.Data.Video.MaxFps == 30 && repaired.Data.Bindings.Count == GameControls.Actions.Length, "Invalid display values and null bindings repair without discarding records");
        var malformed = GameControls.DefaultBindings();
        malformed[GameControls.Up] = [-10, long.MaxValue];
        malformed[GameControls.Dash] = [(long)Key.W, (long)Key.W];
        malformed[GameControls.Focus] = [];
        var normalized = GameControls.NormalizeBindings(malformed);
        var keys = normalized.Values.SelectMany(value => value).Where(key => key != 0).ToArray();
        Require(normalized.Values.All(value => value.Length == 2 && value.Any(key => key != 0)) && keys.Distinct().Count() == keys.Length, "Malformed and colliding bindings repair into usable unique keys");
        var bytes = System.IO.File.ReadAllBytes(path);
        Require(!(bytes[0] == 239 && bytes[1] == 187 && bytes[2] == 191), "Settings save uses UTF-8 without BOM");
    }
}
