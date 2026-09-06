using Godot;
using Rebirth.Core;

namespace Rebirth.Presentation;

public partial class GameRoot
{
    private void ShowChoices()
    {
        if (run == null) return;
        var panel = Modal("choices", $"ENLIGHTENMENT  /  修习 {run.Level}", "此刻，悟得一式。", 1100, 546);
        ui.Label(panel, "时间已停。选择角色能力或通用修习，决定下一步打法。", new(37, 111, 1018, 30), 16, Palette.Muted);
        Button? first = null;
        for (var index = 0; index < run.Choices.Count; index++)
        {
            var selectedIndex = index;
            var art = ArtCatalog.Get(run.Choices[index]);
            var rank = run.Ranks[(int)art.Id];
            var color = new Color(art.Color);
            var card = ui.Panel(panel, new(35 + index * 346, 162, 334, 303), new Color("172a31"));
            ui.Label(card, $"0{index + 1}    /    {art.School}", new(19, 16, 294, 30), 14, color);
            ui.Label(card, art.Name, new(17, 59, 300, 49), 29, Palette.Paper, true);
            ui.Label(card, ArtCatalog.RankText(art.Id, rank), new(20, 112, 294, 24), 14, color);
            ui.Label(card, ArtCatalog.UpgradeText(art.Id, rank), new(20, 147, 294, 70), 17, Palette.Paper);
            var footer = art.Source.Length > 0 ? art.Source : "通用修习 · 不改变角色的能力归属";
            ui.Label(card, footer, new(20, 219, 294, 40), 13, color);
            var button = ui.Button(card, $"[{index + 1}]  领悟", new(19, 263, 296, 30), () => SelectArt(selectedIndex), true);
            first ??= button;
        }
        ui.Button(panel, "查看构筑 [E]", new(36, 486, 232, 36), OpenBuild);
        ui.Label(panel, "查看不会消耗选择，也不会刷新候选能力。", new(296, 490, 766, 28), 14, Palette.Muted);
        first?.GrabFocus();
    }

    private void ShowPause()
    {
        if (run == null) return;
        var panel = Modal("pause", "A MOMENT OF STILLNESS  /  暂歇", "风止，夜未尽。", 920, 520);
        ui.Label(panel, $"行走 {GameCanvas.FormatTime(run.Time)}   ·   修习 {run.Level}   ·   退治 {run.Kills}", new(36, 132, 840, 32), 19, Palette.Gold);
        var build = ArtCatalog.All.Where(art => art.Id != ArtKind.Recovery && run.Ranks[(int)art.Id] > 0).Select(art => $"{art.Name}  {run.Ranks[(int)art.Id]} 重");
        ui.Label(panel, string.Join("     ", build), new(36, 188, 844, 116), 20, Palette.Paper);
        ui.Button(panel, "属性与构筑 [E]", new(36, 325, 410, 45), OpenBuild);
        ui.Button(panel, "更新记录", new(466, 325, 416, 45), ShowChangelog);
        ui.Label(panel, "Esc / P 继续 · 子页面先返回此处，不直接恢复战斗", new(36, 382, 844, 28), 14, Palette.Muted);
        ui.Button(panel, "继续行走", new(36, 426, 270, 49), NavigateBack, true).GrabFocus();
        ui.Button(panel, "音画设置", new(324, 426, 270, 49), ShowSettings);
        ui.Button(panel, "结束本局", new(612, 426, 270, 49), ShowAbandonConfirmation);
    }

    private void ShowAbandonConfirmation()
    {
        var panel = Modal("abandon", "RETURN  /  归途", "结束这次行走？", 680, 330);
        ui.Label(panel, "本局构筑不会保留，也不会记入通关记录。", new(36, 137, 608, 45), 19, Palette.Muted);
        ui.Button(panel, "继续行走", new(36, 237, 292, 49), ShowPause, true).GrabFocus();
        ui.Button(panel, "回到标题", new(348, 237, 294, 49), ShowTitle);
    }

    private void ShowResult()
    {
        if (run == null) return;
        if (!recorded) { if (!diagnosticMode) profile.Record(run); recorded = true; }
        var won = run.Phase == RunPhase.Won;
        var panel = Modal("result", won ? "INCIDENT RESOLVED  /  异变平息" : "THE STORY CONTINUES  /  此行未竟", won ? "妖雾散，夜色归。" : "整装，再来。", 960, 558);
        ui.Label(panel, won ? "雾中的来客笑着收起了弹幕。幻想乡，又迎来一个平静的清晨。" : "并非每次出发都要抵达终点。记住这一局的弹隙，下次会更从容。", new(36, 130, 888, 51), 18, Palette.Muted);
        var statistics = new[] { ("行走时间", GameCanvas.FormatTime(run.Time)), ("退治妖怪", run.Kills.ToString()), ("擦弹次数", run.Grazes.ToString()), ("净化古印", $"{run.PurifiedSeals} / 3") };
        for (var index = 0; index < statistics.Length; index++)
        {
            var card = ui.Panel(panel, new(36 + index * 225, 207, 212, 105), new Color("172c31"));
            ui.Label(card, statistics[index].Item1, new(17, 12, 178, 28), 14, Palette.Muted);
            ui.Label(card, statistics[index].Item2, new(17, 46, 178, 49), 31, Palette.Gold);
        }
        var build = string.Join("  ·  ", ArtCatalog.Abilities(run.Hero).Where(art => run.Ranks[(int)art.Id] > 0).Select(art => $"{art.Name} {run.Ranks[(int)art.Id]}重"));
        ui.Label(panel, build, new(36, 335, 888, 47), 19, Palette.Paper);
        ui.Label(panel, $"修习 {run.Level}  ·  符卡施放 {run.SpellsCast} 次  ·  本局种子 {run.Seed}", new(36, 391, 888, 31), 15, Palette.Muted);
        if (profile.Warning.Length > 0) ui.Label(panel, profile.Warning, new(36, 424, 888, 25), 13, Palette.Red);
        ui.Button(panel, "再行一局", new(36, 474, 278, 49), () => StartRun(lastHero), true).GrabFocus();
        ui.Button(panel, "换一位行者", new(336, 474, 278, 49), () => { run = null; canvas.Run = null; ShowHeroes(); });
        ui.Button(panel, "回到标题", new(636, 474, 288, 49), ShowTitle);
    }
}
