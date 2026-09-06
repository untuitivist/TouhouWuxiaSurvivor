using Godot;
using Rebirth.Core;

namespace Rebirth.Presentation;

public partial class GameRoot
{
    private Label? settingsWarning;
    private void ShowTitle()
    {
        run = null;
        canvas.Run = null;
        canvas.ResetView();
        ClearScreen("title");
        ui.Label(screen!, "TOUHOU  /  WUXIA  /  SURVIVOR", new(83, 68, 500, 30), 13, Palette.Gold);
        ui.Label(screen!, "幻想乡", new(77, 118, 490, 105), 82, Palette.Paper, true);
        ui.Label(screen!, "夜境异闻", new(81, 220, 510, 75), 53, Palette.Gold, true);
        ui.Label(screen!, "一段夜行，一场尚未平息的异变。", new(85, 312, 495, 38), 20, Palette.Muted, true);
        var first = ui.Button(screen!, "踏入夜境     →", new(86, 380, 362, 58), ShowHeroes, true);
        ui.Button(screen!, "行走须知", new(86, 450, 173, 45), ShowHelp);
        ui.Button(screen!, "音画设置", new(275, 450, 173, 45), ShowSettings);
        ui.Button(screen!, "更新记录", new(86, 507, 173, 43), ShowChangelog);
        ui.Button(screen!, "暂别夜境", new(275, 507, 173, 43), () => GetTree().Quit());
        ui.Label(screen!, $"异闻录   /   退治最佳 {profile.Data.BestKills}   ·   平息异变 {profile.Data.Victories} 次", new(86, 582, 500, 30), 14, Palette.Muted);
        ui.Label(screen!, "博丽夜境  ·  约五分钟一局  ·  自动战斗", new(816, 617, 403, 30), 15, Palette.Gold);
        var version = ProjectSettings.GetSetting("application/config/version").AsString();
        ui.Label(screen!, $"{version}  ·  从零重写试玩版", new(49, 681, 400, 26), 12, Palette.Muted);
        ui.Label(screen!, "东方同人内部原型 · 素材未经公开发行授权", new(841, 681, 395, 26), 12, Palette.Muted);
        if (profile.Warning.Length > 0) ui.Label(screen!, profile.Warning, new(86, 621, 510, 36), 13, Palette.Red);
        first.GrabFocus();
    }

