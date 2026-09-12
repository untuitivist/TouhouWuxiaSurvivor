using Godot;
using Rebirth.Core;

namespace Rebirth.Presentation;

public partial class GameRoot
{
    private void ShowBuild()
    {
        if (run == null) return;
        var panel = Modal("build", GameText.Get("YOUR PATH  /  属性与构筑"), GameText.Get("知己所长，进退有度。"), 1120, 620);
        var hero = run.Hero == HeroKind.Reimu ? GameText.Get("博丽灵梦") : GameText.Get("雾雨魔理沙");
        ui.Label(panel, GameText.Format($"{hero}  ·  修习 {run.Level}"), new(36, 132, 292, 32), 22, Palette.Gold);
        ui.Label(panel, GameText.Format($"生命  {run.Health:0.#} / {run.MaxHealth:0}\n术式威力  ×{run.Power:0.00}\n施放频率  ×{run.CastSpeed:0.00}\n移动速度  {run.MoveSpeed:0.#}\n拾取半径  {run.PickupRadius:0}\n闪身冷却  {run.DashInterval:0.00} 秒"), new(36, 180, 280, 202), 18);
        ui.Label(panel, GameText.Format($"符卡：{ArtCatalog.SignatureName(run.Hero)}\n{(run.Build.SignatureUnlocked ? GameText.Get("已解锁，满蓄势自动施放。") : GameText.Get("尚未解锁，修习中领悟。"))}\n数值含本局修习；频率不等于总伤害。"), new(36, 397, 281, 79), 14, Palette.Muted);
        var abilities = ArtCatalog.Abilities(run.Hero).ToArray();
        for (var index = 0; index < abilities.Length; index++)
        {
            var art = abilities[index];
            var rank = run.Ranks[(int)art.Id];
            var color = rank > 0 ? new Color(art.Color) : Palette.Muted;
            var card = ui.Panel(panel, new(344, 132 + index * 115, 723, 110), new Color("172a31"));
            ui.Label(card, art.Name, new(16, 8, 570, 31), 23, color, true);
            ui.Label(card, rank == 0 ? GameText.Get("未习得") : GameText.Format($"{rank} / {art.MaxRank} 重"), new(617, 14, 92, 25), 14, color);
            var branches = UpgradeCatalog.All.Where(upgrade => upgrade.Ability == art.Id && (upgrade.Kind is UpgradeKind.Behavior or UpgradeKind.Training or UpgradeKind.Stance));
            var branchText = string.Join(" · ", branches.Select(upgrade => upgrade.Kind == UpgradeKind.Training
                ? UpgradeCatalog.LearnedName(upgrade, run.Build)
                : GameText.Format($"{(run.Build.Rank(upgrade) > 0 ? GameText.Get("已悟") : upgrade.Kind == UpgradeKind.Stance && run.Build.Stance != MainlineStance.None ? GameText.Get("本局放弃") : GameText.Get("待悟"))} {upgrade.Name}")));
            var branchDetails = new RichTextLabel { Position = new(17, 40), Size = new(689, 63), BbcodeEnabled = false, ScrollActive = true, Text = branchText };
            branchDetails.AddThemeFontSizeOverride("normal_font_size", 13);
            branchDetails.AddThemeColorOverride("default_color", PixelSkin.Ink);
            card.AddChild(branchDetails);
        }
        ui.Label(panel, run.Hero == HeroKind.Marisa ? GameText.Format($"在场星体 {run.PlayerStarCount}/{MarisaTuning.StarLimit} · 满载阻塞 {run.Marisa.BlockedEmissions} 次")
            : GameText.Format($"主线威力 ×{MainlineGrowth.Power(run.Build):0.00}"), new(344, 476, 723, 22), 13, Palette.Muted);
        var training = ArtCatalog.Training.Select(art => GameText.Format($"{art.Name} {run.Ranks[(int)art.Id]}/{art.MaxRank}"));
        ui.Label(panel, GameText.Get("通用修习    ") + string.Join("    ·    ", training), new(36, 508, 1048, 34), 16, Palette.Jade);
        ui.Button(panel, GameText.Format($"返回 [{GameControls.Hint(GameControls.Inspect)} / Esc]"), new(36, 554, 276, 42), CloseBuild, true).GrabFocus();
        var destination = run.Phase == RunPhase.Choosing ? GameText.Get("返回后继续三选一，不改变候选项。") : resumeAfterInspection ? GameText.Get("返回后继续战斗。") : GameText.Get("返回暂停菜单，战斗仍暂停。");
        ui.Label(panel, GameText.Get("时间已停。") + destination, new(343, 561, 741, 30), 16, Palette.Muted);
    }

    private void ShowChangelog()
    {
        var panel = Modal("changelog", GameText.Get("RELEASE NOTES  /  更新记录"), GameText.Get("每一次改变，都留有来处。"), 1060, 620);
        var text = Godot.FileAccess.GetFileAsString("res://CHANGELOG.md");
        var headings = System.Text.RegularExpressions.Regex.Matches(text, @"(?m)^## ((?:alpha|beta|rc|stable)-\d+\.\d+\.\d+)\r?$");
        var versions = new OptionButton { Name = "release_version", Position = new(36, 129), Size = new(382, 40) };
        foreach (System.Text.RegularExpressions.Match heading in headings) versions.AddItem(heading.Groups[1].Value);
        versions.AddItem(GameText.Get("未发布（工作区变更）"));
        versions.AddItem(GameText.Get("全部历史（含版本说明）"));
        panel.AddChild(versions);
        var history = new RichTextLabel
        {
            Position = new(36, 190), Size = new(988, 340),
            BbcodeEnabled = false, SelectionEnabled = true,
            ScrollActive = true, FocusMode = Control.FocusModeEnum.All
        };
        history.AddThemeFontSizeOverride("normal_font_size", 17);
        panel.AddChild(history);
        void SelectVersion(long selected)
        {
            var index = (int)selected;
            var section = text;
            if (index < headings.Count)
            {
                var end = index + 1 < headings.Count ? headings[index + 1].Index : text.Length;
                section = text[headings[index].Index..end];
            }
            else if (index == headings.Count)
            {
                var start = text.IndexOf("## 未发布", StringComparison.Ordinal);
                section = start >= 0 ? text[start..(headings.Count > 0 ? headings[0].Index : text.Length)] : GameText.Get("暂无未发布记录。");
            }
            history.Clear();
            foreach (var line in section.Split('\n'))
            {
                var content = line.TrimEnd('\r');
                var heading = content.StartsWith('#');
                history.PushFontSize(heading ? 20 : 17);
                history.PushColor(heading ? PixelSkin.Red : PixelSkin.Ink);
                history.AddText((heading ? content.TrimStart('#', ' ') : content).Replace("`", "") + "\n");
                history.Pop();
                history.Pop();
            }
            if (string.IsNullOrWhiteSpace(section)) history.AddText(GameText.Get("更新记录读取失败。请检查此版本的完整性。"));
            history.ScrollToLine(0);
        }
        versions.ItemSelected += SelectVersion;
        SelectVersion(0);
        ui.Button(panel, GameText.Get("返回"), new(36, 554, 260, 42), NavigateBack, true).GrabFocus();
        ui.Label(panel, GameText.Get("按版本浏览 · 可选择复制 · 历史内容随包保留"), new(325, 563, 699, 28), 15, Palette.Muted);
    }
}
