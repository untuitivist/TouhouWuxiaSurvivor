using Godot;
using Rebirth.Core;

namespace Rebirth.Presentation;

public partial class GameRoot
{
    private void PrepareFreeGrowthPreview(HeroKind hero)
    {
        StartRun(hero, 42);
        foreach (var art in ArtCatalog.Abilities(hero))
        {
            var upgrade = UpgradeCatalog.All.First(candidate => candidate.Ability == art.Id
                && candidate.Kind == (run!.Ranks[(int)art.Id] == 0 ? UpgradeKind.Unlock : UpgradeKind.Refine));
            Require(run!.Build.TryApply(upgrade, 100), "Prepare three independent investments");
        }
        run!.AddExperience(run.NextLevelExperience);
        run.Step(default);
        canvas.ResetView();
        RefreshRunScreen();
    }

    private bool PrepareLongRunFixture(string mode)
    {
        if (mode is "free-growth-reimu" or "free-growth-marisa")
        {
            PrepareFreeGrowthPreview(mode == "free-growth-reimu" ? HeroKind.Reimu : HeroKind.Marisa);
            return true;
        }
        if (mode is not ("boss" or "boss-reimu" or "boss-marisa" or "challenge-result")) return false;
        StartRun(mode == "boss-reimu" ? HeroKind.Marisa : HeroKind.Reimu, 42);
        var boss = run!.SpawnEnemy(EnemyKind.Boss, new(320, -30));
        boss.Health = boss.MaxHealth * 0.3f;
        boss.Abilities!.FieldCooldown = 0;
        boss.Abilities.ShotCooldown = 0;
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
            PrepareFreeGrowthPreview(hero);
            var pending = run!.PendingChoices;
            var original = run.Choices.ToArray();
            Require(original.Take(2).Select(upgrade => upgrade.Ability).Distinct().Count() == 2, "Choices expose independent abilities without forcing a primary route");
            AssertUiBounds();
            if (GameText.IsEnglish) AssertEnglishScreen();
            OpenBuild();
            Require(Descendants(screen!).OfType<RichTextLabel>().All(label => label.GetThemeColor("default_color") == PixelSkin.Ink && label.Text.Length > 0), "Build details use readable dark ink on the light cards");
            CloseBuild();
            Require(run.Choices.SequenceEqual(original) && run.PendingChoices == pending, "Inspection preserves the original choices without consuming growth");
            var investment = run.Build.Investment(original[0].Ability);
            PressKey(Key.Key1);
            Require(run.Build.AllocatedPoints == 4 && run.PendingChoices == pending - 1, "One input applies exactly one ordinary upgrade");
            Require(run.Build.Investment(original[0].Ability) == investment + 1, "The chosen ability records its own investment");
            foreach (var other in original.Where(upgrade => upgrade.Ability != original[0].Ability))
                Require(run.Build.CanChoose(other, run.Level), "Investing in one direction keeps other directions legal");
            PrepareFreeGrowthPreview(hero);
            PressKey(Key.Key3);
            Require(run.Build.AllocatedPoints == 4 && run.PendingChoices == 0, "The third card is a normal single-input choice, not a deferral");
        }
        profile.Data.TouchMode = touch;
        PrepareLongRunFixture("challenge-result");
        Require(run!.HasStandardVictory && run.EndlessRounds == 1 && currentScreen == "result", "Extended result retains standard victory");
        AssertUiBounds();
        if (GameText.IsEnglish) AssertEnglishScreen();
        TestContinuationRecords();
        ShowTitle();
        GD.Print("LONG_RUN_UI_PASS: free independent choices, one-input selection, inspection, touch, continuation records and legacy profiles");
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
