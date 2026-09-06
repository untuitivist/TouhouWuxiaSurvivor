using Godot;

namespace Rebirth.Presentation;

public static class PixelTheme
{
    public static Theme Create(Font body)
    {
        var theme = new Theme { DefaultFont = body, DefaultFontSize = 18 };
        foreach (var type in new[] { "Button", "OptionButton", "PopupMenu" })
        {
            foreach (var state in new[] { "font_color", "font_hover_color", "font_pressed_color", "font_focus_color" }) theme.SetColor(state, type, PixelSkin.Ink);
            theme.SetColor("font_disabled_color", type, PixelSkin.Muted);
            theme.SetStylebox("normal", type, PixelSkin.Frame("button"));
            theme.SetStylebox("hover", type, PixelSkin.Frame("hover"));
            theme.SetStylebox("pressed", type, PixelSkin.Frame("pressed"));
            theme.SetStylebox("disabled", type, PixelSkin.Frame("disabled"));
            theme.SetStylebox("focus", type, PixelSkin.Frame("focus"));
        }
        theme.SetColor("font_color", "Label", PixelSkin.Ink);
        theme.SetColor("default_color", "RichTextLabel", PixelSkin.Ink);
        theme.SetColor("font_selected_color", "PopupMenu", PixelSkin.Ink);
        theme.SetStylebox("panel", "PopupMenu", PixelSkin.Frame("panel"));
        theme.SetStylebox("panel", "TooltipPanel", PixelSkin.Frame("panel"));
        theme.SetColor("font_color", "TooltipLabel", PixelSkin.Ink);
        theme.SetIcon("arrow", "OptionButton", PixelSkin.Icon("arrow"));
        theme.SetStylebox("slider", "HSlider", PixelSkin.Frame("track"));
        theme.SetStylebox("grabber_area", "HSlider", PixelSkin.Frame("fill"));
        theme.SetStylebox("grabber_area_highlight", "HSlider", PixelSkin.Frame("fill"));
        theme.SetIcon("grabber", "HSlider", PixelSkin.Icon("thumb"));
        theme.SetIcon("grabber_highlight", "HSlider", PixelSkin.Icon("thumb-hover"));
        theme.SetIcon("grabber_disabled", "HSlider", PixelSkin.Icon("thumb"));
        theme.SetConstant("grabber_offset", "HSlider", 0);
        foreach (var type in new[] { "VScrollBar", "HScrollBar" })
        {
            theme.SetStylebox("scroll", type, PixelSkin.Frame("track"));
            theme.SetStylebox("grabber", type, PixelSkin.Frame("button"));
            theme.SetStylebox("grabber_highlight", type, PixelSkin.Frame("hover"));
            theme.SetStylebox("grabber_pressed", type, PixelSkin.Frame("pressed"));
        }
        return theme;
    }
}
