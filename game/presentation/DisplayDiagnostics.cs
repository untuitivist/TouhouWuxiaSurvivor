using Godot;

namespace Rebirth.Presentation;

public partial class GameRoot
{
    private async void RunDisplaySmokeTests()
    {
        try
        {
            Require(DisplayServer.GetName() != "headless", "Real display is required");
            var expected = new VideoPreferences();
            foreach (var mode in new[] { 0, 1, 2, 0 })
            {
                expected = new() { WindowMode = mode, Width = 640, Height = 360, MaxFps = 60, Vsync = true };
                expected.Apply();
                for (var frame = 0; frame < 8; frame++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                GD.Print($"DISPLAY_REQUEST requested={mode} actual={DisplayServer.WindowGetMode()} size={DisplayServer.WindowGetSize()} borderless={DisplayServer.WindowGetFlag(DisplayServer.WindowFlags.Borderless)}");
                var expectedMode = mode switch { 1 => DisplayServer.WindowMode.Maximized, 2 => DisplayServer.WindowMode.Fullscreen, _ => DisplayServer.WindowMode.Windowed };
                Require(DisplayServer.WindowGetMode() == expectedMode, "Actual window mode matches request");
                if (mode != 2)
                {
                    Require(DisplayServer.WindowGetFlag(DisplayServer.WindowFlags.Borderless) == (mode == 1), "Actual borderless flag matches request");
                    if (mode == 0) Require(DisplayServer.WindowGetSize() == new Vector2I(640, 360), "Actual window size matches request");
                    else
                    {
                        var usable = DisplayServer.ScreenGetUsableRect(DisplayServer.WindowGetCurrentScreen());
                        var actual = new Rect2I(DisplayServer.WindowGetPosition(), DisplayServer.WindowGetSize());
                        GD.Print($"DISPLAY_MAXIMIZED usable={usable} actual={actual}");
                        Require(actual.Grow(16).Encloses(usable) && usable.Grow(16).Encloses(actual), "Maximized window matches work area within native frame margins");
                    }
                }
                Require(Engine.MaxFps == 60, "Frame cap applies independently of physics");
                GD.Print($"DISPLAY mode={mode} size={DisplayServer.WindowGetSize()} vsync={DisplayServer.WindowGetVsyncMode()} fps={Engine.MaxFps}");
            }
            profile.Data.Video = expected;
            ShowSettings();
            settingsTab = 1;
            BuildSettings();
            BeginVideoPreview(new() { WindowMode = 2 }, true);
            TickVideoPreview(16);
            for (var frame = 0; frame < 8; frame++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Require(DisplayServer.WindowGetMode() == DisplayServer.WindowMode.Windowed && DisplayServer.WindowGetSize() == new Vector2I(640, 360), "Preview timeout restores real window");
            GD.Print("REBIRTH_DISPLAY_PASS");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
}
