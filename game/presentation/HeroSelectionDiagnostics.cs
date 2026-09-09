using Godot;
using Rebirth.Core;

namespace Rebirth.Presentation;

public partial class GameRoot
{
    private void TestHeroSelectionPortraits()
    {
        var previousLanguage = GameText.Language;
        var previousTouch = profile.Data.TouchMode;
        try
        {
            foreach (var language in new[] { "zh", "en" })
            foreach (var touch in new[] { 0, 1 })
            {
                GameText.SetLanguage(language);
                profile.Data.TouchMode = touch;
                ShowHeroes();
                AssertHeroSelectionPortraits();
                AssertUiBounds();
                foreach (var hero in new[] { HeroKind.Reimu, HeroKind.Marisa })
                {
                    var choice = Descendants(screen!).OfType<Button>().Single(button => button.Name == $"choose_{hero}");
                    choice.EmitSignal(BaseButton.SignalName.Pressed);
                    Require(run?.Hero == hero && currentScreen == "playing", "Portrait card starts the matching hero");
                    Require(!Descendants(screen!).OfType<TextureRect>().Any(texture => texture.Name.ToString().StartsWith("selection_portrait_", StringComparison.Ordinal)), "Selection portraits do not enter gameplay UI");
                    ShowHeroes();
                }
                PressKey(Key.Escape);
                Require(currentScreen == "title", "Hero selection still supports Escape return");
            }
        }
        finally
        {
            GameText.SetLanguage(previousLanguage);
            profile.Data.TouchMode = previousTouch;
            ShowTitle();
        }
        GD.Print("HERO_PORTRAITS_PASS: both textures, bounded aspect-preserving layout, bilingual/touch cards, matching hero callbacks and menu-only scope");
    }

    private void AssertHeroSelectionPortraits()
    {
        var portraits = Descendants(screen!).OfType<TextureRect>().Where(texture => texture.Name.ToString().StartsWith("selection_portrait_", StringComparison.Ordinal)).ToArray();
        Require(portraits.Length == 2, "Hero selection contains exactly two portraits");
        foreach (var hero in new[] { HeroKind.Reimu, HeroKind.Marisa })
        {
            var portrait = portraits.Single(texture => texture.Name == $"selection_portrait_{hero}");
            var frame = (Control)portrait.GetParent();
            var card = (Control)frame.GetParent();
            var button = card.GetChildren().OfType<Button>().Single();
            Require(portrait.Texture != null && portrait.Texture.ResourcePath == HeroPortraitPath(hero), "Each hero loads their own portrait");
            Require(portrait.Texture!.GetSize() == new Vector2(520, 800), "Portrait keeps the authored aspect and size");
            Require(portrait.ExpandMode == TextureRect.ExpandModeEnum.IgnoreSize && portrait.StretchMode == TextureRect.StretchModeEnum.KeepAspectCentered, "Texture native size cannot expand the card or stretch the body");
            Require(frame.ClipContents && portrait.MouseFilter == Control.MouseFilterEnum.Ignore && frame.MouseFilter == Control.MouseFilterEnum.Ignore, "Artwork clips safely and cannot intercept card input");
            Require(frame.GetGlobalRect().Encloses(portrait.GetGlobalRect()), "Portrait fits its own frame");
            Require(!frame.GetGlobalRect().Intersects(button.GetGlobalRect()), "Portrait does not overlap its start button");
            Require(button.Size.Y >= (TouchLayout ? 76 : 48), "Start buttons retain usable desktop and touch sizes");
            foreach (var label in card.GetChildren().OfType<Label>())
                Require(!frame.GetGlobalRect().Intersects(label.GetGlobalRect()), "Portrait does not overlap hero text");
        }
    }
}
