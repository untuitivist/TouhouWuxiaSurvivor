using TouhouWuxiaSurvivor.Gameplay.Meta.Definitions;

namespace TouhouWuxiaSurvivor.Gameplay.Meta.Runtime;

/// <summary>
/// 保存购买结果及相关定义，使界面不需要复制购买规则或猜测失败原因。
/// </summary>
public sealed class CultivationPurchaseResult
{
    public CultivationPurchaseStatus Status { get; }
    public CultivationDefinition? Definition { get; }
    public bool Succeeded => Status == CultivationPurchaseStatus.Purchased;

    /// <summary>
    /// 建立一次不可变购买结果，并允许未知 ID 没有关联定义。
    /// </summary>
    public CultivationPurchaseResult(
        CultivationPurchaseStatus status,
        CultivationDefinition? definition)
    {
        Status = status;
        Definition = definition;
    }
}
