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

    public override void _Ready()
    {
        DisplayServer.WindowSetTitle("幻想乡 · 夜境异闻");
        var body = new SystemFont { FontNames = ["Microsoft YaHei UI", "Microsoft YaHei", "Noto Sans CJK SC", "sans-serif"], Antialiasing = TextServer.FontAntialiasing.Lcd };
        var title = new SystemFont { FontNames = ["KaiTi", "STKaiti", "Noto Serif CJK SC", "serif"], Antialiasing = TextServer.FontAntialiasing.Lcd };
        canvas.BodyFont = body;
        canvas.TitleFont = title;
        AddChild(canvas);
        AddChild(audio);
        ui = new(body, title);
        profile = new();
        GameControls.Configure();
        audio.Apply(profile.Data);
        canvas.ReducedMotion = profile.Data.ReducedMotion;
        var layer = new CanvasLayer();
        AddChild(layer);
        layer.AddChild(interfaceRoot);
        interfaceRoot.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        interfaceRoot.Theme = ui.CreateTheme();
        ShowTitle();
        InitializeDiagnostics();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (diagnosticMode) return;
        if (run == null) return;
        canvas.Focused = Input.IsActionPressed(GameControls.Focus);
        if (run.Phase == RunPhase.Playing)
        {
            float horizontal = Input.GetAxis(GameControls.Left, GameControls.Right);
            float vertical = Input.GetAxis(GameControls.Up, GameControls.Down);
            run.Step(new(new NumericsVector(horizontal, vertical), canvas.Focused, dashRequested));
            canvas.ReceiveEvents();
            audio.PlayEvents(run.Events);
        }
        dashRequested = false;
        if (displayedPhase != run.Phase) RefreshRunScreen();
    }

    public override void _UnhandledInput(InputEvent input)
    {
        if (input is not InputEventKey { Pressed: true, Echo: false } key) return;
        var code = key.PhysicalKeycode == Key.None ? key.Keycode : key.PhysicalKeycode;
        if (run?.Phase == RunPhase.Playing && input.IsActionPressed(GameControls.Dash)) { dashRequested = true; GetViewport().SetInputAsHandled(); }
        if (run?.Phase == RunPhase.Choosing && currentScreen == "choices")
        {
            var index = code switch { Key.Key1 or Key.Kp1 => 0, Key.Key2 or Key.Kp2 => 1, Key.Key3 or Key.Kp3 => 2, _ => -1 };
            if (index >= 0) { SelectArt(index); GetViewport().SetInputAsHandled(); }
        }
    }

    public override void _Notification(int notification)
    {
        if (notification != NotificationWMWindowFocusOut || diagnosticMode || run?.Phase != RunPhase.Playing) return;
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
        if (screen != null) { interfaceRoot.RemoveChild(screen); screen.QueueFree(); }
        screen = new Control { Size = new(1280, 720), MouseFilter = Control.MouseFilterEnum.Ignore };
        interfaceRoot.AddChild(screen);
        currentScreen = name;
    }

    private Control Modal(string name, string eyebrow, string heading, int width = 1080, int height = 540)
    {
        dashRequested = false;
        ClearScreen(name);
        var shade = new ColorRect { Color = new(0.025f, 0.045f, 0.06f, 0.83f), Size = new(1280, 720), MouseFilter = Control.MouseFilterEnum.Stop };
        screen!.AddChild(shade);
        var panel = ui.Panel(screen, new((1280 - width) / 2, (720 - height) / 2, width, height));
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
