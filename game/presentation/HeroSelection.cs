using Godot;
using Rebirth.Core;

namespace Rebirth.Presentation;

public partial class GameRoot
{
    private void ShowHeroes()
    {
        var panel = Modal("heroes", GameText.Get("CHOOSE YOUR PATH  /  选择行者"), GameText.Get("今夜，由谁来平息异变？"), 1200, 652);
        var heroes = new[]
        {
            (Kind: HeroKind.Reimu, Name: GameText.Get("博丽灵梦"), Subtitle: GameText.Get("乐园的巫女"), Motif: GameText.Get("御札 · 阴阳玉 · 封魔"), Stats: GameText.Get("110 点生命\n初始：基础直射御札\n需解锁：灵符「梦想封印」"), Summary: GameText.Get("修习解锁阵与玉，符可追踪、爆炸。\n从容穿行弹隙，守住进退之路。"), Accent: Palette.Red),
            (Kind: HeroKind.Marisa, Name: GameText.Get("雾雨魔理沙"), Subtitle: GameText.Get("普通的魔法使"), Motif: GameText.Get("星屑 · 光热 · 魔炮"), Stats: GameText.Get("85 点生命，伤害 +16%\n初始：星光射击 + Master Spark\n满蓄势：强化魔炮"), Summary: GameText.Get("星弹散射，慢移时收束。\n魔炮蓄势锁向，走位可平移火线。"), Accent: Palette.Violet)
        };
        Button? first = null;
        for (var index = 0; index < heroes.Length; index++)
        {
            var hero = heroes[index];
            var card = ui.Panel(panel, new(36 + index * 576, 136, 552, 432), new Color("14272d"));
            card.Name = $"hero_card_{hero.Kind}";
            var artwork = ui.Panel(card, new(12, 12, 204, 322));
            artwork.Name = $"portrait_frame_{hero.Kind}";
            artwork.ClipContents = true;
            artwork.MouseFilter = Control.MouseFilterEnum.Ignore;
            artwork.AddThemeStyleboxOverride("panel", PixelSkin.Frame("dark"));
            var portrait = new TextureRect
            {
                Name = $"selection_portrait_{hero.Kind}",
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
                MouseFilter = Control.MouseFilterEnum.Ignore,
                Texture = GD.Load<Texture2D>(HeroPortraitPath(hero.Kind)),
                Position = new(4, 4),
                Size = new(196, 314)
            };
            artwork.AddChild(portrait);
            ui.Label(card, hero.Subtitle, new(236, 22, 296, 24), 14, hero.Accent);
            ui.Label(card, hero.Name, new(232, 57, 300, 70), 32, Palette.Paper, true);
            ui.Label(card, hero.Motif, new(236, 139, 296, 36), 14, Palette.Gold);
            ui.Label(card, hero.Stats, new(236, 185, 296, 93), 18, Palette.Paper);
            if (!TouchLayout) ui.Label(card, hero.Summary, new(236, 289, 296, 60), 15, Palette.Muted);
            var button = ui.Button(card, GameText.Format($"执此道 · {hero.Name}"), new(24, TouchLayout ? 344 : 362, 504, TouchLayout ? 76 : 48), () => StartRun(hero.Kind), true);
            button.Name = $"choose_{hero.Kind}";
            first ??= button;
        }
        ui.Button(panel, GameText.Get("返回"), new(36, 584, 128, 52), ShowTitle);
        ui.Label(panel, GameText.Get("没有局外数值加成。每一次异闻，都从一次新的出发开始。"), new(192, 588, 972, 44), 15, Palette.Muted);
        first?.GrabFocus();
    }

    private static string HeroPortraitPath(HeroKind hero) => hero switch
    {
        HeroKind.Reimu => "res://assets/ui/portraits/ai-preview/reimu.png",
        HeroKind.Marisa => "res://assets/ui/portraits/ai-preview/marisa.png",
        _ => throw new ArgumentOutOfRangeException(nameof(hero), hero, "No selection portrait for this hero")
    };
}
