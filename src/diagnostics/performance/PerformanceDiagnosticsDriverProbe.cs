namespace TouhouWuxiaSurvivor.Diagnostics.Performance;

/// <summary>
/// 明确记录进程内驱动版本查询被跳过；详细驱动证据由同会话 Godot verbose 日志提供。
/// </summary>
public static class PerformanceDiagnosticsDriverProbe
{
    /// <summary>
    /// 创建稳定的跳过记录，避免 OS 驱动 API 在启动时卡顿或在退出时跨越 Godot 生命周期。
    /// </summary>
    public static PerformanceDiagnosticsDriverRecord CreateSkippedRecord()
    {
        return new PerformanceDiagnosticsDriverRecord
        {
            TimestampUtc = DateTimeOffset.UtcNow.ToString("O"),
            VideoDriverInfo = "omitted_in_process_use_godot_verbose_log",
            ProbeMilliseconds = 0.0,
        };
    }
}
