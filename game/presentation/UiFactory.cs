using Godot;

namespace Rebirth.Presentation;

public sealed class UiFactory(Font body, Font title)
{
    public Theme CreateTheme() => PixelTheme.Create(body);

    public Panel Panel(Control parent, Rect2 rectangle, Color? fill = null)
    {
        var panel = new Panel { Position = rectangle.Position, Size = rectangle.Size, TextureFilter = CanvasItem.TextureFilterEnum.Nearest };
        panel.AddThemeStyleboxOverride("panel", PixelSkin.Frame(fill.HasValue ? "inset" : "panel"));
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
        label.AddThemeColorOverride("font_color", PixelSkin.TextColor(color ?? Palette.Paper));
        parent.AddChild(label);
        label.Size = rectangle.Size;
        return label;
    }

    public Button Button(Control parent, string text, Rect2 rectangle, Action action, bool primary = false)
    {
        var button = new Button { Text = text, TooltipText = text, ClipText = true, Position = rectangle.Position, Size = rectangle.Size, MouseDefaultCursorShape = Control.CursorShape.PointingHand };
        if (primary)
        {
            button.AddThemeStyleboxOverride("normal", PixelSkin.Frame("primary"));
        }
        button.Pressed += action;
        parent.AddChild(button);
        button.Size = rectangle.Size;
        return button;
    }
}
