namespace TouhouWuxiaSurvivor.Gameplay.Meta.Runtime;

/// <summary>
/// 为单局持有开局档案投影和唯一结算 ID，把磁盘管理与奖励幂等性隔离在世界之外。
/// </summary>
public sealed class MetaRunSession
{
    private readonly ProgressionProfileManager _profileManager;
    private readonly string _runId;
    public ProfileRunBonuses Bonuses { get; }

    /// <summary>
    /// 使用可注入管理器和运行 ID 建立单局会话；正式运行默认加载用户档并生成随机 ID。
    /// </summary>
    public MetaRunSession(
        ProgressionProfileManager? profileManager = null,
        string? runId = null)
    {
        _profileManager = profileManager ?? ProgressionProfileManager.CreateDefault();
        _runId = string.IsNullOrWhiteSpace(runId)
            ? Guid.NewGuid().ToString("N")
            : runId;
        Bonuses = new ProfileRunBonuses(_profileManager.Current);
    }

    /// <summary>
    /// 计算最终战绩的钱财并以本局唯一 ID 结算，重复调用不会重复增加档案货币。
    /// </summary>
    public RunSettlementResult Settle(
        double survivalSeconds,
        int defeatedEnemies,
        int finalLevel)
    {
        int reward = RunRewardCalculator.Calculate(
            survivalSeconds,
            defeatedEnemies,
            finalLevel);
        return _profileManager.SettleRun(_runId, reward);
    }
}
