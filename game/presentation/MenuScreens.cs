using Godot;
using Rebirth.Core;

namespace Rebirth.Presentation;

public partial class GameRoot
{
    private void ShowTitle()
    {
        run = null;
        canvas.Run = null;
        canvas.ResetView();
        ClearScreen("title");
        if (TouchLayout) { BuildTouchTitle(); return; }
        ui.Panel(screen!, new(48, 52, 548, 601));
        ui.Label(screen!, "夜 境 手 帖    /    TOUHOU SURVIVOR", new(83, 76, 462, 30), 14, Palette.Gold);
        ui.Label(screen!, "幻想乡", new(77, 121, 490, 105), 74, Palette.Paper, true);
        ui.Label(screen!, "夜境异闻", new(81, 220, 510, 75), 53, Palette.Gold, true);
        ui.Label(screen!, "一段夜行，一场尚未平息的异变。", new(85, 312, 495, 38), 20, Palette.Muted, true);
        var first = ui.Button(screen!, "踏入夜境     →", new(86, 380, 362, 58), ShowHeroes, true);
        ui.Button(screen!, "行走须知", new(86, 450, 173, 45), ShowHelp);
        ui.Button(screen!, "游戏设置", new(275, 450, 173, 45), ShowSettings);
        ui.Button(screen!, "更新记录", new(86, 507, 173, 43), ShowChangelog);
        ui.Button(screen!, GamePlatform.IsWeb ? "切换全屏" : "暂别夜境", new(275, 507, 173, 43), () => { if (GamePlatform.IsWeb) ToggleWebFullscreen(); else GetTree().Quit(); });
        ui.Button(screen!, "夜境图鉴", new(86, 561, 362, 45), OpenJournal);
        ui.Label(screen!, $"退治最佳 {profile.Data.BestKills}   ·   平息异变 {profile.Data.Victories} 次", new(86, 610, 500, 26), 14, Palette.Muted);
        ui.Label(screen!, "博丽夜境  ·  约五分钟一局  ·  自动战斗", new(790, 617, 445, 30), 15, Palette.Gold).AddThemeColorOverride("font_color", Palette.Paper);
        var version = ProjectSettings.GetSetting("application/config/version").AsString();
        ui.Label(screen!, $"{version}  ·  从零重写试玩版", new(49, 681, 400, 26), 12, Palette.Muted).AddThemeColorOverride("font_color", Palette.Paper);
        ui.Label(screen!, "东方同人内部原型 · 素材未经公开发行授权", new(841, 681, 395, 26), 12, Palette.Muted).AddThemeColorOverride("font_color", Palette.Paper);
        if (profile.Notice.Length > 0) ui.Label(screen!, profile.Notice, new(86, 642, 510, 30), 13, Palette.Red);
        first.GrabFocus();
    }

    private void ShowHeroes()
    {
        var panel = Modal("heroes", "CHOOSE YOUR PATH  /  选择行者", "今夜，由谁来平息异变？", 1080, 570);
        var heroes = new[]
        {
            (HeroKind.Reimu, "博丽灵梦", "乐园的巫女", "御札 · 阴阳玉 · 封魔", "110 点生命\n初始：追踪御札 + 阴阳玉\n满蓄势：灵符「梦想封印」", "御札追踪，阴阳玉护身，留阵迎敌。\n从容穿行弹隙，守住进退之路。", Palette.Red, "灵"),
            (HeroKind.Marisa, "雾雨魔理沙", "普通的魔法使", "星屑 · 光热 · 魔炮", "85 点生命，伤害 +16%\n初始：星光射击 + Master Spark\n满蓄势：强化魔炮", "星弹散射，慢移时收束。\n魔炮蓄势锁向，走位可平移火线。", Palette.Violet, "魔")
        };
        Button? first = null;
        for (var index = 0; index < heroes.Length; index++)
        {
            var hero = heroes[index];
            var card = ui.Panel(panel, new(35 + index * 515, 136, 495, 351), new Color("14272d"));
            ui.Label(card, hero.Item8, new(361, 10, 114, 103), 78, Palette.Alpha(hero.Item7, 0.28f), true);
            ui.Label(card, hero.Item3, new(23, 22, 350, 24), 14, hero.Item7);
            ui.Label(card, hero.Item2, new(20, 53, 345, 50), 35, Palette.Paper, true);
            ui.Label(card, hero.Item4, new(24, 111, 440, 25), 14, Palette.Gold);
            ui.Label(card, hero.Item5, new(24, 152, 445, 88), 18, Palette.Paper);
            if (!TouchLayout) ui.Label(card, hero.Item6, new(24, 244, 445, 56), 15, Palette.Muted);
            var button = ui.Button(card, $"执此道 · {hero.Item2}", new(23, TouchLayout ? 269 : 302, 449, TouchLayout ? 76 : 38), () => StartRun(hero.Item1), true);
            first ??= button;
        }
        ui.Button(panel, "返回", new(35, 507, 115, 37), ShowTitle);
        ui.Label(panel, "没有局外数值加成。每一次异闻，都从一次新的出发开始。", new(210, 511, 820, 30), 15, Palette.Muted);
        first?.GrabFocus();
    }

    private void ShowHelp()
    {
        var panel = Modal("help", "FIELD NOTES  /  行走须知", "把注意力留给走位。", 1000, 566);
        ui.Label(panel, "01   行", new(36, 135, 260, 40), 26, Palette.Gold, true);
        ui.Label(panel, $"{GameControls.Hint(GameControls.Up)} / {GameControls.Hint(GameControls.Left)} / {GameControls.Hint(GameControls.Down)} / {GameControls.Hint(GameControls.Right)}  移动\n{GameControls.Hint(GameControls.Focus)}  慢移，显示判定点\n{GameControls.Hint(GameControls.Dash)}  闪身，短暂无敌\nEsc  安全暂停 / 返回\n{GameControls.Hint(GameControls.Fullscreen)}  全屏预览", new(36, 188, 290, 172), 17);
        ui.Label(panel, "02   悟", new(355, 135, 270, 40), 26, Palette.Jade, true);
        ui.Label(panel, $"攻击自动寻找目标。\n各自三条能力 + 通用修习。\n升级按卡片提示键或点击。\n{GameControls.Hint(GameControls.Inspect)} 查看效果与原作出处。", new(355, 188, 293, 148), 17);
        ui.Label(panel, "03   破", new(676, 135, 280, 40), 26, Palette.Red, true);
        ui.Label(panel, "擦弹与退治积累符卡蓄势。\n灵梦：梦想封印追踪灵光。\n魔理沙：锁向持续魔炮。\n发动时清弹、吸取经验。", new(676, 188, 290, 148), 18);
        ui.Label(panel, "路上有三个古印。净化进度会保留，遇险可以先退；不净化也能迎战终局。", new(36, 370, 925, 60), 20, Palette.Gold, true);
        ui.Label(panel, $"符卡为本作改编，四分钟后击破结界残影获胜。{GameControls.Hint(GameControls.Debug)} 显示只读诊断；可在操作设置中改键。", new(36, 444, 925, 45), 14, Palette.Muted);
        ui.Button(panel, "明白了，回到夜境", new(36, 504, 924, 39), ShowTitle, true).GrabFocus();
    }

}
