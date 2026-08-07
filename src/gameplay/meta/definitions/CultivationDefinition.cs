namespace TouhouWuxiaSurvivor.Gameplay.Meta.Definitions;

/// <summary>
/// 描述一项可购买整备的名称、效果、重数、费用曲线和累计收入解锁条件。
/// </summary>
public sealed class CultivationDefinition
{
    public string Id { get; }
    public string DisplayName { get; }
    public string Description { get; }
    public CultivationKind Kind { get; }
    public int MaxRank { get; }
    public int BaseCost { get; }
    public int CostGrowth { get; }
    public int UnlockLifetimeMoney { get; }

    /// <summary>
    /// 建立一项不可变修行定义，使目录、购买规则和界面共享同一份策划数据。
    /// </summary>
    public CultivationDefinition(
        string id,
        string displayName,
        string description,
        CultivationKind kind,
        int maxRank,
        int baseCost,
        int costGrowth,
        int unlockLifetimeMoney)
    {
        Id = id;
        DisplayName = displayName;
        Description = description;
        Kind = kind;
        MaxRank = Math.Max(1, maxRank);
        BaseCost = Math.Max(1, baseCost);
        CostGrowth = Math.Max(0, costGrowth);
        UnlockLifetimeMoney = Math.Max(0, unlockLifetimeMoney);
    }

    /// <summary>
    /// 按购买前当前重数计算下一重费用，已满重时仍返回稳定的末重费用供展示使用。
    /// </summary>
    public int GetCost(int currentRank) =>
        BaseCost + Math.Clamp(currentRank, 0, MaxRank - 1) * CostGrowth;
}
