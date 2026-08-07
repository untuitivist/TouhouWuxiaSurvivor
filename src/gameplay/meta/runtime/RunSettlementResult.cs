namespace TouhouWuxiaSurvivor.Gameplay.Meta.Runtime;

/// <summary>
/// 表示一次局外结算是否首次持久化，以及实际收入和结算后的钱财余额。
/// </summary>
public sealed class RunSettlementResult
{
    public bool WasSettled { get; }
    public int Reward { get; }
    public int Balance { get; }

    /// <summary>
    /// 建立不可变结算结果，重复或保存失败的结算使用零奖励和当前余额。
    /// </summary>
    public RunSettlementResult(bool wasSettled, int reward, int balance)
    {
        WasSettled = wasSettled;
        Reward = Math.Max(0, reward);
        Balance = Math.Max(0, balance);
    }
}
