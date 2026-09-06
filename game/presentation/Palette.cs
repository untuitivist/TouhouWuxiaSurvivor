using Godot;

namespace Rebirth.Presentation;

public static class Palette
{
    public static readonly Color Ink = new("101e25");
    public static readonly Color Deep = new("091318");
    public static readonly Color Paper = new("eee8d4");
    public static readonly Color Muted = new("94aaa9");
    public static readonly Color Gold = new("d9bb7c");
    public static readonly Color Jade = new("82cfb5");
    public static readonly Color Red = new("d96c78");
    public static readonly Color Violet = new("b8a4e5");
    public static Color Alpha(Color color, float alpha) => new(color.R, color.G, color.B, alpha);
    public static Vector2 Vector(System.Numerics.Vector2 vector) => new(vector.X, vector.Y);
}
