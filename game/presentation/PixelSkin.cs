using Godot;

namespace Rebirth.Presentation;

public static class PixelSkin
{
    public static readonly Color Ink = new("30413b");
    public static readonly Color Muted = new("5d6558");
    public static readonly Color Paper = new("e8e3cd");
    public static readonly Color Light = new("fff1ce");
    public static readonly Color Wood = new("754c38");
    public static readonly Color Gold = new("bb8c4e");
    public static readonly Color Red = new("a84342");
    public static readonly Color Green = new("526b51");
    private static readonly Dictionary<string, StyleBoxTexture> styles = [];
    private static readonly Dictionary<string, Texture2D> icons = [];

    public static Color TextColor(Color color)
    {
        var result = color == Palette.Paper ? Ink : color == Palette.Muted ? Muted : color == Palette.Gold ? new Color("8a512c")
            : color == Palette.Jade ? Green : color == Palette.Red ? Red : color == Palette.Violet ? new Color("715275")
            : new Color(color.R * 0.55f, color.G * 0.55f, color.B * 0.55f);
        return new(result.R, result.G, result.B, color.A);
    }

    public static StyleBoxTexture Frame(string kind)
    {
        if (styles.TryGetValue(kind, out var existing)) return existing;
        var style = new StyleBoxTexture
        {
            Texture = Artwork(kind),
            AxisStretchHorizontal = StyleBoxTexture.AxisStretchMode.Tile,
            AxisStretchVertical = StyleBoxTexture.AxisStretchMode.Tile
        };
        foreach (var side in new[] { Side.Left, Side.Top, Side.Right, Side.Bottom }) style.SetTextureMargin(side, 14);
        style.ContentMarginLeft = 12;
        style.ContentMarginRight = 12;
        style.ContentMarginTop = 3;
        style.ContentMarginBottom = 5;
        styles[kind] = style;
        return style;
    }

    public static Texture2D Icon(string kind)
    {
        if (icons.TryGetValue(kind, out var existing)) return existing;
        var texture = Artwork(kind);
        icons[kind] = texture;
        return texture;
    }

    public static Texture2D Artwork(string name)
    {
        if (icons.TryGetValue(name, out var existing)) return existing;
        var texture = GD.Load<Texture2D>($"{VisualAssets.InterfaceRoot}{name}.png");
        icons[name] = texture;
        return texture;
    }
}
