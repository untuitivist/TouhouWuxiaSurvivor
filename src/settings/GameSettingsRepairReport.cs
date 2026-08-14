namespace TouhouWuxiaSurvivor.Settings;

/// <summary>
/// 保存视频设置规范化前后的关键值，使诊断日志能够证明旧存档是否曾锁定异常帧率或尺寸。
/// </summary>
public sealed record GameSettingsRepairReport(
    int OriginalWidth,
    int OriginalHeight,
    int AppliedWidth,
    int AppliedHeight,
    int OriginalMaxFps,
    int AppliedMaxFps)
{
    /// <summary>
    /// 当分辨率或帧率上限被修复时返回 true，供设置服务决定是否立即回写迁移结果。
    /// </summary>
    public bool Changed =>
        OriginalWidth != AppliedWidth ||
        OriginalHeight != AppliedHeight ||
        OriginalMaxFps != AppliedMaxFps;

    /// <summary>
    /// 创建默认的未修复报告，保证设置服务尚未初始化时诊断代码仍能安全读取。
    /// </summary>
    public static GameSettingsRepairReport UnchangedDefaults() => new(
        VideoSettingsCatalog.DefaultResolution.X,
        VideoSettingsCatalog.DefaultResolution.Y,
        VideoSettingsCatalog.DefaultResolution.X,
        VideoSettingsCatalog.DefaultResolution.Y,
        VideoSettingsCatalog.DefaultMaxFps,
        VideoSettingsCatalog.DefaultMaxFps);
}
