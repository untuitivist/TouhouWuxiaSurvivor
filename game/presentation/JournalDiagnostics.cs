using Godot;
using Rebirth.Core;

namespace Rebirth.Presentation;

public partial class GameRoot
{
    private void TestJournal()
    {
        Require(JournalCatalog.All.Select(entry => entry.Id).Distinct().Count() == JournalCatalog.All.Count, "Journal IDs are unique");
        foreach (var kind in Enum.GetValues<ArtKind>())
        {
            var entry = JournalCatalog.All.Single(item => item.Id == "art-" + kind);
            Require(entry.Name == ArtCatalog.Get(kind).Name && entry.Details.Contains(ArtCatalog.Get(kind).Description), "Journal reads current art catalog");
            if (ArtCatalog.Get(kind).Owner.HasValue)
                Require(entry.Details.Contains(ArtCatalog.UpgradeText(kind, 4)), "Journal mastery numbers match runtime tuning");
        }
        foreach (var category in Enum.GetValues<JournalCategory>())
            Require(JournalCatalog.All.Any(entry => entry.Category == category), "All active journal categories have entries");
        ShowTitle();
        PressButton("夜境图鉴");
        Require(currentScreen == "journal", "Title opens journal");
        AssertUiBounds();
        foreach (var page in Enumerable.Range(0, (JournalCatalog.All.Count + JournalPageSize - 1) / JournalPageSize))
        {
            journalPage = page;
            ShowJournal();
            var previews = Descendants(screen!).OfType<TextureRect>().Where(image => image.Name == "journal_picture").ToArray();
            Require(previews.Length == Math.Min(JournalPageSize, JournalCatalog.All.Count - page * JournalPageSize), "Every visible card keeps its preview");
            foreach (var image in previews)
                Require(image.Size == new Vector2(80, 80) && image.GetParent<Control>().ClipContents, "Card textures stay inside the clipped preview at native image sizes");
        }
        journalPage = 0;
        ShowJournal();
        PressButton("下一页");
        var card = Descendants(screen!).OfType<Button>().First(button => button.Name.ToString().StartsWith("journal_card_", StringComparison.Ordinal));
        var selectedName = card.Name;
        card.EmitSignal(Button.SignalName.Pressed);
        Require(currentScreen == "journal_detail", "Card opens independent detail");
        BeginVideoPreview(profile.Data.Video.Copy(), false);
        FinishVideoPreview(false);
        Require(currentScreen == "journal_detail" && "journal_card_" + journalSelected == selectedName, "Display preview restores the selected journal detail");
        AssertUiBounds();
        PressKey(Key.Escape);
        Require(currentScreen == "journal" && journalPage == 1 && GetViewport().GuiGetFocusOwner()?.Name == selectedName, "Detail back preserves page and focus");
        foreach (var entry in JournalCatalog.All)
        {
            ShowJournalDetail(entry);
            var preview = Descendants(screen!).OfType<TextureRect>().Single(image => image.Name == "journal_picture");
            Require(preview.Texture != null, "Every journal entry has an existing original texture");
            Require(preview.Size == new Vector2(240, 216), "Large original images fit detail preview");
            Require(Descendants(screen!).OfType<RichTextLabel>().Single().Text.Contains(entry.Source), "Details retain provenance and adaptation notes");
            AssertUiBounds();
        }
        ShowJournal();
        PressButton("术式");
        Require(journalCategory == JournalCategory.Ability && journalPage == 0, "Filter resets pagination");
        PressKey(Key.Escape);
        Require(currentScreen == "title", "Journal returns to title");
        StartRun(HeroKind.Marisa, 42);
        PressKey(Key.Escape);
        var originalRun = run!;
        var ticks = originalRun.Ticks;
        var ranks = originalRun.Ranks.ToArray();
        PressButton("夜境图鉴");
        ShowJournalDetail(JournalCatalog.All[0]);
        originalRun.Step(default);
        Require(originalRun.Ticks == ticks && originalRun.Ranks.SequenceEqual(ranks), "Browsing cannot advance or alter active run");
        PressKey(Key.Escape);
        Require(currentScreen == "journal" && journalCategory == JournalCategory.Ability, "Details preserve filter");
        PressKey(Key.Escape);
        Require(currentScreen == "pause" && ReferenceEquals(run, originalRun) && originalRun.Phase == RunPhase.Paused, "Pause journal never resumes battle on exit");
        journalCategory = null;
        journalPage = 0;
        journalSelected = "";
        ShowTitle();
        GD.Print("JOURNAL_PASS: current catalog, all assets, pages, filters, focus, details and pause safety");
    }
}
