using Godot;

namespace Rebirth.Presentation;

public partial class GameRoot
{
    private int settingsTab;
    private Label? settingsWarning;
    private string settingsMessage = "";

    private void ShowSettings()
    {
        settingsTab = 0;
        settingsMessage = "";
        BuildSettings();
    }

    private void BuildSettings()
    {
        captureAction = null;
        var panel = Modal("settings", "SETTINGS  /  游戏设置", "按自己的节奏。", 1060, 660);
        var tabs = new[] { "声音", "画面", "操作" };
        for (var index = 0; index < tabs.Length; index++)
        {
            var selected = index;
            ui.Button(panel, tabs[index], new(36 + index * 335, 130, 318, 42), () => { settingsTab = selected; settingsMessage = ""; BuildSettings(); }, index == settingsTab);
        }
        switch (settingsTab)
        {
            case 0: BuildAudioSettings(panel); break;
            case 1: BuildVideoSettings(panel); break;
            case 2: BuildControlSettings(panel); break;
        }
        settingsWarning = ui.Label(panel, profile.Warning.Length > 0 ? profile.Warning : settingsMessage, new(36, 548, 988, 35), 14, Palette.Gold);
        ui.Button(panel, "返回", new(36, 599, 210, 40), NavigateBack, true).GrabFocus();
        ui.Button(panel, "恢复本页默认", new(766, 599, 258, 40), ConfirmSettingsReset);
        ui.Label(panel, "Tab 切换控件 · Esc 安全返回", new(274, 606, 465, 28), 15, Palette.Muted);
    }

    private void BuildAudioSettings(Control panel)
    {
        AddVolumeSlider(panel, "总音量", "master_volume", 211, profile.Data.MasterVolume, value => profile.Data.MasterVolume = value);
        AddVolumeSlider(panel, "音乐音量", "music_volume", 285, profile.Data.MusicVolume, value => profile.Data.MusicVolume = value);
        AddVolumeSlider(panel, "音效音量", "sound_volume", 359, profile.Data.SoundVolume, value => profile.Data.SoundVolume = value);
        ui.Button(panel, $"音乐：{(profile.Data.MusicEnabled ? "开启" : "静音")}", new(36, 440, 480, 44), () => { profile.Data.MusicEnabled = !profile.Data.MusicEnabled; SaveSettings(); });
        ui.Button(panel, $"音效：{(profile.Data.SoundEnabled ? "开启" : "静音")}", new(540, 440, 484, 44), () => { profile.Data.SoundEnabled = !profile.Data.SoundEnabled; SaveSettings(); });
        ui.Label(panel, "音量即时生效并自动保存；静音保留音量数值。左右键微调，Home / End 调至两端。", new(36, 502, 988, 32), 16, Palette.Muted);
    }

    private void AddVolumeSlider(Control panel, string title, string name, int vertical, float value, Action<float> update)
    {
        ui.Label(panel, title, new(36, vertical, 145, 35), 19);
        var percentage = ui.Label(panel, $"{value * 100:0}%", new(930, vertical, 94, 35), 18, Palette.Gold);
        var slider = new HSlider
        {
            Name = name, Position = new(210, vertical + 5), Size = new(682, 30),
            MinValue = 0, MaxValue = 100, Step = 1, Value = Math.Round(value * 100),
            FocusMode = Control.FocusModeEnum.All, TooltipText = title
        };
        slider.ValueChanged += volume => { update((float)volume / 100); percentage.Text = $"{volume:0}%"; SaveSettings(false); };
        panel.AddChild(slider);
    }

    private void SaveSettings(bool refresh = true)
    {
        profile.Save();
        if (IsInstanceValid(settingsWarning)) settingsWarning!.Text = profile.Warning.Length > 0 ? profile.Warning : settingsMessage;
        audio.Apply(profile.Data, !diagnosticMode);
        canvas.ReducedMotion = profile.Data.ReducedMotion;
        if (refresh) BuildSettings();
    }

    private void ConfirmSettingsReset()
    {
        captureAction = null;
        var panel = Modal("settings_reset", "RESET  /  恢复默认", "只重置当前页，不清除成绩。", 880, 365);
        ui.Label(panel, "声音恢复音量与静音开关；画面恢复显示选项与震屏；操作恢复双槽默认键位。\n只处理刚才所在的页。画面仍需预览确认。", new(36, 143, 808, 75), 18, Palette.Muted);
        ui.Button(panel, "取消", new(36, 267, 385, 44), BuildSettings, true).GrabFocus();
        ui.Button(panel, "确认恢复", new(449, 267, 395, 44), () =>
        {
            if (settingsTab == 1) { BeginVideoPreview(new(), true, false); return; }
            if (settingsTab == 0)
            {
                profile.Data.MasterVolume = profile.Data.MusicVolume = profile.Data.SoundVolume = 1;
                profile.Data.MusicEnabled = true;
                profile.Data.SoundEnabled = true;
            }
            else { profile.Data.Bindings = GameControls.DefaultBindings(); GameControls.Configure(profile.Data.Bindings); }
            settingsMessage = "当前页已恢复默认，成绩与其他页保持不变。";
            SaveSettings();
        });
    }
}
