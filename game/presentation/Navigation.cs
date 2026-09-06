using Godot;
using Rebirth.Core;

namespace Rebirth.Presentation;

public partial class GameRoot
{
    private bool resumeAfterInspection;

    public override void _Input(InputEvent input)
    {
        if (input is InputEventScreenTouch { Pressed: true } or InputEventMouseButton { Pressed: true } or InputEventKey { Pressed: true }) audio.Activate(profile.Data, !diagnosticMode);
        if (touchHud.Handle(input)) { GetViewport().SetInputAsHandled(); return; }
        if (input is not InputEventKey key) return;
        if (CaptureBinding(key)) { GetViewport().SetInputAsHandled(); return; }
        if (!key.Pressed || key.Echo) return;
        var code = key.PhysicalKeycode == Key.None ? key.Keycode : key.PhysicalKeycode;
        if (input.IsActionPressed(GameControls.Debug)) ToggleDebug();
        else if (currentScreen == "video_confirm")
        {
            if (code != Key.Escape && !input.IsActionPressed(GameControls.Fullscreen)) return;
            FinishVideoPreview(false);
        }
        else if (currentScreen == "settings_reset")
        {
            if (code != Key.Escape) return;
            BuildSettings();
        }
        else if (input.IsActionPressed(GameControls.Fullscreen))
        {
            if (GamePlatform.IsWeb) { ToggleWebFullscreen(); GetViewport().SetInputAsHandled(); return; }
            var candidate = profile.Data.Video.Copy();
            candidate.WindowMode = candidate.WindowMode == 2 ? 0 : 2;
            BeginVideoPreview(candidate, currentScreen == "settings");
        }
        else if (input.IsActionPressed(GameControls.Inspect))
        {
            if (currentScreen == "build") CloseBuild();
            else if (currentScreen is "playing" or "pause" or "choices") OpenBuild();
            else return;
        }
        else if (code == Key.Escape || input.IsActionPressed(GameControls.Pause))
        {
            if (code != Key.Escape && run == null && currentScreen != "settings") return;
            NavigateBack();
        }
        else if (currentScreen == "playing" && input.IsActionPressed(GameControls.Dash)) dashRequested = true;
        else if (currentScreen == "choices")
        {
            var index = input.IsActionPressed(GameControls.ChoiceOne) ? 0 : input.IsActionPressed(GameControls.ChoiceTwo) ? 1 : input.IsActionPressed(GameControls.ChoiceThree) ? 2 : -1;
            if (index < 0) return;
            SelectArt(index);
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
