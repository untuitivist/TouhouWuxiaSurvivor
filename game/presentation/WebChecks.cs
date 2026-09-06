using System.Diagnostics;
using System.Text.Json;
using Godot;
using Rebirth.Core;
using Rebirth.Diagnostics;

namespace Rebirth.Presentation;

public partial class GameRoot
{
    private bool webChecks;
    private bool webPilot;
    private bool webPerformance;
    private double webCheckSeconds;

    private ProfileStore CreateProfile(string[] arguments)
    {
        if (GamePlatform.IsWeb && arguments.Contains("--web-validation"))
            return new("user://rebirth/diagnostics/web-validation/profile.json");
        return diagnosticMode ? new(ProjectSettings.GlobalizePath($"user://rebirth/diagnostics/sessions/{Guid.NewGuid():N}/profile.json")) : new();
    }

    private void InitializeWebChecks()
    {
        var arguments = OS.GetCmdlineUserArgs();
        webChecks = GamePlatform.IsWeb && arguments.Contains("--web-validation");
        if (!webChecks) return;
        webPilot = arguments.Contains("--web-pilot");
        var fixture = arguments.FirstOrDefault(argument => argument.StartsWith("--web-fixture=", StringComparison.Ordinal))?.Split('=', 2)[1] ?? "";
        webPerformance = fixture == "performance";
        if (webPilot || fixture.Length > 0)
        {
            StartRun(arguments.Contains("--web-marisa") ? HeroKind.Marisa : HeroKind.Reimu, 42);
            if (fixture == "choices") { run!.AddExperience(30); run.Step(default); RefreshRunScreen(); }
            if (fixture == "boss") { run!.SpawnEnemy(EnemyKind.Boss, new(350, 0)); RefreshRunScreen(); }
            if (webPerformance) PreparePerformancePreview();
        }
        GD.Print("SHARED_WEB_CHECKS_READY");
    }

    private void UpdateWebChecks(double delta)
    {
        if (!webChecks) return;
        if (webPilot && run != null && run.Phase is RunPhase.Playing or RunPhase.Choosing)
        {
            var started = Stopwatch.GetTimestamp();
            for (var step = 0; step < 120; step++)
            {
                RunPilot.ResolveChoices(run);
                run.Step(RunPilot.Input(run, run.Ticks));
                if (run.Phase is RunPhase.Won or RunPhase.Lost || Stopwatch.GetElapsedTime(started).TotalMilliseconds >= 16) break;
            }
            if (displayedPhase != run.Phase) RefreshRunScreen();
        }
        webCheckSeconds += delta;
        if (webCheckSeconds < 0.1) return;
        webCheckSeconds = 0;
        var state = new WebCheckState
        {
            RenderWidth = (int)GetViewport().GetTexture().GetSize().X, RenderHeight = (int)GetViewport().GetTexture().GetSize().Y,
            Fps = Engine.GetFramesPerSecond(), DrawCalls = Performance.GetMonitor(Performance.Monitor.RenderTotalDrawCallsInFrame),
            BatchMilliseconds = canvas.BatchBuildMilliseconds, BatchInstances = canvas.VisibleBatchInstances,
            Screen = currentScreen, Hero = run?.Hero.ToString() ?? "", Phase = run?.Phase.ToString() ?? "",
            Tick = run?.Ticks ?? 0, Time = run?.Time ?? 0, X = run?.PlayerPosition.X ?? 0, Y = run?.PlayerPosition.Y ?? 0,
            Focused = run?.Focused ?? false, DashCooldown = run?.DashCooldown ?? 0, MoveX = touchHud.Movement.X, MoveY = touchHud.Movement.Y,
            TouchVisible = touchHud.Visible, TouchFocus = touchHud.FocusHeld, DebugVisible = debugOverlay.Visible,
            Persistent = OS.IsUserfsPersistent(), Warning = profile.Notice, MasterVolume = profile.Data.MasterVolume,
            CompletedRuns = profile.Data.CompletedRuns, BossSpawned = run?.BossSpawned ?? false, BossPresent = run?.Boss != null, Pilot = webPilot,
            HasChineseGlyphs = "博丽灵梦雾雨魔理沙夜境异闻".All(character => canvas.BodyFont.HasChar(character)),
            Controls = screen == null ? [] : Descendants(screen).OfType<Control>().Where(control => control.IsVisibleInTree() && control is Button or HSlider).Select(control =>
            {
                var rectangle = control.GetGlobalRect();
                return new WebCheckControl { Name = control.Name, Text = control is Button button ? button.Text : "", Kind = control.GetType().Name, X = rectangle.Position.X, Y = rectangle.Position.Y, Width = rectangle.Size.X, Height = rectangle.Size.Y, Value = control is HSlider slider ? slider.Value : 0 };
            }).ToArray()
        };
        JavaScriptBridge.Eval("window.__touhouProbe=" + JsonSerializer.Serialize(state, ProfileJsonContext.Default.WebCheckState) + ";void 0;", true);
    }
}
