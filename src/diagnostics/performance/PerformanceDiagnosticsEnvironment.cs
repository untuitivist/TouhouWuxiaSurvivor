namespace TouhouWuxiaSurvivor.Diagnostics.Performance;

/// <summary>
/// 保存诊断会话只采集一次的系统、显卡、渲染器、窗口和设置迁移信息。
/// </summary>
public sealed class PerformanceDiagnosticsEnvironment
{
    public string Type { get; init; } = "environment";
    public string TimestampUtc { get; init; } = string.Empty;
    public string SessionLabel { get; init; } = string.Empty;
    public string GameVersion { get; init; } = string.Empty;
    public bool DebugBuild { get; init; }
    public string OperatingSystem { get; init; } = string.Empty;
    public string OperatingSystemVersion { get; init; } = string.Empty;
    public string CpuName { get; init; } = string.Empty;
    public int LogicalCpuCount { get; init; }
    public string Architecture { get; init; } = string.Empty;
    public string DisplayDriver { get; init; } = string.Empty;
    public string RenderingMethod { get; init; } = string.Empty;
    public string RenderingDriver { get; init; } = string.Empty;
    public string VideoAdapterName { get; init; } = string.Empty;
    public string VideoAdapterVendor { get; init; } = string.Empty;
    public string VideoAdapterType { get; init; } = string.Empty;
    public string VideoApiVersion { get; init; } = string.Empty;
    public string VideoDriverInfo { get; init; } = string.Empty;
    public double DriverProbeMilliseconds { get; init; }
    public int ScreenWidth { get; init; }
    public int ScreenHeight { get; init; }
    public double ScreenRefreshRate { get; init; }
    public int WindowWidth { get; init; }
    public int WindowHeight { get; init; }
    public string WindowMode { get; init; } = string.Empty;
    public string VsyncMode { get; init; } = string.Empty;
    public bool Borderless { get; init; }
    public int PersistedMaxFps { get; init; }
    public int EngineMaxFps { get; init; }
    public int OriginalMaxFps { get; init; }
    public int OriginalResolutionWidth { get; init; }
    public int OriginalResolutionHeight { get; init; }
    public bool VideoSettingsRepaired { get; init; }
    public int PhysicsTicksPerSecond { get; init; }
}
