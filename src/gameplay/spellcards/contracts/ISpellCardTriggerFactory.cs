using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;

namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Contracts;

/// <summary>
/// 为每张已悟奥义建立独立触发策略实例，隔离协调器与具体触发类型的构造规则。
/// </summary>
public interface ISpellCardTriggerFactory
{
    /// <summary>依据奥义定义建立只归该卡持有的有状态触发判定器。</summary>
    ISpellCardTrigger Create(SpellCardDefinition card);
}
