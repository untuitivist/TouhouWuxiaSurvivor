using Godot;
using Rebirth.Core;

namespace Rebirth.Presentation;

public partial class GameRoot
{
    private void ShowChoices()
    {
        if (run == null) return;
        var panel = Modal("choices", GameText.Format($"ENLIGHTENMENT  /  修习 {run.Level}"), GameText.Get("此刻，悟得一式。"), 1100, 620);
        ui.Label(panel, GameText.Get("时间已停。选择角色能力或通用修习，决定下一步打法。"), new(37, 126, 1018, 25), 15, Palette.Muted);
        Button? first = null;
        for (var index = 0; index < run.Choices.Count; index++)
        {
            var selectedIndex = index;
            var upgrade = run.Choices[index];
            var art = upgrade.Art;
            var color = new Color(art.Color);
            var card = ui.Panel(panel, new(35 + index * 346, 162, 334, 350), new Color("172a31"));
            ui.Label(card, GameText.Format($"0{index + 1}    /    {upgrade.Category}"), new(19, 16, 294, 30), 14, color);
            ui.Label(card, upgrade.Name, new(17, 59, 300, 49), 29, Palette.Paper, true);
            ui.Label(card, UpgradeCatalog.Progress(upgrade, run.Build), new(20, 109, 294, 40), 13, color);
            ui.Label(card, UpgradeCatalog.Description(upgrade, run.Build), new(20, 153, 294, 105), 16, Palette.Paper);
            var footer = art.Source.Length > 0 ? art.Source : GameText.Get("通用修习 · 不改变角色的能力归属");
            if (!TouchLayout) ui.Label(card, footer, new(20, 267, 294, 36), 13, color);
            var action = new[] { GameControls.ChoiceOne, GameControls.ChoiceTwo, GameControls.ChoiceThree }[index];
            var label = pendingStance == upgrade.Id ? GameText.Get("再次确认定式")
                : index == 2 && run.Choices.Any(choice => choice.Kind == UpgradeKind.Stance) ? GameText.Get("暂不定式 · 获得此项")
                : TouchLayout ? GameText.Get("领悟此式") : GameText.Format($"[{GameControls.Hint(action)}]  领悟");
            var button = ui.Button(card, label, new(19, TouchLayout ? 266 : 310, 296, TouchLayout ? 78 : 30), () => SelectArt(selectedIndex), true);
            button.Name = $"growth_choice_{index + 1}";
            if (pendingStance == upgrade.Id) first = button;
            else first ??= button;
        }
        ui.Button(panel, GameText.Format($"查看构筑 [{GameControls.Hint(GameControls.Inspect)}]"), new(36, 560, 232, 36), OpenBuild);
        ui.Label(panel, GameText.Get("查看不会消耗选择，也不会刷新候选能力。"), new(296, 564, 766, 28), 14, Palette.Muted);
        first?.GrabFocus();
    }

    private void ShowPause()
    {
        if (run == null) return;
        var panel = Modal("pause", GameText.Get("A MOMENT OF STILLNESS  /  暂歇"), GameText.Get("风止，夜未尽。"), 920, 520);
        ui.Label(panel, GameText.Format($"行走 {GameCanvas.FormatTime(run.Time)}   ·   修习 {run.Level}   ·   退治 {run.Kills}"), new(36, 132, 840, 32), 19, Palette.Gold);
        var build = ArtCatalog.All.Where(art => art.Id != ArtKind.Recovery && run.Ranks[(int)art.Id] > 0).Select(art => GameText.Format($"{art.Name}  {run.Ranks[(int)art.Id]} 重"));
        ui.Label(panel, string.Join("     ", build) + "\n" + string.Join(" · ", UpgradeCatalog.All.Where(upgrade => (upgrade.Kind is UpgradeKind.Behavior or UpgradeKind.Training or UpgradeKind.Stance) && run.Build.Rank(upgrade) > 0).Select(upgrade => UpgradeCatalog.LearnedName(upgrade, run.Build))), new(36, 188, 844, 116), 18, Palette.Paper);
        ui.Button(panel, GameText.Format($"属性与构筑 [{GameControls.Hint(GameControls.Inspect)}]"), new(36, 311, 270, 66), OpenBuild);
        ui.Button(panel, GameText.Get("更新记录"), new(324, 311, 270, 66), ShowChangelog);
        ui.Button(panel, GameText.Get("夜境图鉴"), new(612, 311, 270, 66), OpenJournal);
        ui.Label(panel, GameText.Format($"{GameControls.Hint(GameControls.Pause, true)} 继续 · 子页面先返回此处，不直接恢复战斗"), new(36, 382, 844, 28), 14, Palette.Muted);
        ui.Button(panel, GameText.Get("继续行走"), new(36, 426, 270, TouchLayout ? 82 : 49), NavigateBack, true).GrabFocus();
        ui.Button(panel, GameText.Get("游戏设置"), new(324, 426, 270, TouchLayout ? 82 : 49), ShowSettings);
        ui.Button(panel, GameText.Get("结束本局"), new(612, 426, 270, TouchLayout ? 82 : 49), ShowAbandonConfirmation);
    }

