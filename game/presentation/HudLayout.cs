using Godot;

namespace Rebirth.Presentation;

public static class HudLayout
{
    public static readonly Rect2 PauseButton = new(1112, 109, 136, 82);
    public static readonly Rect2 InspectButton = new(958, 109, 136, 82);

    public static Rect2 Minimap(bool touchVisible)
        => new(1111, touchVisible ? PauseButton.End.Y + 18 : 108, 141, 111);
}
