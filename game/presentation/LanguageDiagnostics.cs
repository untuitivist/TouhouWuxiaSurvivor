using Godot;
using Rebirth.Core;
using System.Text;

namespace Rebirth.Presentation;

public partial class GameRoot
{
    private async Task RunLanguageSmoke()
    {
        try
        {
            var directory = ProjectSettings.GlobalizePath($"user://rebirth/diagnostics/language/{Guid.NewGuid():N}");
            var path = Path.Combine(directory, "profile.json");
            profile = new ProfileStore(path);
            profile.Data.BestKills = 137;
            profile.Data.MasterVolume = 0.37f;
            ShowSettings();
            SelectSetting("game_language", 1);
            Require(GameText.IsEnglish && profile.Data.Language == "en", "Language selector applies English");
            var restored = new ProfileStore(path);
            Require(restored.Data.Language == "en" && restored.Data.BestKills == 137 && restored.Data.MasterVolume == 0.37f, "Language saves without losing records or volume");
            var legacyPath = Path.Combine(directory, "legacy.json");
            System.IO.File.WriteAllText(legacyPath, "{\"Version\":1,\"BestKills\":91}", new UTF8Encoding(false));
            var legacy = new ProfileStore(legacyPath);
            Require(legacy.Data.Language == "zh" && legacy.Data.BestKills == 91, "Legacy profile defaults to Chinese");
            System.IO.File.WriteAllText(legacyPath, "{\"Version\":1,\"Language\":null,\"BestKills\":91}", new UTF8Encoding(false));
            Require(new ProfileStore(legacyPath).Data.Language == "zh", "Null locale falls back safely");
            foreach (var touch in new[] { 0, 1 })
            {
                profile.Data.TouchMode = touch;
                foreach (var show in new Action[] { ShowTitle, ShowHeroes, ShowHelp, ShowSettings, ShowJournal })
                {
                    show();
                    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                    AssertEnglishScreen();
                    if (currentScreen == "heroes") AssertHeroSelectionPortraits();
                }
                for (var tab = 0; tab < 4; tab++)
                {
                    settingsTab = tab;
                    BuildSettings();
                    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                    AssertEnglishScreen();
                }
                foreach (var entry in JournalCatalog.All)
                {
                    ShowJournalDetail(entry);
                    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                    AssertEnglishScreen();
                }
                StartRun(HeroKind.Reimu, 42);
                run!.TogglePause();
                ShowPause();
                AssertEnglishScreen();
                ShowSettings();
                settingsTab = 1;
                BuildSettings();
                videoDraft.MaxFps = 144;
                var preserved = run;
                var ticks = run.Ticks;
                var ranks = run.Ranks.ToArray();
                SelectSetting("game_language", 0);
                Require(GameText.Language == "zh", "Chinese can be restored");
                Require(JournalCatalog.All.First().Name == "博丽灵梦", "Journal cache follows language");
                SelectSetting("game_language", 1);
                Require(ReferenceEquals(run, preserved) && run.Phase == RunPhase.Paused && run.Ticks == ticks && run.Ranks.SequenceEqual(ranks), "Switching language preserves the paused run");
                Require(GamePlatform.IsWeb || videoDraft.MaxFps == 144, "Switching language preserves unapplied video options");
                ShowBuild();
                AssertEnglishScreen();
                run.TogglePause();
                run.AddExperience(run.NextLevelExperience);
                run.Step(default);
                RefreshRunScreen();
                AssertEnglishScreen();
                ShowResult();
                AssertEnglishScreen();
                PrepareGrowthPreview(HeroKind.Marisa);
                AssertEnglishScreen();
                ShowBuild();
                AssertEnglishScreen();
                PrepareGrowthCombatPreview(HeroKind.Marisa);
                OpenBuild();
                AssertEnglishScreen();
            }
            GD.Print("LANGUAGE_SMOKE_PASS: bilingual settings, persistence, legacy saves, desktop/touch UI, journal, upgrades and paused-run safety");
            if (!GamePlatform.IsWeb) GetTree().Quit();
        }
        catch (Exception error)
        {
            GD.PushError(error.ToString());
            if (!GamePlatform.IsWeb) GetTree().Quit(1);
        }
    }

    private void AssertEnglishScreen()
    {
        AssertUiBounds();
        foreach (var node in Descendants(screen!))
        {
            if (node.Name == "game_language") continue;
            var text = node switch { Label label => label.Text, RichTextLabel rich => rich.Text, Button button => button.Text, _ => "" };
            Require(!text.Any(character => character >= 0x3400 && character <= 0x9fff), $"English text on {currentScreen}: {text}");
            if (node is OptionButton option)
                for (var index = 0; index < option.ItemCount; index++)
                    Require(!option.GetItemText(index).Any(character => character >= 0x3400 && character <= 0x9fff), "English option label");
        }
    }
}
