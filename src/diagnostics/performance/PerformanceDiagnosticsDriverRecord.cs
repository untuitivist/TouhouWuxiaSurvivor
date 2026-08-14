namespace TouhouWuxiaSurvivor.Diagnostics.Performance;

/// <summary>
/// 保存后台显卡驱动探测结果，使可能耗时数秒的系统查询不会阻塞首帧或污染帧时样本。
/// </summary>
public sealed class PerformanceDiagnosticsDriverRecord
{
    public string Type { get; init; } = "driver";
    public string TimestampUtc { get; init; } = string.Empty;
    public string VideoDriverInfo { get; init; } = string.Empty;
    public double ProbeMilliseconds { get; init; }
}
