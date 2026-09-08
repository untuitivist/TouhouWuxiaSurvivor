using Godot;
using Rebirth.Core;

namespace Rebirth.Presentation;

public partial class GameRoot
{
    private bool TouchLayout => GamePlatform.HasTouch || profile?.Data.TouchMode == 1;
    private bool portraitPaused;

    private void UpdatePlatformLayout()
    {
        var portrait = GamePlatform.IsPortrait();
        if (portrait && !portraitPaused)
        {
            touchHud.ResetPointers();
            dashRequested = false;
            if (run?.Phase == RunPhase.Playing) { run.TogglePause(); RefreshRunScreen(); }
        }
        portraitPaused = portrait;
    }

    private void BuildTouchTitle()
    {
        var panel = ui.Panel(screen!, new(80, 56, 1120, 605));
        ui.Label(panel, "TOUHOU SURVIVOR  /  夜境手帖", new(48, 28, 1000, 34), 22, Palette.Gold);
        ui.Label(panel, "幻想乡 · 夜境异闻", new(44, 78, 1032, 82), 54, Palette.Paper, true);
        ui.Label(panel, "一段夜行，一场尚未平息的异变。", new(48, 155, 1000, 38), 24, Palette.Muted);
        ui.Button(panel, "踏入夜境     →", new(48, 220, 1024, 90), ShowHeroes, true).GrabFocus();
        ui.Button(panel, "行走须知", new(48, 328, 502, 86), ShowHelp);
        ui.Button(panel, "游戏设置", new(570, 328, 502, 86), ShowSettings);
        ui.Button(panel, "夜境图鉴", new(48, 432, 330, 86), OpenJournal);
        ui.Button(panel, "更新记录", new(395, 432, 330, 86), ShowChangelog);
        ui.Button(panel, "触控与诊断", new(742, 432, 330, 86), () => { settingsTab = 3; BuildSettings(); });
        ui.Label(panel, $"退治最佳 {profile.Data.BestKills}  ·  平息异变 {profile.Data.Victories} 次  ·  横屏触控 / 键盘均可", new(48, 546, 1024, 34), 21, Palette.Muted);
        ui.Label(screen!, profile.Notice.Length > 0 ? profile.Notice : "东方同人内部原型 · 素材未经公开发行授权", new(84, 676, 1110, 33), 17, Palette.Paper).AddThemeColorOverride("font_color", Palette.Paper);
    }
}
