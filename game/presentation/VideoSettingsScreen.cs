using Godot;

namespace Rebirth.Presentation;

public partial class GameRoot
{
    private VideoPreferences videoDraft = new();
    private VideoPreferences? videoPreview;
    private bool? previewReducedMotion;
    private double videoSeconds;
    private Label? videoCountdown;
    private bool videoFromSettings;
    private string videoReturnScreen = "title";

    private void BuildVideoSettings(Control panel)
    {
        if (GamePlatform.IsWeb) { BuildWebVideoSettings(panel); return; }
        videoDraft = profile.Data.Video.Copy();
        OptionButton? resolution = null;
        AddVideoOption(panel, "窗口模式", "window_mode", 199, ["窗口化", "无边框窗口", "全屏"], videoDraft.WindowMode,
            selected => { videoDraft.WindowMode = selected; resolution!.Disabled = selected != 0; });
        resolution = AddVideoOption(panel, "窗口分辨率", "resolution", 259,
            VideoPreferences.Resolutions.Select(size => $"{size.X} × {size.Y}").ToArray(),
            Array.IndexOf(VideoPreferences.Resolutions, new(videoDraft.Width, videoDraft.Height)),
            selected => { var size = VideoPreferences.Resolutions[selected]; videoDraft.Width = size.X; videoDraft.Height = size.Y; });
        resolution.Disabled = videoDraft.WindowMode != 0;
        AddVideoOption(panel, "垂直同步", "vsync", 319, ["关闭", "开启"], videoDraft.Vsync ? 1 : 0, selected => videoDraft.Vsync = selected == 1);
        AddVideoOption(panel, "帧率上限", "fps_limit", 379,
            VideoPreferences.FrameLimits.Select(value => value == 0 ? "不限制" : $"{value} FPS").ToArray(),
            Array.IndexOf(VideoPreferences.FrameLimits, videoDraft.MaxFps), selected => videoDraft.MaxFps = VideoPreferences.FrameLimits[selected]);
        ui.Button(panel, $"减少震屏：{(profile.Data.ReducedMotion ? "开" : "关")}", new(704, 199, 320, 42), () =>
        {
            profile.Data.ReducedMotion = !profile.Data.ReducedMotion;
            SaveSettings(false);
            var draft = videoDraft.Copy();
            BuildSettings();
            videoDraft = draft;
            RefreshVideoOptions();
        });
        ui.Label(panel, "减少震屏即时保存。\n其他画面修改需应用确认。\n\n窗口过大时按可用区域收缩。\n无边框保留任务栏；全屏铺满。\n均使用当前显示器。", new(704, 266, 320, 173), 16, Palette.Muted);
        ui.Button(panel, "应用画面设置", new(36, 451, 632, 43), () => BeginVideoPreview(videoDraft.Copy(), true), true);
        ui.Label(panel, "应用后 15 秒内确认；超时、Esc 或点击撤销会回退。离开本页丢弃未应用的画面选项。", new(36, 506, 988, 31), 15, Palette.Muted);
    }

    private void RefreshVideoOptions()
    {
        var controls = Descendants(screen!).OfType<OptionButton>().ToDictionary(option => option.Name.ToString());
        controls["window_mode"].Select(videoDraft.WindowMode);
        controls["resolution"].Select(Array.IndexOf(VideoPreferences.Resolutions, new(videoDraft.Width, videoDraft.Height)));
        controls["resolution"].Disabled = videoDraft.WindowMode != 0;
        controls["vsync"].Select(videoDraft.Vsync ? 1 : 0);
        controls["fps_limit"].Select(Array.IndexOf(VideoPreferences.FrameLimits, videoDraft.MaxFps));
    }

    private OptionButton AddVideoOption(Control panel, string label, string name, int vertical, string[] choices, int selected, Action<int> update)
    {
        ui.Label(panel, label, new(36, vertical, 210, 38), 18);
        var option = new OptionButton { Name = name, Position = new(254, vertical), Size = new(414, 42) };
        foreach (var choice in choices) option.AddItem(choice);
        option.Select(selected);
        option.ItemSelected += index => update((int)index);
        panel.AddChild(option);
        return option;
    }

    private void BeginVideoPreview(VideoPreferences candidate, bool fromSettings, bool? reducedMotion = null)
    {
        if (videoPreview != null) return;
        videoReturnScreen = currentScreen;
        videoFromSettings = fromSettings;
        if (currentScreen == "playing") { run!.TogglePause(); displayedPhase = run.Phase; }
        videoPreview = candidate;
        previewReducedMotion = reducedMotion;
        videoSeconds = 15;
        candidate.Apply();
        var panel = Modal("video_confirm", "DISPLAY PREVIEW  /  画面预览", "当前画面是否正常？", 880, 365);
        videoCountdown = ui.Label(panel, "15 秒后自动恢复原设置。", new(36, 145, 808, 54), 22, Palette.Gold);
        ui.Label(panel, "未确认的显示设置不会写入存档。Esc 撤销，确认后才保存。", new(36, 215, 808, 30), 17, Palette.Muted);
        ui.Button(panel, "撤销", new(36, 274, 385, 44), () => FinishVideoPreview(false), true).GrabFocus();
        ui.Button(panel, "保留画面设置", new(449, 274, 395, 44), () => FinishVideoPreview(true));
    }

    private void TickVideoPreview(double delta)
    {
        if (videoPreview == null) return;
        videoSeconds -= delta;
        if (videoSeconds <= 0) { FinishVideoPreview(false); return; }
        if (IsInstanceValid(videoCountdown)) videoCountdown!.Text = $"{Math.Ceiling(videoSeconds):0} 秒后自动恢复原设置。";
    }

    private void FinishVideoPreview(bool keep)
    {
        if (videoPreview == null) return;
        if (keep)
        {
            profile.Data.Video = videoPreview;
            if (previewReducedMotion.HasValue) profile.Data.ReducedMotion = previewReducedMotion.Value;
            SaveSettings(false);
        }
        else profile.Data.Video.Apply();
        videoPreview = null;
        previewReducedMotion = null;
        settingsMessage = keep ? "画面设置已确认。" : "已恢复原画面设置。";
        if (videoFromSettings) { settingsTab = 1; BuildSettings(); return; }
        switch (videoReturnScreen)
        {
            case "playing": run!.TogglePause(); RefreshRunScreen(); break;
            case "build": ShowBuild(); break;
            case "heroes": ShowHeroes(); break;
            case "help": ShowHelp(); break;
            case "changelog": ShowChangelog(); break;
            case "journal": ShowJournal(); break;
            case "journal_detail": ShowJournalDetail(JournalCatalog.All.Single(entry => entry.Id == journalSelected)); break;
            case "abandon": ShowAbandonConfirmation(); break;
            default: if (run != null) RefreshRunScreen(); else ShowTitle(); break;
        }
    }
}
