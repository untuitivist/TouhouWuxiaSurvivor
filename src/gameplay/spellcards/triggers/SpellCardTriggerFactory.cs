using TouhouWuxiaSurvivor.Gameplay.SpellCards.Contracts;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;

namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Triggers;

/// <summary>
/// 将内容层触发枚举映射到独立策略实例，是具体触发类型唯一集中的组合入口。
/// </summary>
public sealed class SpellCardTriggerFactory : ISpellCardTriggerFactory
{
    /// <summary>为每张卡创建全新策略，保证受击序号、评估节流和锁存信号不会跨卡共享。</summary>
    public ISpellCardTrigger Create(SpellCardDefinition card)
    {
        ArgumentNullException.ThrowIfNull(card);
        return card.ActivationKind switch
        {
            SpellCardActivationKind.Periodic => new PeriodicSpellCardTrigger(),
            SpellCardActivationKind.Crowd => new CrowdSpellCardTrigger(),
            SpellCardActivationKind.OnDamaged => new OnDamagedSpellCardTrigger(),
            _ => throw new ArgumentOutOfRangeException(
                nameof(card), card.ActivationKind, "Unknown spell activation kind."),
        };
    }
}
