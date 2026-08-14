namespace TouhouWuxiaSurvivor.Gameplay.Progression.Definitions;

/// <summary>
/// 集中提供构筑亲和的短中文名，避免界面和测试各自维护显示映射。
/// </summary>
public static class RunUpgradeAffinityFormatter
{
    /// <summary>
    /// 返回适合紧凑三选一界面的单字亲和名称。
    /// </summary>
    public static string Format(RunUpgradeAffinity affinity) => affinity switch
    {
        RunUpgradeAffinity.Force => "刚",
        RunUpgradeAffinity.Precision => "巧",
        RunUpgradeAffinity.Swiftness => "疾",
        RunUpgradeAffinity.Formation => "阵",
        RunUpgradeAffinity.Guard => "御",
        _ => "杂",
    };

    /// <summary>
    /// 按定义顺序拼接一个或多个亲和标签，空集合稳定显示为无倾向。
    /// </summary>
    public static string FormatMany(IEnumerable<RunUpgradeAffinity> affinities)
    {
        string[] names = affinities.Distinct().Select(Format).ToArray();
        return names.Length == 0 ? "无倾向" : string.Join("·", names);
    }
}
