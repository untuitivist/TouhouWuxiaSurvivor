using Godot;

namespace Rebirth.Presentation;

public partial class GameRoot
{
    private void BuildTouchSettings(Control panel)
    {
        AddVideoOption(panel, "触控按钮", "touch_mode", 209, ["自动检测", "始终显示", "隐藏"], profile.Data.TouchMode, selected =>
        {
            profile.Data.TouchMode = selected;
            SaveSettings(false);
        });
        ui.Label(panel, "左侧摇杆移动，右侧按住慢移、点按闪身。\n支持同时移动、慢移和闪身；抬手、暂停或切到后台会清除触控。\n右上角可暂停或查看构筑，升级时直接点击卡片。\n手机请横屏游玩；键盘设置与触控相互独立。", new(36, 285, 988, 145), 21);
        ui.Button(panel, "切换诊断信息（F3）", new(36, 453, 480, 52), ToggleDebug);
        ui.Label(panel, "诊断只读，不暂停，也不会改变战况。", new(544, 463, 480, 43), 17, Palette.Muted);
    }

    private void BuildWebVideoSettings(Control panel)
    {
        AddVideoOption(panel, "帧率上限", "fps_limit", 209,
            VideoPreferences.FrameLimits.Select(value => value == 0 ? "不限制" : $"{value} FPS").ToArray(),
            Array.IndexOf(VideoPreferences.FrameLimits, profile.Data.Video.MaxFps), selected =>
            {
                profile.Data.Video.MaxFps = VideoPreferences.FrameLimits[selected];
                profile.Data.Video.Apply();
                SaveSettings(false);
            });
        ui.Button(panel, $"减少震屏：{(profile.Data.ReducedMotion ? "开" : "关")}", new(36, 285, 480, 54), () =>
        {
            profile.Data.ReducedMotion = !profile.Data.ReducedMotion;
            SaveSettings();
        });
        ui.Button(panel, "切换浏览器全屏", new(544, 285, 480, 54), ToggleWebFullscreen);
        ui.Label(panel, "网页画面随浏览器大小适配。\n窗口分辨率、无边框窗口和垂直同步由浏览器管理，\n这里不显示无效的桌面设置。\n帧率与减少震屏即时保存；全屏是否可用取决于浏览器。", new(36, 374, 988, 146), 21, Palette.Muted);
    }

    private void ToggleWebFullscreen()
    {
        DisplayServer.WindowSetMode(DisplayServer.WindowGetMode() == DisplayServer.WindowMode.Fullscreen ? DisplayServer.WindowMode.Windowed : DisplayServer.WindowMode.Fullscreen);
    }
}
