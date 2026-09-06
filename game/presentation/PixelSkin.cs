using Godot;

namespace Rebirth.Presentation;

public static class PixelSkin
{
    public static readonly Color Ink = new("49362d");
    public static readonly Color Muted = new("79664f");
    public static readonly Color Paper = new("f2e3bc");
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
        using var image = Image.CreateEmpty(32, 32, false, Image.Format.Rgba8);
        image.Fill(Colors.Transparent);
        var fill = kind switch
        {
            "primary" => new Color("bfd0a0"), "hover" => Light, "pressed" => new Color("d7b47d"),
            "disabled" => new Color("d4cab1"), "inset" => new Color("e5d3aa"), "dark" => new Color("342e2e"),
            "track" => new Color("a68c65"), "fill" => Green, _ => Paper
        };
        if (kind == "focus")
        {
            foreach (var corner in new[] { new Vector2I(0, 0), new Vector2I(26, 0), new Vector2I(0, 26), new Vector2I(26, 26) })
            {
                Block(image, corner.X, corner.Y < 10 ? 0 : 30, 6, 2, Red);
                Block(image, corner.X < 10 ? 0 : 30, corner.Y, 2, 6, Red);
            }
        }
        else
        {
            Block(image, 3, 4, 27, 27, new("36272c"));
            Block(image, 1, 3, 29, 25, Wood);
            Block(image, 3, 1, 25, 29, Wood);
            Block(image, 3, 3, 25, 25, Gold);
            Block(image, 4, 4, 23, 23, fill);
            Block(image, 5, 4, 21, 1, kind == "dark" ? Wood : Light);
            Block(image, 4, 5, 1, 21, kind == "dark" ? Wood : Light);
            Block(image, 5, 26, 21, 1, kind == "dark" ? Wood : new("c8ac7b"));
            if (kind == "panel")
            {
                foreach (var corner in new[] { new Vector2I(2, 2), new Vector2I(25, 2), new Vector2I(2, 25), new Vector2I(25, 25) })
                {
                    Block(image, corner.X, corner.Y, 4, 4, Wood);
                    Block(image, corner.X + 1, corner.Y + 1, 2, 2, Light);
                }
            }
        }
        image.Resize(64, 64, Image.Interpolation.Nearest);
        var style = new StyleBoxTexture { Texture = ImageTexture.CreateFromImage(image) };
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
        using var image = Image.CreateEmpty(12, 12, false, Image.Format.Rgba8);
        image.Fill(Colors.Transparent);
        if (kind is "thumb" or "thumb-hover")
        {
            Block(image, 2, 1, 8, 10, Wood);
            Block(image, 3, 2, 6, 8, kind == "thumb-hover" ? Light : Gold);
            Block(image, 5, 4, 2, 4, Paper);
        }
        else
            for (var row = 0; row < 4; row++) Block(image, 2 + row, 4 + row, 8 - row * 2, 1, Ink);
        image.Resize(24, 24, Image.Interpolation.Nearest);
        var texture = ImageTexture.CreateFromImage(image);
        icons[kind] = texture;
        return texture;
    }

    private static void Block(Image image, int horizontal, int vertical, int width, int height, Color color)
        => image.FillRect(new(horizontal, vertical, width, height), color);
}
