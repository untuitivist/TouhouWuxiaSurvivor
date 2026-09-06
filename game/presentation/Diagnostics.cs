using Godot;
using Rebirth.Core;
using Rebirth.Diagnostics;
using NumericsVector = System.Numerics.Vector2;

namespace Rebirth.Presentation;

public partial class GameRoot
{
    private bool diagnosticMode;
    private bool smokeMode;
    private string capturePath = "";
    private int diagnosticFrames;
    private bool diagnosticFinished;

    private void InitializeDiagnostics()
    {
        var arguments = OS.GetCmdlineUserArgs();
        smokeMode = arguments.Contains("--rebirth-smoke");
        capturePath = arguments.FirstOrDefault(argument => argument.StartsWith("--rebirth-capture=", StringComparison.Ordinal))?.Split('=', 2)[1] ?? "";
        diagnosticMode = smokeMode || capturePath.Length > 0;
        if (!diagnosticMode) return;
        audio.Apply(new PlayerProfile { MusicEnabled = false, SoundEnabled = false });
        var mode = arguments.FirstOrDefault(argument => argument.StartsWith("--rebirth-screen=", StringComparison.Ordinal))?.Split('=', 2)[1] ?? "title";
        if (mode == "title") return;
        if (mode == "heroes") { ShowHeroes(); return; }
        if (mode == "help") { ShowHelp(); return; }
        if (mode == "settings") { ShowSettings(); return; }
        PrepareBattlePreview(mode);
    }

    private void PrepareBattlePreview(string mode)
    {
        StartRun(HeroKind.Reimu, 260906);
        var targetTicks = mode == "boss" ? 15300 : 4500;
        for (var index = 0; index < targetTicks; index++)
        {
            RunPilot.ResolveChoices(run!);
            run!.Step(RunPilot.Input(run, index));
            if (run.Phase is RunPhase.Won or RunPhase.Lost) break;
        }
        if (mode == "choices")
        {
            while (run!.Phase == RunPhase.Choosing) run.Choose(0);
            run.AddExperience(run.NextLevelExperience);
            run.Step(default);
        }
        if (mode == "pause") run!.TogglePause();
        if (mode == "result")
        {
            while (run!.Phase == RunPhase.Choosing) run.Choose(0);
            var boss = run.SpawnEnemy(EnemyKind.Boss, run.PlayerPosition + new NumericsVector(45, 0));
            boss.Health = 1;
            run.Projectiles.Add(new() { Position = boss.Position, Damage = 100, Radius = 40, Life = 1 });
            run.Step(default);
        }
        canvas.Clock = run!.Time;
        canvas.ResetView();
        canvas.Focused = true;
        canvas.ReceiveEvents();
        RefreshRunScreen();
    }

    public override void _Process(double delta)
    {
        if (!diagnosticMode || diagnosticFinished) return;
        diagnosticFrames++;
        if (diagnosticFrames < 24) return;
        diagnosticFinished = true;
        if (smokeMode)
        {
            try { RunUiSmokeTests(); GD.Print("REBIRTH_UI_SMOKE_PASS"); GetTree().Quit(); }
            catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
        }
        else CaptureFrame();
    }

