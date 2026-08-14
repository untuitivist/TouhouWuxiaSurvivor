using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;

namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Contracts;

/// <summary>
/// 定义定时协调器所需的最小奥义执行边界，使计时层不依赖具体玩家、ECS 或视觉场景。
/// </summary>
public interface ISpellCardEffectExecutor
{
    /// <summary>按当前基础属性把内容系数解析为一次施展的一致最终数值。</summary>
    ResolvedSpellCardCombat Resolve(SpellCardDefinition card);

    /// <summary>尝试执行已经解析的奥义；缺少必要目标时返回 false 且不得结算或重置周期。</summary>
    bool TryCast(SpellCardDefinition card, ResolvedSpellCardCombat resolved);
}
