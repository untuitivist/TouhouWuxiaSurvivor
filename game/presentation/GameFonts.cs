using Godot;

namespace Rebirth.Presentation;

public static class GameFonts
{
    public static (Font Body, Font Title) Load()
    {
        var body = GD.Load<FontFile>("res://assets/fonts/NightJournalSans.otf");
        if (body == null) throw new InvalidOperationException("Bundled Chinese font is missing.");
        body.Antialiasing = TextServer.FontAntialiasing.Gray;
        var title = (FontFile)body.Duplicate();
        title.Antialiasing = TextServer.FontAntialiasing.None;
        return (body, title);
    }
}
