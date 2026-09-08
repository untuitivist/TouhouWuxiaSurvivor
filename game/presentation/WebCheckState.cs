namespace Rebirth.Presentation;

internal sealed class WebCheckState
{
    public string Screen { get; set; } = "";
    public string Language { get; set; } = "zh";
    public string Hero { get; set; } = "";
    public string Phase { get; set; } = "";
    public int Tick { get; set; }
    public float Time { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public bool Focused { get; set; }
    public float DashCooldown { get; set; }
    public float MoveX { get; set; }
    public float MoveY { get; set; }
    public bool TouchVisible { get; set; }
    public float MinimapX { get; set; }
    public float MinimapY { get; set; }
    public float MinimapWidth { get; set; }
    public float MinimapHeight { get; set; }
    public bool DebugVisible { get; set; }
    public bool Persistent { get; set; }
    public string Warning { get; set; } = "";
    public float MasterVolume { get; set; }
    public int CompletedRuns { get; set; }
    public bool BossSpawned { get; set; }
    public bool BossPresent { get; set; }
    public bool Pilot { get; set; }
    public bool HasChineseGlyphs { get; set; }
    public bool TouchFocus { get; set; }
    public int RenderWidth { get; set; }
    public int RenderHeight { get; set; }
    public double Fps { get; set; }
    public double DrawCalls { get; set; }
    public double BatchMilliseconds { get; set; }
    public int BatchInstances { get; set; }
    public double SimulationMilliseconds { get; set; }
    public double EventMilliseconds { get; set; }
    public int Enemies { get; set; }
    public int Projectiles { get; set; }
    public int Pickups { get; set; }
    public double[] SystemMilliseconds { get; set; } = [];
    public WebCheckControl[] Controls { get; set; } = [];
}

internal sealed class WebCheckControl
{
    public string Name { get; set; } = "";
    public string Text { get; set; } = "";
    public string Kind { get; set; } = "";
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
    public double Value { get; set; }
}
