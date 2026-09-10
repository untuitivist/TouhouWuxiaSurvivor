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
    private bool webArtPreview;
    private double webCheckSeconds;
    private CombatStressScenario? webCombatStress;
    private double simulationMilliseconds;
    private double eventMilliseconds;

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
        var language = arguments.FirstOrDefault(argument => argument.StartsWith("--web-language="))?.Split('=', 2)[1];
        if (language != null) { profile.Data.Language = GameText.NormalizeLanguage(language); GameText.SetLanguage(profile.Data.Language); ShowTitle(); }
        webPilot = arguments.Contains("--web-pilot");
        var fixture = arguments.FirstOrDefault(argument => argument.StartsWith("--web-fixture=", StringComparison.Ordinal))?.Split('=', 2)[1] ?? "";
        webPerformance = fixture == "performance";
        if (fixture == "growth-choices") { PrepareGrowthPreview(); return; }
        if (PrepareMarisaGrowthFixture(fixture)) return;
        webArtPreview = fixture is "reimu-field" or "reimu-spell" or "marisa-stars" or "marisa-warmup" or "marisa-beam";
        if (webPilot || fixture.Length > 0)
        {
            StartRun(arguments.Contains("--web-marisa") ? HeroKind.Marisa : HeroKind.Reimu, 42);
            if (fixture == "choices") { run!.AddExperience(30); run.Step(default); RefreshRunScreen(); }
            if (fixture == "boss") { run!.SpawnEnemy(EnemyKind.Boss, new(350, 0)); RefreshRunScreen(); }
            if (webPerformance) PreparePerformancePreview();
            if (fixture == "combat-performance")
            {
                var load = arguments.FirstOrDefault(argument => argument.StartsWith("--web-load="))?.Split('=', 2)[1];
                webCombatStress = new(run!, int.TryParse(load, out var count) ? count : 320);
            }
            if (webArtPreview) PrepareAbilityPreview(fixture);
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
            SimulationMilliseconds = simulationMilliseconds, EventMilliseconds = eventMilliseconds,
            Enemies = run?.Enemies.Count ?? 0, Projectiles = run?.Projectiles.Count ?? 0, Pickups = run?.Pickups.Count ?? 0,
            SystemMilliseconds = run?.Timings?.Milliseconds ?? [],
            Screen = currentScreen, Hero = run?.Hero.ToString() ?? "", Phase = run?.Phase.ToString() ?? "",
            Language = GameText.Language,
            AbilityRanks = run?.Ranks ?? [], Traits = (int)(run?.Build.Traits ?? AbilityTraits.None),
            SignatureUnlocked = run?.Build.SignatureUnlocked ?? false,
            ChoiceIds = run?.Choices.Select(upgrade => upgrade.Id).ToArray() ?? [],
            Tick = run?.Ticks ?? 0, Time = run?.Time ?? 0, X = run?.PlayerPosition.X ?? 0, Y = run?.PlayerPosition.Y ?? 0,
            Focused = run?.Focused ?? false, DashCooldown = run?.DashCooldown ?? 0, MoveX = touchHud.Movement.X, MoveY = touchHud.Movement.Y,
            TouchVisible = touchHud.Visible, TouchFocus = touchHud.FocusHeld, DebugVisible = debugOverlay.Visible,
            MinimapX = canvas.MinimapBounds.Position.X, MinimapY = canvas.MinimapBounds.Position.Y,
            MinimapWidth = canvas.MinimapBounds.Size.X, MinimapHeight = canvas.MinimapBounds.Size.Y,
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
