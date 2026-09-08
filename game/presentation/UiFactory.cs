using Godot;
using Rebirth.Core;

namespace Rebirth.Presentation;

public sealed class UiFactory(Font body, Font title)
{
    private static int FitText(Font font, string text, Vector2 bounds, int size)
    {
        while (size > 12)
        {
            var lines = font.GetMultilineStringSize(text, HorizontalAlignment.Left, bounds.X, size);
            if (lines.Y <= bounds.Y && lines.X <= bounds.X) break;
            size--;
        }
        return size;
    }

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
        text = GameText.Get(text);
        if (GameText.IsEnglish) size = FitText(heading ? title : body, text, rectangle.Size, size);
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
        text = GameText.Get(text);
        var button = new Button { Text = text, TooltipText = text, ClipText = true, Position = rectangle.Position, Size = rectangle.Size, MouseDefaultCursorShape = Control.CursorShape.PointingHand };
        if (primary)
        {
            button.AddThemeStyleboxOverride("normal", PixelSkin.Frame("primary"));
            button.AddThemeStyleboxOverride("hover", PixelSkin.Frame("primary-hover"));
            button.AddThemeStyleboxOverride("pressed", PixelSkin.Frame("primary-pressed"));
            foreach (var state in new[] { "font_color", "font_hover_color", "font_pressed_color", "font_focus_color" })
                button.AddThemeColorOverride(state, PixelSkin.Light);
        }
        if (GameText.IsEnglish)
        {
            var size = 18;
            while (size > 12 && body.GetStringSize(text, fontSize: size).X > rectangle.Size.X - 28) size--;
            button.AddThemeFontSizeOverride("font_size", size);
        }
        button.Pressed += action;
        parent.AddChild(button);
        button.Size = rectangle.Size;
        return button;
    }
}
