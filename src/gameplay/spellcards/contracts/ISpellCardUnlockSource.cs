using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;

namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Contracts;

/// <summary>
/// 定义本局已悟奥义的只读来源，使计时器不直接查询全局目录、内容选择或可变构筑实现。
/// </summary>
public interface ISpellCardUnlockSource
{
    /// <summary>按稳定目录顺序返回当前已经悟得且属于启用内容的奥义。</summary>
    IReadOnlyList<SpellCardDefinition> GetUnlockedCards();
}
