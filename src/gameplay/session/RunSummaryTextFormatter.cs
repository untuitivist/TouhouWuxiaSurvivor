namespace TouhouWuxiaSurvivor.Gameplay.Session;

/// <summary>
/// 集中处理本局总结的稳定文本格式，避免不同结算入口出现不同的时间与坐标写法。
/// </summary>
public static class RunSummaryTextFormatter
{
    /// <summary>
    /// 根据终局原因生成快速弹窗标题，明确区分规则战败与玩家主动结束。
    /// </summary>
    public static string FormatOutcomeTitle(RunEndReason reason) => reason switch
    {
        RunEndReason.Cleared => "异变平定",
        RunEndReason.Abandoned => "主动结束",
        _ => "符力耗尽",
    };

    /// <summary>
    /// 根据终局原因生成失败说明，主动离场必须明确告知本局已经按失败结算。
    /// </summary>
    public static string FormatOutcomeMessage(RunEndReason reason) => reason switch
    {
        RunEndReason.Cleared => "幻想乡暂归平静，本局已经按成功结算",
        RunEndReason.Abandoned => "本局探索已主动结束，并按失败结算",
        _ => "这次幻想乡之行已经结束",
    };

    /// <summary>
    /// 将生存秒数格式化为不受本地化影响的时分秒；不足一小时省略小时段。
    /// </summary>
    public static string FormatDuration(double seconds)
    {
        int totalSeconds = Math.Max(0, (int)Math.Floor(seconds));
        int hours = totalSeconds / 3600;
        int minutes = totalSeconds / 60 % 60;
        int remainingSeconds = totalSeconds % 60;
        return hours > 0
            ? $"{hours:00}:{minutes:00}:{remainingSeconds:00}"
            : $"{minutes:00}:{remainingSeconds:00}";
    }

    /// <summary>
    /// 生成适合失败弹出层快速浏览的核心战绩与本局收入。
    /// </summary>
    public static string FormatQuickStats(RunSummary summary) =>
        $"生存 {FormatDuration(summary.SurvivalSeconds)}    击破 {summary.DefeatedEnemies}" +
        $"    境界 {summary.FinalLevel}    钱 +{summary.RewardEarned}";
}
