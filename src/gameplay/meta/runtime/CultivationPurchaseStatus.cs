namespace TouhouWuxiaSurvivor.Gameplay.Meta.Runtime;

/// <summary>
/// 区分一次修行购买的成功状态与各类可向玩家明确解释的失败原因。
/// </summary>
public enum CultivationPurchaseStatus
{
    Purchased,
    Unknown,
    Locked,
    MaxRank,
    InsufficientJade,
    SaveFailed,
}
