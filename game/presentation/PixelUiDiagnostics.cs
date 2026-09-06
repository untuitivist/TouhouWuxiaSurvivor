using Godot;

namespace Rebirth.Presentation;

public partial class GameRoot
{
    private void TestPixelUiAndDebug()
    {
        Require(interfaceRoot.TextureFilter == CanvasItem.TextureFilterEnum.Nearest, "UI textures use nearest filtering");
        foreach (var state in new[] { "normal", "hover", "pressed", "disabled", "focus" })
            Require(interfaceRoot.Theme.GetStylebox(state, "Button") is StyleBoxTexture, $"Pixel button state: {state}");
        Require(interfaceRoot.Theme.GetStylebox("normal", "OptionButton") is StyleBoxTexture, "Dropdowns share pixel styling");
        var oldBindings = GameControls.DefaultBindings();
        oldBindings.Remove(GameControls.Debug);
        oldBindings[GameControls.Dash] = [(long)Key.F3, 0];
        var migrated = GameControls.NormalizeBindings(oldBindings);
        Require(migrated[GameControls.Dash][0] == (long)Key.F3 && migrated[GameControls.Debug][0] != (long)Key.F3, "New debug action does not steal an existing user's F3 binding");
        ShowTitle();
        var count = debugOverlay.RefreshCount;
        debugOverlay._Process(1);
        Require(!debugOverlay.Visible && debugOverlay.RefreshCount == count, "Hidden debug overlay does not rebuild text");
        PressKey(Key.F3);
        Require(debugOverlay.Visible && debugOverlay.Summary.Contains("尚未开始"), "F3 opens safely in title without a run");
        if (!OS.IsDebugBuild()) Require(debugOverlay.Summary.Contains("N/A（发行引擎不提供）"), "Release F3 does not invent debug-only memory metrics");
        PressKey(Key.F3);
        StartRun(Rebirth.Core.HeroKind.Reimu, 197);
        var ticks = run!.Ticks;
        PressKey(Key.F3);
        Require(debugOverlay.Visible && run.Ticks == ticks && currentScreen == "playing", "F3 does not pause or advance gameplay");
        Require(debugOverlay.Summary.Contains("Seed: 197") && debugOverlay.Summary.Contains("2D 世界单位"), "Debug reports actual seed and 2D coordinates");
        run.Step(default);
        Require(run.Ticks == ticks + 1, "Simulation continues with debug visible");
        PressKey(Key.E);
        var pausedTicks = run.Ticks;
        debugOverlay._Process(0.3);
        Require(currentScreen == "build" && run.Ticks == pausedTicks, "Debug refresh cannot unpause inspection");
        PressKey(Key.F3);
        PressKey(Key.Escape);
        PressKey(Key.P);
        ShowSettings();
        PressButton("操作");
        BeginBindingCapture(GameControls.Dash, 0);
        PressKey(Key.F3);
        Require(!debugOverlay.Visible && settingsMessage.Contains("诊断信息"), "F3 capture reports conflict without toggling overlay");
        BeginBindingCapture(GameControls.Debug, 0);
        PressKey(Key.F4);
        PressButton("返回");
        PressKey(Key.F3);
        Require(!debugOverlay.Visible, "Old F3 stops working after rebind");
        PressKey(Key.F4);
        Require(debugOverlay.Visible && debugOverlay.Summary.Contains("[F4]"), "Rebound debug key and its hint work");
        PressKey(Key.F4);
        profile.Data.Bindings = GameControls.DefaultBindings();
        GameControls.Configure(profile.Data.Bindings);
        ShowTitle();
        GD.Print("PIXEL_DEBUG_PASS: textured control states, nearest filtering, hidden refresh suppression, F3 state isolation/capture/rebinding, real seed and coordinates");
    }
}
