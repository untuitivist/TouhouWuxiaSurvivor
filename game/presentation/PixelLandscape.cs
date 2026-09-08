using Godot;

namespace Rebirth.Presentation;

public static class PixelLandscape
{
    public static Texture2D Load(string name)
        => GD.Load<Texture2D>($"{VisualAssets.Root}scenery/{name}.png");
}
