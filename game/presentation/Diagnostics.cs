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
        diagnosticMode = smokeMode || capturePath.Length > 0 || arguments.Contains("--rebirth-video-smoke") || arguments.Contains("--rebirth-batch-smoke");
        if (!diagnosticMode) return;
        if (smokeMode) { profile.Data.MusicEnabled = false; profile.Data.SoundEnabled = false; }
        canvas.ReducedMotion = profile.Data.ReducedMotion;
        audio.Apply(new PlayerProfile { MusicEnabled = false, SoundEnabled = false });
        if (arguments.Contains("--rebirth-batch-smoke")) { diagnosticFinished = true; _ = TestSpriteBatchRendering(); return; }
        var mode = arguments.FirstOrDefault(argument => argument.StartsWith("--rebirth-screen=", StringComparison.Ordinal))?.Split('=', 2)[1] ?? "title";
        if (mode == "debug-title") { ShowTitle(); ToggleDebug(); return; }
        if (mode == "debug-combat") { PrepareBattlePreview("boss"); ToggleDebug(); return; }
        if (mode == "title") return;
        if (mode == "performance") { PreparePerformancePreview(); return; }
        if (mode == "heroes") { ShowHeroes(); return; }
        if (mode == "help") { ShowHelp(); return; }
        if (mode == "settings") { ShowSettings(); return; }
        if (mode is "settings-video" or "settings-controls" or "settings-confirm")
        {
            ShowSettings();
            settingsTab = mode == "settings-controls" ? 2 : 1;
            BuildSettings();
            if (mode == "settings-confirm")
            {
                var size = DisplayServer.WindowGetSize();
                BeginVideoPreview(new() { Width = size.X, Height = size.Y }, true);
            }
            return;
        }
        if (mode == "changelog") { ShowChangelog(); return; }
        if (mode == "journal") { OpenJournal(); return; }
        if (mode == "journal-detail") { ShowJournalDetail(JournalCatalog.All.Single(entry => entry.Id == "art-MasterSpark")); return; }
        if (mode == "growth-choices") { PrepareGrowthPreview(); return; }
        if (mode == "growth-orbit") { PrepareGrowthCombatPreview(); return; }
        PrepareBattlePreview(mode);
    }

    private void PrepareBattlePreview(string mode)
    {
        StartRun(mode.StartsWith("marisa", StringComparison.Ordinal) ? HeroKind.Marisa : HeroKind.Reimu, 260906);
        var targetTicks = mode == "boss" ? 15300 : 4500;
        for (var index = 0; index < targetTicks; index++)
        {
            RunPilot.ResolveChoices(run!);
            run!.Step(RunPilot.Input(run, index));
            if (run.Phase is RunPhase.Won or RunPhase.Lost) break;
        }
        if (mode is "choices" or "marisa-choices")
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
        if (mode is "build" or "build-max" or "marisa-build")
        {
            while (run.Phase == RunPhase.Choosing) run.Choose(0);
            if (mode == "build-max")
                foreach (var art in ArtCatalog.All.Where(art => art.Id != ArtKind.Recovery && ArtCatalog.Available(run.Hero, art.Id))) run.Ranks[(int)art.Id] = art.MaxRank;
            RefreshRunScreen();
            OpenBuild();
        }
        if (mode is "reimu-spell" or "reimu-field" or "marisa-beam" or "marisa-stars" or "marisa-warmup") PrepareAbilityPreview(mode);
    }

    private void PrepareAbilityPreview(string mode)
    {
        StartRun(mode.StartsWith("marisa", StringComparison.Ordinal) ? HeroKind.Marisa : HeroKind.Reimu, 260906);
        foreach (var art in ArtCatalog.Abilities(run!.Hero)) run.Ranks[(int)art.Id] = 3;
        foreach (var offset in new[] { new NumericsVector(230, -80), new NumericsVector(420, -140), new NumericsVector(-180, 110) })
        {
            var enemy = run.SpawnEnemy(EnemyKind.Elite, offset);
            enemy.Health = enemy.MaxHealth = 10000;
            enemy.Speed = 0;
        }
        if (mode == "reimu-spell") run.Build.TryApply(UpgradeCatalog.Get(UpgradeCatalog.DreamSeal), 5);
        if (run.Hero == HeroKind.Reimu) run.Build.TryApply(UpgradeCatalog.Get(UpgradeCatalog.Homing), 3);
        if (mode == "reimu-spell")
            for (var index = 0; index < 20; index++) run.Projectiles.Add(new() { Position = Geometry.Angle(index * MathF.Tau / 20) * 25, Hostile = true, Radius = 1, Life = 2 });
        var ticks = mode == "marisa-warmup" ? 8 : mode == "reimu-spell" ? 14 : mode == "marisa-stars" ? 18 : 45;
        for (var index = 0; index < ticks; index++) run.Step(new(mode == "reimu-field" ? new NumericsVector(0, 1) : NumericsVector.Zero));
        canvas.Clock = run.Time;
        canvas.ResetView();
        canvas.ReceiveEvents();
        RefreshRunScreen();
    }

    public override void _Process(double delta)
    {
        UpdatePlatformLayout();
        UpdateWebChecks(delta);
        UpdateDebugState();
        TickVideoPreview(delta);
        if (!diagnosticMode || diagnosticFinished) return;
        diagnosticFrames++;
        RecordRenderFrame(delta);
        if (performancePreview ? performanceSeconds < 4 : diagnosticFrames < 24) return;
        diagnosticFinished = true;
        if (OS.GetCmdlineUserArgs().Contains("--rebirth-video-smoke")) { RunDisplaySmokeTests(); return; }
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
            SaveRenderReport(path);
            GD.Print($"REBIRTH_CAPTURE_PASS {path}");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }

    private void RunUiSmokeTests()
    {
        TestSharedPlatform();
        TestPixelUiAndDebug();
        TestRenderInvalidation();
        TestJournal();
        ShowTitle();
        AssertUiBounds();
        PressButton("踏入夜境     →");
        Require(currentScreen == "heroes", "Title starts character selection");
        AssertUiBounds();
        PressButton("执此道 · 博丽灵梦");
        Require(run?.Hero == HeroKind.Reimu && currentScreen == "playing", "Character starts run");
        TestNavigation();
        run!.Step(new(new NumericsVector(1, 0), false, true));
        Require(run.DashCooldown > 0, "Dash input reaches simulation");
        run.TogglePause();
        RefreshRunScreen();
        AssertUiBounds();
        PressButton("游戏设置");
        Require(currentScreen == "settings" && run.Phase == RunPhase.Paused, "Settings preserves pause");
        AssertUiBounds();
        var master = Descendants(screen!).OfType<HSlider>().Single(slider => slider.Name == "master_volume");
        master.Value = 0;
        Require(AudioServer.IsBusMute(0), "Zero master volume really mutes audio");
        master.Value = 37;
        Require(!AudioServer.IsBusMute(0) && Math.Abs(profile.Data.MasterVolume - 0.37f) < 0.001f, "Volume changes apply without rebuilding screen");
        TestFullSettings();
        PressButton("返回");
        PressButton("继续行走");
        run.AddExperience(30);
        run.Step(default);
        RefreshRunScreen();
        Require(currentScreen == "choices", "Experience opens choices");
        AssertUiBounds();
        var originalChoices = run.Choices.ToArray();
        var pending = run.PendingChoices;
        PressKey(Key.E);
        Require(currentScreen == "build" && run.Phase == RunPhase.Choosing, "Upgrade inspection retains choosing state");
        AssertUiBounds();
        PressKey(Key.Key1);
        Require(run.PendingChoices == pending && run.Choices.SequenceEqual(originalChoices), "Hidden choices cannot be selected through inspection");
        PressKey(Key.Escape);
        Require(currentScreen == "choices" && run.Choices.SequenceEqual(originalChoices), "Inspection returns to unchanged offers");
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
        ShowTitle();
        PressButton("更新记录");
        AssertUiBounds();
        var history = Descendants(screen!).OfType<RichTextLabel>().Single();
        Require(history.GetParsedText().Contains(ProjectSettings.GetSetting("application/config/version").AsString()), "Changelog initially shows current release");
        var versions = Descendants(screen!).OfType<OptionButton>().Single();
        Require(versions.ItemCount >= 9, "Historical releases are individually selectable");
        versions.EmitSignal(OptionButton.SignalName.ItemSelected, versions.ItemCount - 2);
        Require(history.GetParsedText().Contains("未发布"), "Unreleased bucket remains separately accessible after release promotion");
        versions.EmitSignal(OptionButton.SignalName.ItemSelected, 0);
        Require(history.GetParsedText().Contains(ProjectSettings.GetSetting("application/config/version").AsString()), "Selecting the current release restores its own notes");
        var settingsRelease = Enumerable.Range(0, versions.ItemCount).Single(index => versions.GetItemText(index) == "alpha-0.0.9");
        versions.EmitSignal(OptionButton.SignalName.ItemSelected, settingsRelease);
        Require(history.GetParsedText().Contains("完整设置回归") && history.GetParsedText().Contains("F3"), "Historical settings and debug release notes remain available");
        versions.EmitSignal(OptionButton.SignalName.ItemSelected, versions.ItemCount - 1);
        Require(history.GetParsedText().Contains("alpha-0.0.0") && history.GetParsedText().Contains("alpha-0.0.5"), "Embedded complete history remains accessible");
        PressKey(Key.Escape);
        Require(currentScreen == "title", "Changelog returns to title");
        ShowHeroes();
        PressButton("执此道 · 雾雨魔理沙");
        PressKey(Key.E);
        var buildText = string.Join(" ", Descendants(screen!).OfType<Label>().Select(label => label.Text));
        Require(buildText.Contains("Master Spark") && buildText.Contains("星光射击") && !buildText.Contains("追踪御札"), "Marisa inspection displays only her abilities");
        AssertUiBounds();
        PressKey(Key.Escape);
        run!.AddExperience(30);
        run.Step(default);
        RefreshRunScreen();
        Require(run.Choices.All(art => ArtCatalog.Available(HeroKind.Marisa, art.Ability)), "Marisa choices are character-owned");
        AssertUiBounds();
        ShowTitle();
        GD.Print("UI: navigation, inspection, preserved offers, audio sliders, embedded history, title, heroes, dash, victory, replay, help, viewport bounds");
    }

    private void TestNavigation()
    {
        Require(InputMap.ActionGetEvents(GameControls.Left).Count == 2 && InputMap.ActionGetEvents(GameControls.Pause).Count == 2, "Movement and pause retain dual defaults");
        PressKey(Key.P);
        Require(currentScreen == "pause" && run!.Phase == RunPhase.Paused, "P pauses from combat");
        PressButton("结束本局");
        PressKey(Key.Escape);
        Require(currentScreen == "pause" && run!.Phase == RunPhase.Paused, "Escape cancels abandon without resuming combat");
        PressButton("更新记录");
        PressKey(Key.Escape);
        Require(currentScreen == "pause" && run!.Phase == RunPhase.Paused, "Pause changelog returns to pause");
        PressKey(Key.E);
        Require(currentScreen == "build", "E opens build despite focused pause button");
        AssertUiBounds();
        PressKey(Key.E);
        Require(currentScreen == "pause", "Pause inspection does not resume combat");
        PressKey(Key.P);
        Require(currentScreen == "playing", "P resumes pause");
        dashRequested = true;
        PressKey(Key.E);
        var ticks = run!.Ticks;
        run.Step(default);
        Require(currentScreen == "build" && run.Ticks == ticks && !dashRequested, "Combat inspection freezes simulation and clears queued dash");
        PressKey(Key.Escape);
        Require(currentScreen == "playing" && run.Phase == RunPhase.Playing, "Direct combat inspection returns to combat");
    }

    private void PressKey(Key code)
    {
        GetViewport().PushInput(new InputEventKey { PhysicalKeycode = code, Keycode = code, Pressed = true });
        GetViewport().PushInput(new InputEventKey { PhysicalKeycode = code, Keycode = code, Pressed = false });
    }

    private static void TestProfilePersistence(RunState victory)
    {
        var directory = ProjectSettings.GlobalizePath($"user://rebirth/diagnostics/profile-tests/{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "profile.json");
        var store = new ProfileStore(path);
        store.Data.MusicEnabled = false;
        store.Data.MasterVolume = 0.37f;
        store.Data.MusicVolume = 0.52f;
        store.Data.SoundVolume = 0;
        store.Record(victory);
        var restored = new ProfileStore(path);
        Require(restored.Data.Victories == 1 && restored.Data.CompletedRuns == 1 && !restored.Data.MusicEnabled, "Profile round trip");
        Require(restored.Data.MasterVolume == 0.37f && restored.Data.MusicVolume == 0.52f && restored.Data.SoundVolume == 0, "Volume preferences round trip");
        var legacyPath = Path.Combine(directory, "legacy.json");
        System.IO.File.WriteAllText(legacyPath, "{\"Version\":1,\"BestKills\":42,\"MusicEnabled\":false}", new System.Text.UTF8Encoding(false));
        var legacy = new ProfileStore(legacyPath);
        Require(legacy.Data.BestKills == 42 && !legacy.Data.MusicEnabled && legacy.Data.MasterVolume == 1 && legacy.Data.MusicVolume == 1 && legacy.Data.SoundVolume == 1, "Old profiles retain records and gain default volumes");
        var rangePath = Path.Combine(directory, "range.json");
        System.IO.File.WriteAllText(rangePath, "{\"Version\":1,\"BestKills\":42,\"MasterVolume\":-5,\"SoundVolume\":20}", new System.Text.UTF8Encoding(false));
        var range = new ProfileStore(rangePath);
        Require(range.Data.BestKills == 42 && range.Data.MasterVolume == 0 && range.Data.SoundVolume == 1, "Out-of-range volumes are clamped without discarding records");
        var bytes = System.IO.File.ReadAllBytes(path);
        Require(bytes.Length > 3 && !(bytes[0] == 239 && bytes[1] == 187 && bytes[2] == 191), "Profile UTF-8 without BOM");
        var corruptPath = Path.Combine(directory, "corrupt.json");
        System.IO.File.WriteAllText(corruptPath, "{broken", new System.Text.UTF8Encoding(false));
        var corrupt = new ProfileStore(corruptPath);
        Require(corrupt.Warning.Length > 0, "Corrupt profile falls back with warning");
        corrupt.Save();
        Require(System.IO.File.ReadAllText(corruptPath, System.Text.Encoding.UTF8) == "{broken", "Corrupt original remains intact");
        GD.Print("PROFILE_PASS: round trip, volume preferences, legacy compatibility, range clamps, victory count, UTF-8, malformed-file preservation");
    }

    private void PressButton(string text)
    {
        var button = Descendants(screen!).OfType<Button>().FirstOrDefault(candidate => candidate.Text == GameText.Get(text));
        Require(button != null, $"Button exists: {text}");
        button!.EmitSignal(BaseButton.SignalName.Pressed);
    }

    private void AssertUiBounds()
    {
        var viewport = new Rect2(-1, -1, 1282, 722);
        foreach (var control in Descendants(screen!).OfType<Control>())
        {
            if (control is not (Button or Label or HSlider or RichTextLabel or TextureRect)) continue;
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
