using Godot;
using Rebirth.Core;
using NumericsVector = System.Numerics.Vector2;

namespace Rebirth.Presentation;

public partial class GameRoot : Node
{
    private readonly GameCanvas canvas = new();
    private readonly GameAudio audio = new();
    private readonly Control interfaceRoot = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
    private Control? screen;
    private UiFactory ui = null!;
    private ProfileStore profile = null!;
    private RunState? run;
    private RunPhase displayedPhase;
    private HeroKind lastHero;
    private string currentScreen = "";
    private bool dashRequested;
    private bool recorded;
    private int seedCounter;
    private readonly TouchHud touchHud = new();

    public override void _Ready()
    {
        DisplayServer.WindowSetTitle("幻想乡 · 夜境异闻");
        if (GamePlatform.IsWeb) GetWindow().ContentScaleMode = Window.ContentScaleModeEnum.Viewport;
        var (body, title) = GameFonts.Load();
        canvas.BodyFont = body;
        canvas.TitleFont = title;
        AddChild(canvas);
        AddChild(audio);
        ui = new(body, title);
        var arguments = OS.GetCmdlineUserArgs();
        diagnosticMode = arguments.Contains("--rebirth-smoke") || arguments.Contains("--rebirth-video-smoke") || arguments.Contains("--rebirth-batch-smoke") || arguments.Any(argument => argument.StartsWith("--rebirth-capture=", StringComparison.Ordinal));
        profile = CreateProfile(arguments);
        if (!diagnosticMode) profile.Data.Video.Apply();
        else { profile.Data.MusicEnabled = false; profile.Data.SoundEnabled = false; }
        GameControls.Configure(profile.Data.Bindings);
        audio.Apply(profile.Data);
        canvas.ReducedMotion = profile.Data.ReducedMotion;
        var layer = new CanvasLayer();
        AddChild(layer);
        layer.AddChild(interfaceRoot);
        interfaceRoot.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        interfaceRoot.Theme = ui.CreateTheme();
        interfaceRoot.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
        touchHud.BodyFont = body;
        touchHud.DashPressed = () => dashRequested = true;
        touchHud.PausePressed = NavigateBack;
        touchHud.InspectPressed = OpenBuild;
        layer.AddChild(touchHud);
        touchHud.VisibilityChanged += () => canvas.SetTouchHudVisible(touchHud.Visible);
        InitializeDebugOverlay(layer, body);
        ShowTitle();
        InitializeDiagnostics();
        InitializeWebChecks();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (diagnosticMode || webPilot || webPerformance || webArtPreview) return;
        if (run == null) return;
        canvas.Focused = Input.IsActionPressed(GameControls.Focus) || touchHud.FocusHeld;
        if (run.Phase == RunPhase.Playing)
        {
            float horizontal = Input.GetAxis(GameControls.Left, GameControls.Right) + touchHud.Movement.X;
            float vertical = Input.GetAxis(GameControls.Up, GameControls.Down) + touchHud.Movement.Y;
            webCombatStress?.Refill(run);
            var started = webChecks ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
            run.Step(new(new NumericsVector(horizontal, vertical), canvas.Focused, dashRequested));
            if (webChecks) simulationMilliseconds = System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            started = webChecks ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
            canvas.ReceiveEvents();
            audio.PlayEvents(run.Events);
            if (webChecks) eventMilliseconds = System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        }
        dashRequested = false;
        if (displayedPhase != run.Phase) RefreshRunScreen();
    }

    public override void _Notification(int notification)
    {
        if (notification != NotificationWMWindowFocusOut && notification != NotificationApplicationPaused) return;
        touchHud.ResetPointers();
        dashRequested = false;
        if (diagnosticMode || run?.Phase != RunPhase.Playing) return;
        run.TogglePause();
        RefreshRunScreen();
    }

    private void StartRun(HeroKind hero, int? seed = null)
    {
        lastHero = hero;
        run = new(hero, seed ?? unchecked((int)Time.GetTicksUsec() + ++seedCounter * 7919));
        canvas.Run = run;
        canvas.ResetView();
        dashRequested = false;
        recorded = false;
        RefreshRunScreen();
    }

    private void ClearScreen(string name)
    {
        if (screen != null) { screen.Hide(); screen.QueueFree(); }
        screen = new Control { Size = new(1280, 720), MouseFilter = Control.MouseFilterEnum.Ignore };
        interfaceRoot.AddChild(screen);
        currentScreen = name;
        touchHud.SetContext(name == "playing", profile?.Data.TouchMode ?? 0);
    }

    private Control Modal(string name, string eyebrow, string heading, int width = 1080, int height = 540)
    {
        dashRequested = false;
        ClearScreen(name);
        var shade = new ColorRect { Color = new(0.025f, 0.045f, 0.06f, 0.83f), Size = new(1280, 720), MouseFilter = Control.MouseFilterEnum.Stop };
        screen!.AddChild(shade);
        var panel = ui.Panel(screen, new((1280 - width) / 2, (720 - height) / 2, width, height));
        var spray = new TextureRect { Texture = PixelSkin.Artwork("panel-spray"), Position = new(width - 280, 22), Size = new(180, 96), ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, Modulate = new(1, 1, 1, 0.2f), MouseFilter = Control.MouseFilterEnum.Ignore };
        panel.AddChild(spray);
        var bookmark = new TextureRect { Texture = PixelSkin.Artwork("bookmark"), Position = new(width - 80, 8), Size = new(28, 44), MouseFilter = Control.MouseFilterEnum.Ignore };
        panel.AddChild(bookmark);
        ui.Label(bookmark, "夜", new(6, 6, 18, 23), 14).AddThemeColorOverride("font_color", PixelSkin.Light);
        var stitch = new TextureRect { Texture = PixelSkin.Artwork("divider"), Position = new(36, 117), Size = new(width - 72, 4), StretchMode = TextureRect.StretchModeEnum.Tile, ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, MouseFilter = Control.MouseFilterEnum.Ignore };
        panel.AddChild(stitch);
        ui.Label(panel, eyebrow, new(36, 24, width - 72, 25), 13, Palette.Gold);
        ui.Label(panel, heading, new(34, 60, width - 68, 55), 38, Palette.Paper, true);
        return panel;
    }

    private void RefreshRunScreen()
    {
        if (run == null) return;
        displayedPhase = run.Phase;
        switch (run.Phase)
        {
            case RunPhase.Playing: ClearScreen("playing"); break;
            case RunPhase.Choosing: ShowChoices(); break;
            case RunPhase.Paused: ShowPause(); break;
            default: ShowResult(); break;
        }
    }

    private void SelectArt(int index)
    {
        if (run?.Choose(index) == true) RefreshRunScreen();
    }
}
