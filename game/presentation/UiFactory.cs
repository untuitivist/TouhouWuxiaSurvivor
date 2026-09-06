using Godot;

namespace Rebirth.Presentation;

public sealed class UiFactory(Font body, Font title)
{
    public Theme CreateTheme()
    {
        var theme = new Theme { DefaultFont = body, DefaultFontSize = 18 };
        theme.SetColor("font_color", "Label", Palette.Paper);
        theme.SetColor("font_color", "Button", Palette.Paper);
        theme.SetColor("font_hover_color", "Button", Palette.Gold);
        theme.SetColor("font_pressed_color", "Button", Palette.Deep);
        theme.SetColor("font_focus_color", "Button", Palette.Gold);
        theme.SetStylebox("normal", "Button", Box(new("172b32"), new("47615e")));
        theme.SetStylebox("hover", "Button", Box(new("243b3e"), Palette.Gold));
        theme.SetStylebox("pressed", "Button", Box(Palette.Gold, Palette.Gold));
        theme.SetStylebox("focus", "Button", Box(Colors.Transparent, Palette.Gold, 2));
        return theme;
    }

    public static StyleBoxFlat Box(Color fill, Color border, int width = 1) => new()
    {
        BgColor = fill,
        BorderColor = border,
        BorderWidthBottom = width,
        BorderWidthTop = width,
        BorderWidthLeft = width,
        BorderWidthRight = width,
        ContentMarginLeft = 16,
        ContentMarginRight = 16,
        ContentMarginTop = 8,
        ContentMarginBottom = 8
    };

    public Panel Panel(Control parent, Rect2 rectangle, Color? fill = null)
    {
        var panel = new Panel { Position = rectangle.Position, Size = rectangle.Size };
        panel.AddThemeStyleboxOverride("panel", Box(fill ?? Palette.Ink, Palette.Alpha(Palette.Gold, 0.4f)));
        parent.AddChild(panel);
        return panel;
    }

    public Label Label(Control parent, string text, Rect2 rectangle, int size = 18, Color? color = null, bool heading = false)
    {
        var label = new Label
        {
            Text = text,
            Position = rectangle.Position,
            Size = rectangle.Size,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        label.AddThemeFontOverride("font", heading ? title : body);
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", color ?? Palette.Paper);
        parent.AddChild(label);
        label.Size = rectangle.Size;
        return label;
    }

    public Button Button(Control parent, string text, Rect2 rectangle, Action action, bool primary = false)
    {
        var button = new Button { Text = text, TooltipText = text, ClipText = true, Position = rectangle.Position, Size = rectangle.Size, MouseDefaultCursorShape = Control.CursorShape.PointingHand };
        if (primary)
        {
            button.AddThemeStyleboxOverride("normal", Box(new("35463f"), Palette.Gold));
            button.AddThemeColorOverride("font_color", Palette.Gold);
        }
        button.Pressed += action;
        parent.AddChild(button);
        button.Size = rectangle.Size;
        return button;
    }
}
