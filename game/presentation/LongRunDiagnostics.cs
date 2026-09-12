using Godot;
using Rebirth.Core;

namespace Rebirth.Presentation;

public partial class GameRoot
{
    private void PrepareStancePreview(HeroKind hero)
    {
        StartRun(hero, 42);
        var refine = hero == HeroKind.Reimu ? "reimu.ofuda.refine" : "marisa.stars.refine";
        for (var index = 0; index < 3; index++) run!.Build.TryApply(UpgradeCatalog.Get(refine), 100);
        run!.AddExperience(run.NextLevelExperience);
        run.Step(default);
        canvas.ResetView();
        RefreshRunScreen();
    }

    private bool PrepareLongRunFixture(string mode)
    {
        if (mode is "stance-reimu" or "stance-marisa")
        {
            PrepareStancePreview(mode == "stance-reimu" ? HeroKind.Reimu : HeroKind.Marisa);
            return true;
        }
        if (mode is not ("boss" or "boss-reimu" or "boss-marisa" or "challenge-result")) return false;
        StartRun(mode == "boss-reimu" ? HeroKind.Marisa : HeroKind.Reimu, 42);
        var boss = run!.SpawnEnemy(EnemyKind.Boss, new(320, -30));
        boss.Health = boss.MaxHealth * 0.3f;
        boss.Abilities!.FieldCooldown = 0;
        boss.Abilities.SpecialCooldown = 0;
        boss.Abilities.RecoveryCooldown = 1000;
        for (var tick = 0; tick < 90; tick++) run.Step(new(new(0, 0.35f)));
        if (mode == "challenge-result")
        {
            run.DamageEnemy(boss, boss.Health, System.Numerics.Vector2.Zero);
            run.ContinueChallenge();
            var next = run.SpawnEnemy(EnemyKind.Boss, new(320, -30));
            run.DamageEnemy(next, next.Health, System.Numerics.Vector2.Zero);
        }
        canvas.Clock = run.Time;
        canvas.ResetView();
        canvas.ReceiveEvents();
        RefreshRunScreen();
        return true;
    }

    private void TestLongRunInterface()
    {
        var touch = profile.Data.TouchMode;
        foreach (var hero in Enum.GetValues<HeroKind>())
        foreach (var mode in new[] { 0, 1 })
        {
            profile.Data.TouchMode = mode;
            PrepareStancePreview(hero);
            var pending = run!.PendingChoices;
            var original = run.Choices.ToArray();
            PressKey(Key.Key1);
            Require(run.Build.Stance == MainlineStance.None && run.PendingChoices == pending && pendingStance == original[0].Id, "First input previews a stance without consuming growth");
            AssertUiBounds();
            if (GameText.IsEnglish) AssertEnglishScreen();
            OpenBuild();
            Require(Descendants(screen!).OfType<RichTextLabel>().All(label => label.GetThemeColor("default_color") == PixelSkin.Ink && label.Text.Length > 0), "Build details use readable dark ink on the light cards");
            CloseBuild();
            Require(run.Choices.SequenceEqual(original) && pendingStance == original[0].Id, "Inspection preserves the pending stance and original cards");
            PressKey(Key.Key1);
            Require(run.Build.Stance == original[0].Stance && run.PendingChoices == pending - 1, "Second input confirms exactly once");
            PrepareStancePreview(hero);
            PressKey(Key.Key3);
            Require(run.Build.Stance == MainlineStance.None && run.Build.AllocatedPoints == 4, "The third card grants ordinary growth while deferring the stance");
        }
        profile.Data.TouchMode = touch;
        PrepareLongRunFixture("challenge-result");
        Require(run!.HasStandardVictory && run.EndlessRounds == 1 && currentScreen == "result", "Extended result retains standard victory");
        AssertUiBounds();
        if (GameText.IsEnglish) AssertEnglishScreen();
        TestContinuationRecords();
        ShowTitle();
        GD.Print("LONG_RUN_UI_PASS: stance confirmation, deferral, inspection, touch, continuation records and legacy profiles");
    }

    private void TestContinuationRecords()
    {
        var directory = ProjectSettings.GlobalizePath($"user://rebirth/diagnostics/continuation/{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "profile.json");
        System.IO.File.WriteAllText(path, "{\"Version\":1,\"Victories\":3,\"CompletedRuns\":7,\"FastestVictory\":250,\"MasterVolume\":0.37}", new System.Text.UTF8Encoding(false));
        var store = new ProfileStore(path);
        Require(store.Data.LongRunVictories == 0 && store.Data.BestEndlessRounds == 0 && store.Data.Victories == 3, "Old victories migrate without being reclassified as long runs");
        store.Record(run!);
        store.RecordContinuation(run!);
        store.RecordContinuation(run!);
        var restored = new ProfileStore(path);
        Require(restored.Data.Victories == 4 && restored.Data.CompletedRuns == 8 && restored.Data.LongRunVictories == 1 && restored.Data.BestEndlessRounds == 1 && restored.Data.MasterVolume == 0.37f, "Repeated continuation records do not duplicate standard completion or discard settings");
    }
}
