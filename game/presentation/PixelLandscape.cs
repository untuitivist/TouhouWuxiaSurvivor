using Godot;

namespace Rebirth.Presentation;

public static class PixelLandscape
{
    public static Texture2D Load(string name)
        => GD.Load<Texture2D>($"res://assets/internal_original/base/scenery/{name}.png");
}
