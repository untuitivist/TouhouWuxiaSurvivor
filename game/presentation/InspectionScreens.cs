using Godot;
using Rebirth.Core;

namespace Rebirth.Presentation;

public partial class GameRoot
{
    private void ShowBuild()
    {
        if (run == null) return;
        var panel = Modal("build", "YOUR PATH  /  属性与构筑", "知己所长，进退有度。", 1120, 620);
        var hero = run.Hero == HeroKind.Reimu ? "博丽灵梦" : "雾雨魔理沙";
        ui.Label(panel, $"{hero}  ·  修习 {run.Level}", new(36, 132, 292, 32), 22, Palette.Gold);
        ui.Label(panel, $"生命  {run.Health:0.#} / {run.MaxHealth:0}\n术式威力  ×{run.Power:0.00}\n施放频率  ×{run.CastSpeed:0.00}\n移动速度  {run.MoveSpeed:0.#}\n拾取半径  {run.PickupRadius:0}\n闪身冷却  {run.DashInterval:0.00} 秒", new(36, 180, 280, 202), 18);
        ui.Label(panel, $"符卡：{ArtCatalog.SignatureName(run.Hero)}\n蓄势满后自动施放，无额外按键。\n数值含本局修习；频率不等于总伤害。", new(36, 397, 281, 79), 14, Palette.Muted);
        var abilities = ArtCatalog.Abilities(run.Hero).ToArray();
        for (var index = 0; index < abilities.Length; index++)
        {
            var art = abilities[index];
            var rank = run.Ranks[(int)art.Id];
            var color = rank > 0 ? new Color(art.Color) : Palette.Muted;
            var card = ui.Panel(panel, new(344, 132 + index * 115, 723, 110), new Color("172a31"));
            ui.Label(card, art.Name, new(16, 8, 570, 31), 23, color, true);
            ui.Label(card, rank == 0 ? "未习得" : $"{rank} / {art.MaxRank} 重", new(617, 14, 92, 25), 14, color);
            ui.Label(card, art.Source, new(17, 42, 689, 23), 12, Palette.Muted);
            ui.Label(card, rank >= art.MaxRank ? "圆满：" + art.Mastery : ArtCatalog.UpgradeText(art.Id, rank), new(17, 68, 689, 36), 15, Palette.Paper);
        }
        var training = ArtCatalog.Training.Select(art => $"{art.Name} {run.Ranks[(int)art.Id]}/{art.MaxRank}");
        ui.Label(panel, "通用修习    " + string.Join("    ·    ", training), new(36, 496, 1048, 34), 16, Palette.Jade);
        ui.Button(panel, $"返回 [{GameControls.Hint(GameControls.Inspect)} / Esc]", new(36, 554, 276, 42), CloseBuild, true).GrabFocus();
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
        versions.AddItem("未发布（工作区变更）");
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
            else if (index == headings.Count)
            {
                var start = text.IndexOf("## 未发布", StringComparison.Ordinal);
                section = start >= 0 ? text[start..(headings.Count > 0 ? headings[0].Index : text.Length)] : "暂无未发布记录。";
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
            if (string.IsNullOrWhiteSpace(section)) history.AddText("更新记录读取失败。请检查此版本的完整性。");
            history.ScrollToLine(0);
        }
        versions.ItemSelected += SelectVersion;
        SelectVersion(0);
        ui.Button(panel, "返回", new(36, 554, 260, 42), NavigateBack, true).GrabFocus();
        ui.Label(panel, "按版本浏览 · 可选择复制 · 历史内容随包保留", new(325, 563, 699, 28), 15, Palette.Muted);
    }
}
