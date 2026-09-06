using Godot;
using Rebirth.Core;

namespace Rebirth.Presentation;

public partial class GameRoot
{
    private void ShowBuild()
    {
        if (run == null) return;
        var panel = Modal("build", "YOUR PATH  /  属性与构筑", "看清这一剑，再决定下一式。", 1120, 620);
        var hero = run.Hero == HeroKind.Reimu ? "博丽灵梦" : "雾雨魔理沙";
        ui.Label(panel, $"{hero}  ·  第 {run.Level} 境", new(36, 132, 292, 32), 22, Palette.Gold);
        ui.Label(panel, $"生命  {run.Health:0.#} / {run.MaxHealth:0}\n武学威力  ×{run.Power:0.00}\n施放频率  ×{run.CastSpeed:0.00}\n移动速度  {run.MoveSpeed:0.#}\n拾取半径  {run.PickupRadius:0}\n闪身冷却  {run.DashInterval:0.00} 秒", new(36, 180, 280, 202), 18);
        ui.Label(panel, "数值包含角色与本局心法加成。\n移动速度为正常走位，不含慢移或闪身。\n施放频率不等于总伤害。", new(36, 397, 281, 79), 14, Palette.Muted);
        for (var index = 0; index < 4; index++)
        {
            var art = ArtCatalog.All[index];
            var rank = run.Ranks[index];
            var color = rank > 0 ? new Color(art.Color) : Palette.Muted;
            var card = ui.Panel(panel, new(344 + index % 2 * 369, 132 + index / 2 * 179, 354, 165), new Color("172a31"));
            ui.Label(card, art.Name, new(16, 12, 230, 31), 23, color, true);
            ui.Label(card, rank == 0 ? "未习得" : $"{rank} / {art.MaxRank} 重", new(263, 17, 79, 25), 14, color);
            ui.Label(card, art.School, new(17, 49, 318, 23), 14, Palette.Muted);
            ui.Label(card, (rank >= art.MaxRank ? "圆满：" + art.Mastery : "下一重：" + ArtCatalog.UpgradeText(art.Id, rank)), new(17, 84, 319, 66), 16, Palette.Paper);
        }
        var training = ArtCatalog.All.Skip(4).Take(4).Select(art => $"{art.Name} {run.Ranks[(int)art.Id]}/{art.MaxRank}");
        ui.Label(panel, "心法 / 身法    " + string.Join("    ·    ", training), new(36, 496, 1048, 34), 16, Palette.Jade);
        ui.Button(panel, "返回 [E / Esc]", new(36, 554, 276, 42), CloseBuild, true).GrabFocus();
        var destination = run.Phase == RunPhase.Choosing ? "返回后继续三选一，不改变候选项。" : resumeAfterInspection ? "返回后继续战斗。" : "返回暂停菜单，战斗仍暂停。";
        ui.Label(panel, "时间已停。" + destination, new(343, 561, 741, 30), 16, Palette.Muted);
    }

    private void ShowChangelog()
    {
        var panel = Modal("changelog", "RELEASE NOTES  /  更新记录", "每一次改变，都留有来处。", 1060, 620);
        var text = Godot.FileAccess.GetFileAsString("res://CHANGELOG.md");
        var headings = System.Text.RegularExpressions.Regex.Matches(text, @"(?m)^## ((?:alpha|beta|rc|stable)-\d+\.\d+\.\d+)\r?$");
        var versions = new OptionButton { Name = "release_version", Position = new(36, 129), Size = new(382, 40) };
        foreach (System.Text.RegularExpressions.Match heading in headings) versions.AddItem(heading.Groups[1].Value);
        versions.AddItem("全部历史（含版本说明）");
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
            history.Clear();
            foreach (var line in section.Split('\n'))
            {
                var content = line.TrimEnd('\r');
                var heading = content.StartsWith('#');
                history.PushFontSize(heading ? 20 : 17);
                history.PushColor(heading ? Palette.Gold : Palette.Paper);
                history.AddText((heading ? content.TrimStart('#', ' ') : content).Replace("`", "") + "\n");
                history.Pop();
                history.Pop();
            }
            if (string.IsNullOrWhiteSpace(section)) history.AddText("更新记录读取失败。请检查此版本的完整性。");
            history.ScrollToLine(0);
        }
        versions.ItemSelected += SelectVersion;
        SelectVersion(0);
        ui.Button(panel, "返回", new(36, 554, 260, 42), NavigateBack, true).GrabFocus();
        ui.Label(panel, "按版本浏览 · 可选择复制 · 历史内容随包保留", new(325, 563, 699, 28), 15, Palette.Muted);
    }
}
