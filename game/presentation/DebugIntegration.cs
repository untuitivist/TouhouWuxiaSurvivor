using Godot;

namespace Rebirth.Presentation;

public partial class GameRoot
{
    private readonly DebugOverlay debugOverlay = new();

    private void InitializeDebugOverlay(CanvasLayer layer, Font font)
    {
        debugOverlay.BodyFont = font;
        layer.AddChild(debugOverlay);
    }

    private void UpdateDebugState()
    {
        debugOverlay.Run = run;
        debugOverlay.ScreenName = currentScreen;
        debugOverlay.Video = profile.Data.Video;
    }

    private void ToggleDebug()
    {
        UpdateDebugState();
        debugOverlay.Toggle();
    }
}
