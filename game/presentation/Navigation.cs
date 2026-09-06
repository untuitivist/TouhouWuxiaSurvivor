using Godot;
using Rebirth.Core;

namespace Rebirth.Presentation;

public partial class GameRoot
{
    private bool resumeAfterInspection;

    public override void _Input(InputEvent input)
    {
        if (input is not InputEventKey { Pressed: true, Echo: false } key) return;
        var code = key.PhysicalKeycode == Key.None ? key.Keycode : key.PhysicalKeycode;
        if (code == Key.F11)
        {
            var mode = DisplayServer.WindowGetMode();
            DisplayServer.WindowSetMode(mode == DisplayServer.WindowMode.Fullscreen ? DisplayServer.WindowMode.Windowed : DisplayServer.WindowMode.Fullscreen);
        }
        else if (input.IsActionPressed(GameControls.Inspect))
        {
            if (currentScreen == "build") CloseBuild();
            else if (currentScreen is "playing" or "pause" or "choices") OpenBuild();
            else return;
        }
        else if (input.IsActionPressed(GameControls.Pause))
        {
            if (code == Key.P && run == null) return;
            NavigateBack();
        }
        else return;
        GetViewport().SetInputAsHandled();
    }

    private void NavigateBack()
    {
        dashRequested = false;
        switch (currentScreen)
        {
            case "build": CloseBuild(); break;
            case "settings":
            case "changelog":
                if (run == null) ShowTitle(); else ShowPause();
                break;
            case "abandon": ShowPause(); break;
            case "playing":
            case "pause":
                run!.TogglePause();
                RefreshRunScreen();
                break;
            case "choices": break;
            default: ShowTitle(); break;
        }
    }

    private void OpenBuild()
    {
        if (run == null || currentScreen is not ("playing" or "pause" or "choices")) return;
        resumeAfterInspection = run.Phase == RunPhase.Playing;
        if (resumeAfterInspection) run.TogglePause();
        displayedPhase = run.Phase;
        ShowBuild();
    }

    private void CloseBuild()
    {
        if (run == null) { ShowTitle(); return; }
        if (resumeAfterInspection && run.Phase == RunPhase.Paused) run.TogglePause();
        resumeAfterInspection = false;
        RefreshRunScreen();
    }
}
