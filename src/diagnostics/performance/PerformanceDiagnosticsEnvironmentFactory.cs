using Godot;
using TouhouWuxiaSurvivor.Settings;

namespace TouhouWuxiaSurvivor.Diagnostics.Performance;

/// <summary>
/// 从 Godot 和设置服务采集一次性环境证据，不调用可能阻塞启动或退出的驱动版本查询。
/// </summary>
public static class PerformanceDiagnosticsEnvironmentFactory
{
    /// <summary>
    /// 创建完整环境快照；适配器与 API 来自渲染服务，详细驱动信息由 verbose 引擎日志补充。
    /// </summary>
    public static PerformanceDiagnosticsEnvironment Capture(string sessionLabel)
    {
        GameSettingsData settings = GameSettingsService.Current;
        GameSettingsRepairReport repair = GameSettingsService.LastVideoRepair;
        bool headless = DisplayServer.GetName() == "headless";
        Vector2I screenSize = headless ? Vector2I.Zero : DisplayServer.ScreenGetSize();
        Vector2I windowSize = headless ? Vector2I.Zero : DisplayServer.WindowGetSize();
        return new PerformanceDiagnosticsEnvironment
        {
            TimestampUtc = DateTimeOffset.UtcNow.ToString("O"),
            SessionLabel = sessionLabel,
            GameVersion = ProjectSettings.GetSetting("application/config/version").AsString(),
            DebugBuild = OS.IsDebugBuild(),
            OperatingSystem = OS.GetName(),
            OperatingSystemVersion = OS.GetVersion(),
            CpuName = OS.GetProcessorName(),
            LogicalCpuCount = OS.GetProcessorCount(),
            Architecture = Engine.GetArchitectureName(),
            DisplayDriver = DisplayServer.GetName(),
            RenderingMethod = RenderingServer.GetCurrentRenderingMethod(),
            RenderingDriver = RenderingServer.GetCurrentRenderingDriverName(),
            VideoAdapterName = RenderingServer.GetVideoAdapterName(),
            VideoAdapterVendor = RenderingServer.GetVideoAdapterVendor(),
            VideoAdapterType = RenderingServer.GetVideoAdapterType().ToString(),
            VideoApiVersion = RenderingServer.GetVideoAdapterApiVersion(),
            VideoDriverInfo = "see_driver_record_and_godot_verbose_log",
            DriverProbeMilliseconds = 0.0,
            ScreenWidth = screenSize.X,
            ScreenHeight = screenSize.Y,
            ScreenRefreshRate = headless ? 0.0 : DisplayServer.ScreenGetRefreshRate(),
            WindowWidth = windowSize.X,
            WindowHeight = windowSize.Y,
            WindowMode = headless ? "headless" : DisplayServer.WindowGetMode().ToString(),
            VsyncMode = headless ? "headless" : DisplayServer.WindowGetVsyncMode().ToString(),
            Borderless = !headless && DisplayServer.WindowGetFlag(DisplayServer.WindowFlags.Borderless),
            PersistedMaxFps = settings.MaxFps,
            EngineMaxFps = Engine.MaxFps,
            OriginalMaxFps = repair.OriginalMaxFps,
            OriginalResolutionWidth = repair.OriginalWidth,
            OriginalResolutionHeight = repair.OriginalHeight,
            VideoSettingsRepaired = repair.Changed,
            PhysicsTicksPerSecond = Engine.PhysicsTicksPerSecond,
        };
    }

}
