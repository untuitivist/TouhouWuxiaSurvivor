using Godot;

namespace Rebirth.Presentation;

public sealed class VideoPreferences
{
    public int WindowMode { get; set; }
    public int Width { get; set; } = 1280;
    public int Height { get; set; } = 720;
    public bool Vsync { get; set; } = true;
    public int MaxFps { get; set; } = 60;

    public static readonly Vector2I[] Resolutions = [new(640, 360), new(960, 540), new(1280, 720), new(1600, 900), new(1920, 1080), new(2560, 1440), new(3840, 2160)];
    public static readonly int[] FrameLimits = [30, 60, 120, 144, 0];
    public VideoPreferences Copy() => (VideoPreferences)MemberwiseClone();

    public void Normalize()
    {
        if (WindowMode is < 0 or > 2) WindowMode = 0;
        if (!Resolutions.Contains(new(Width, Height))) { Width = 1280; Height = 720; }
        if (!FrameLimits.Contains(MaxFps))
            MaxFps = FrameLimits.Where(value => value > 0).OrderBy(value => Math.Abs((long)value - MaxFps)).ThenBy(value => value).First();
    }

    public void Apply()
    {
        Normalize();
        Engine.MaxFps = MaxFps;
        if (!GamePlatform.CanResizeWindow) return;
        DisplayServer.WindowSetVsyncMode(Vsync ? DisplayServer.VSyncMode.Enabled : DisplayServer.VSyncMode.Disabled);
        var window = ((SceneTree)Engine.GetMainLoop()).Root;
        var monitor = window.CurrentScreen;
        window.Mode = Window.ModeEnum.Windowed;
        window.Borderless = WindowMode == 1;
        window.MinSize = new(640, 360);
        if (WindowMode == 2) { window.Mode = Window.ModeEnum.Fullscreen; return; }
        var usable = DisplayServer.ScreenGetUsableRect(monitor);
        var size = WindowMode == 1 ? usable.Size : new Vector2I(Math.Min(Width, usable.Size.X), Math.Min(Height, usable.Size.Y));
        window.Size = size;
        window.Position = usable.Position + (usable.Size - size) / 2;
        if (WindowMode == 1) window.Mode = Window.ModeEnum.Maximized;
    }
}