    private void ShowAbandonConfirmation()
    {
        var panel = Modal("abandon", GameText.Get("RETURN  /  归途"), GameText.Get("结束这次行走？"), 680, 330);
        ui.Label(panel, GameText.Get(run?.HasStandardVictory == true ? "标准通关已保留；结束时记录本次续战，不保留进行中的构筑。" : "本局构筑不会保留，也不会记入通关记录。"), new(36, 137, 608, 45), 19, Palette.Muted);
        ui.Button(panel, GameText.Get("继续行走"), new(36, 237, 292, 49), ShowPause, true).GrabFocus();
        ui.Button(panel, GameText.Get("回到标题"), new(348, 237, 294, 49), () =>
        {
            if (run?.FinishChallenge() == true && !diagnosticMode) profile.RecordContinuation(run);
            ShowTitle();
        });
    }

    private void ShowResult()
    {
        if (run == null) return;
        if (!recorded) { if (!diagnosticMode) profile.Record(run); recorded = true; }
        else if (!diagnosticMode) profile.RecordContinuation(run);
        var won = run.HasStandardVictory || run.Phase == RunPhase.Won;
        var panel = Modal("result", run.IsEndless ? GameText.Get("EXTENDED CHALLENGE  /  续战修行") : won ? GameText.Get("INCIDENT RESOLVED  /  异变平息") : GameText.Get("THE STORY CONTINUES  /  此行未竟"),
            run.IsEndless ? GameText.Get(run.Phase == RunPhase.Won ? "本轮切磋完成。" : "续战止步，通关保留。") : won ? GameText.Get("妖雾散，夜色归。") : GameText.Get("整装，再来。"), 960, 558);
        ui.Label(panel, won ? GameText.Get("雾中的来客笑着收起了弹幕。幻想乡，又迎来一个平静的清晨。") : GameText.Get("并非每次出发都要抵达终点。记住这一局的弹隙，下次会更从容。"), new(36, 130, 888, 51), 18, Palette.Muted);
        var statistics = new[] { (GameText.Get("行走时间"), GameCanvas.FormatTime(run.Time)), (GameText.Get("退治妖怪"), run.Kills.ToString()), (GameText.Get("擦弹次数"), run.Grazes.ToString()), (GameText.Get("净化古印"), $"{run.PurifiedSeals} / 3") };
        for (var index = 0; index < statistics.Length; index++)
        {
            var card = ui.Panel(panel, new(36 + index * 225, 207, 212, 105), new Color("172c31"));
            ui.Label(card, statistics[index].Item1, new(17, 12, 178, 28), 14, Palette.Muted);
            ui.Label(card, statistics[index].Item2, new(17, 46, 178, 49), 31, Palette.Gold);
        }
        var build = string.Join("  ·  ", ArtCatalog.Abilities(run.Hero).Where(art => run.Ranks[(int)art.Id] > 0).Select(art => GameText.Format($"{art.Name} {run.Ranks[(int)art.Id]}重")));
        var behaviors = string.Join(" · ", UpgradeCatalog.All.Where(upgrade => (upgrade.Kind is UpgradeKind.Behavior or UpgradeKind.Training or UpgradeKind.Stance) && run.Build.Rank(upgrade) > 0).Select(upgrade => UpgradeCatalog.LearnedName(upgrade, run.Build)));
        ui.Label(panel, build + "\n" + behaviors, new(36, 335, 888, 52), 16, Palette.Paper);
        ui.Label(panel, run.IsEndless ? GameText.Format($"标准通关已保留 · 续战 {GameCanvas.FormatTime(run.EndlessTime)} · 完成 {run.EndlessRounds} 轮")
            : GameText.Format($"修习 {run.Level}  ·  符卡施放 {run.SpellsCast} 次  ·  本局种子 {run.Seed}"), new(36, 391, 888, 31), 15, Palette.Muted);
        if (profile.Warning.Length > 0) ui.Label(panel, profile.Warning, new(36, 424, 888, 25), 13, Palette.Red);
        ui.Button(panel, GameText.Get(run.CanContinue ? "继续挑战" : "再行一局"), new(36, 474, 278, 49), () =>
        {
            if (run.CanContinue) { if (run.ContinueChallenge()) RefreshRunScreen(); }
            else StartRun(lastHero);
        }, true).GrabFocus();
        ui.Button(panel, GameText.Get("换一位行者"), new(336, 474, 278, 49), () => { run = null; canvas.Run = null; ShowHeroes(); });
        ui.Button(panel, GameText.Get("回到标题"), new(636, 474, 288, 49), ShowTitle);
    }
}
