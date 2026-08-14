using TouhouWuxiaSurvivor.Gameplay.Progression.Definitions;
using TouhouWuxiaSurvivor.Gameplay.Progression.Runtime;

namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;

/// <summary>
/// 集中定义一局可携带的奥义容量与分类规则，使内容包只能横向扩充候选而不能增加总战力槽位。
/// </summary>
public static class SpellCardSlotPolicy
{
    public const int MaximumOffensiveSlots = 4;
    public const int MaximumSupportSlots = 2;

    /// <summary>
    /// 把会提供无敌时间的护身结界归入护持槽，其余追踪、集中与范围伤害均归入主攻槽。
    /// </summary>
    public static SpellCardSlotKind Classify(SpellCardDefinition card) =>
        card.EffectKind == SpellCardEffectKind.GuardField
            ? SpellCardSlotKind.Support
            : SpellCardSlotKind.Offensive;

    /// <summary>
    /// 统计构筑中已经悟得的指定类型奥义；每张卡只占一个槽，不受其他升级重数影响。
    /// </summary>
    public static int CountOccupied(RunBuildState build, SpellCardSlotKind kind) =>
        SpellCardCatalog.All.Count(card =>
            Classify(card) == kind && build.GetRank(card.UnlockUpgradeId) > 0);

    /// <summary>
    /// 返回新奥义能否进入对应容量；已经持有的卡不重复占槽，方便未来接入显式升重或替换界面。
    /// </summary>
    public static bool HasCapacity(RunBuildState build, SpellCardDefinition card)
    {
        if (build.GetRank(card.UnlockUpgradeId) > 0)
        {
            return true;
        }

        SpellCardSlotKind kind = Classify(card);
        int maximum = kind == SpellCardSlotKind.Support
            ? MaximumSupportSlots
            : MaximumOffensiveSlots;
        return CountOccupied(build, kind) < maximum;
    }

    /// <summary>
    /// 为升级界面返回可直接展示的容量阻断说明；空值表示槽位策略没有阻止这次选择。
    /// </summary>
    public static string? GetBlockReason(RunBuildState build, SpellCardDefinition card)
    {
        if (HasCapacity(build, card))
        {
            return null;
        }

        return Classify(card) == SpellCardSlotKind.Support
            ? $"护持奥义已满（{MaximumSupportSlots}/{MaximumSupportSlots}），本局不能再悟得新护持"
            : $"主攻奥义已满（{MaximumOffensiveSlots}/{MaximumOffensiveSlots}），本局不能再悟得新主攻";
    }

    /// <summary>
    /// 从升级定义解析正式奥义；非奥义定义返回空，损坏的奥义 ID 也不会被误判成免费槽位。
    /// </summary>
    public static SpellCardDefinition? Resolve(RunUpgradeDefinition definition) =>
        definition.Category == RunUpgradeCategory.SpellCard &&
        !string.IsNullOrWhiteSpace(definition.SpellCardId)
            ? SpellCardCatalog.FindById(definition.SpellCardId)
            : null;
}