    private async void CaptureFrame()
    {
        try
        {
            if (DisplayServer.GetName() == "headless") throw new InvalidOperationException("Capture requires a real renderer.");
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            var path = ProjectSettings.GlobalizePath(capturePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            using var image = GetViewport().GetTexture().GetImage();
            var result = image.SavePng(path);
            if (result != Error.Ok) throw new IOException($"Screenshot failed: {result}");
            GD.Print($"REBIRTH_CAPTURE_PASS {path}");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }

    private void RunUiSmokeTests()
    {
        ShowTitle();
        AssertUiBounds();
        PressButton("踏入夜境     →");
        Require(currentScreen == "heroes", "Title starts character selection");
        AssertUiBounds();
        PressButton("执此道 · 博丽灵梦");
        Require(run?.Hero == HeroKind.Reimu && currentScreen == "playing", "Character starts run");
        run!.Step(new(new NumericsVector(1, 0), false, true));
        Require(run.DashCooldown > 0, "Dash input reaches simulation");
        run.TogglePause();
        RefreshRunScreen();
        AssertUiBounds();
        PressButton("音画设置");
        Require(currentScreen == "settings" && run.Phase == RunPhase.Paused, "Settings preserves pause");
        AssertUiBounds();
        PressButton("返回");
        PressButton("继续行走");
        run.AddExperience(30);
        run.Step(default);
        RefreshRunScreen();
        Require(currentScreen == "choices", "Experience opens choices");
        AssertUiBounds();
        while (run.Phase == RunPhase.Choosing) { PressButton("[1]  领悟"); }
        Require(currentScreen == "playing", "Queued choices resume run");
        var boss = run.SpawnEnemy(EnemyKind.Boss, run.PlayerPosition + new NumericsVector(40, 0));
        boss.Health = 1;
        run.Projectiles.Add(new() { Position = boss.Position, Damage = 100, Radius = 40, Life = 1 });
        run.Step(default);
        RefreshRunScreen();
        Require(currentScreen == "result" && run.Phase == RunPhase.Won, "Boss defeat opens victory");
        AssertUiBounds();
        TestProfilePersistence(run);
        PressButton("再行一局");
        Require(run!.Kills == 0 && run.Time == 0 && run.Health == run.MaxHealth, "Replay creates clean state");
        ShowTitle();
        ShowHelp();
        AssertUiBounds();
        GD.Print("UI: title, heroes, start, dash, pause, settings, queued upgrades, victory, replay, help, viewport bounds");
    }

    private static void TestProfilePersistence(RunState victory)
    {
        var directory = ProjectSettings.GlobalizePath($"res://artifacts/profile-tests/{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "profile.json");
        var store = new ProfileStore(path);
        store.Data.MusicEnabled = false;
        store.Record(victory);
        var restored = new ProfileStore(path);
        Require(restored.Data.Victories == 1 && restored.Data.CompletedRuns == 1 && !restored.Data.MusicEnabled, "Profile round trip");
        var bytes = System.IO.File.ReadAllBytes(path);
        Require(bytes.Length > 3 && !(bytes[0] == 239 && bytes[1] == 187 && bytes[2] == 191), "Profile UTF-8 without BOM");
        var corruptPath = Path.Combine(directory, "corrupt.json");
        System.IO.File.WriteAllText(corruptPath, "{broken", new System.Text.UTF8Encoding(false));
        var corrupt = new ProfileStore(corruptPath);
        Require(corrupt.Warning.Length > 0, "Corrupt profile falls back with warning");
        corrupt.Save();
        Require(System.IO.File.ReadAllText(corruptPath, System.Text.Encoding.UTF8) == "{broken", "Corrupt original remains intact");
        GD.Print("PROFILE_PASS: round trip, preferences, victory count, UTF-8, malformed-file preservation");
    }

    private void PressButton(string text)
    {
        var button = Descendants(screen!).OfType<Button>().FirstOrDefault(candidate => candidate.Text == text);
        Require(button != null, $"Button exists: {text}");
        button!.EmitSignal(BaseButton.SignalName.Pressed);
    }

    private void AssertUiBounds()
    {
        var viewport = new Rect2(-1, -1, 1282, 722);
        foreach (var control in Descendants(screen!).OfType<Control>())
        {
            if (control is not (Button or Label)) continue;
            Require(viewport.Encloses(control.GetGlobalRect()), $"Control fits viewport: {control.Name}");
            if (control.GetParent() is Panel parent)
                Require(parent.GetGlobalRect().Grow(2).Encloses(control.GetGlobalRect()), $"Control fits card: {(control as Label)?.Text ?? control.Name}");
        }
    }

    private static IEnumerable<Node> Descendants(Node parent)
    {
        foreach (var child in parent.GetChildren())
        {
            yield return child;
            foreach (var descendant in Descendants(child)) yield return descendant;
        }
    }

    private static void Require(bool condition, string description)
    {
        if (!condition) throw new InvalidOperationException(description);
    }
}