    private void ShowHeroes()
    {
        var panel = Modal("heroes", "CHOOSE YOUR PATH  /  选择行者", "今夜，由谁来平息异变？", 1080, 570);
        var heroes = new[]
        {
            (HeroKind.Reimu, "博丽灵梦", "乐园的巫女", "御札 · 阴阳玉 · 封魔", "110 点生命\n初始：追踪御札 + 阴阳玉\n满蓄势：灵符「梦想封印」", "御札追踪，阴阳玉护身，留阵迎敌。\n从容穿行弹隙，守住进退之路。", Palette.Red, "灵"),
            (HeroKind.Marisa, "雾雨魔理沙", "普通的魔法使", "星屑 · 光热 · 魔炮", "85 点生命，伤害 +16%\n初始：星光射击 + Master Spark\n满蓄势：强化魔炮", "星弹散射，Shift 慢移时收束。\n魔炮蓄势锁向，走位可平移火线。", Palette.Violet, "魔")
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
            ui.Label(card, hero.Item6, new(24, 244, 445, 56), 15, Palette.Muted);
            var button = ui.Button(card, $"执此道 · {hero.Item2}", new(23, 302, 449, 38), () => StartRun(hero.Item1), true);
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
        ui.Label(panel, "WASD / 方向键  移动\nShift  慢移，显示判定点\nSpace  闪身，短暂无敌\nEsc / P  暂停    E  构筑\nF11  全屏", new(36, 188, 290, 172), 18);
        ui.Label(panel, "02   悟", new(355, 135, 270, 40), 26, Palette.Jade, true);
        ui.Label(panel, "攻击自动寻找目标。\n各自三条能力 + 通用修习。\n升级按 1 / 2 / 3 或点击。\nE 查看效果与原作出处。", new(355, 188, 293, 148), 18);
        ui.Label(panel, "03   破", new(676, 135, 280, 40), 26, Palette.Red, true);
        ui.Label(panel, "擦弹与退治积累符卡蓄势。\n灵梦：梦想封印追踪灵光。\n魔理沙：锁向持续魔炮。\n发动时清弹、吸取经验。", new(676, 188, 290, 148), 18);
        ui.Label(panel, "路上有三个古印。净化进度会保留，遇险可以先退；不净化也能迎战终局。", new(36, 370, 925, 60), 20, Palette.Gold, true);
        ui.Label(panel, "符卡取材于原作，本作改为自动施放并调整数值；不是逐帧复刻。四分钟后击破结界残影获胜。", new(36, 444, 925, 45), 14, Palette.Muted);
        ui.Button(panel, "明白了，回到夜境", new(36, 504, 924, 39), ShowTitle, true).GrabFocus();
    }

    private void ShowSettings()
    {
        var panel = Modal("settings", "SETTINGS  /  音画设置", "按自己的节奏。", 820, 600);
        AddVolumeSlider(panel, "总音量", "master_volume", 144, profile.Data.MasterVolume, value => profile.Data.MasterVolume = value);
        AddVolumeSlider(panel, "音乐音量", "music_volume", 208, profile.Data.MusicVolume, value => profile.Data.MusicVolume = value);
        AddVolumeSlider(panel, "音效音量", "sound_volume", 272, profile.Data.SoundVolume, value => profile.Data.SoundVolume = value);
        ui.Button(panel, $"音乐：{(profile.Data.MusicEnabled ? "开启" : "静音")}", new(36, 347, 230, 45), () => { profile.Data.MusicEnabled = !profile.Data.MusicEnabled; SaveSettings(); });
        ui.Button(panel, $"音效：{(profile.Data.SoundEnabled ? "开启" : "静音")}", new(294, 347, 230, 45), () => { profile.Data.SoundEnabled = !profile.Data.SoundEnabled; SaveSettings(); });
        ui.Button(panel, $"减少震屏：{(profile.Data.ReducedMotion ? "开" : "关")}", new(552, 347, 232, 45), () => { profile.Data.ReducedMotion = !profile.Data.ReducedMotion; SaveSettings(); });
        ui.Label(panel, "音量即时生效并自动保存；静音开关保留各自音量。\nTab 选择控件，左右键微调音量 · F11 切换全屏", new(36, 415, 748, 59), 16, Palette.Muted);
        settingsWarning = ui.Label(panel, profile.Warning, new(36, 480, 748, 30), 13, Palette.Red);
        ui.Button(panel, "返回", new(36, 531, 748, 43), NavigateBack, true).GrabFocus();
    }

    private void AddVolumeSlider(Control panel, string title, string name, int vertical, float value, Action<float> update)
    {
        ui.Label(panel, title, new(36, vertical, 130, 35), 19, Palette.Paper);
        var percentage = ui.Label(panel, $"{value * 100:0}%", new(690, vertical, 94, 35), 18, Palette.Gold);
        var slider = new HSlider
        {
            Name = name, Position = new(190, vertical + 5), Size = new(469, 30),
            MinValue = 0, MaxValue = 100, Step = 1, Value = Math.Round(value * 100),
            FocusMode = Control.FocusModeEnum.All, TooltipText = title
        };
        slider.ValueChanged += volume =>
        {
            update((float)volume / 100);
            percentage.Text = $"{volume:0}%";
            SaveSettings(false);
        };
        panel.AddChild(slider);
    }

    private void SaveSettings(bool refresh = true)
    {
        profile.Save();
        if (IsInstanceValid(settingsWarning)) settingsWarning!.Text = profile.Warning;
        audio.Apply(profile.Data);
        canvas.ReducedMotion = profile.Data.ReducedMotion;
        if (refresh) ShowSettings();
    }
}
