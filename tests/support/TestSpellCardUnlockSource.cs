using TouhouWuxiaSurvivor.Gameplay.SpellCards.Contracts;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;

namespace TouhouWuxiaSurvivor.Tests.Support;

/// <summary>
/// 提供可替换且保持顺序的已悟奥义集合，用于隔离协调器与真实构筑仓库。
/// </summary>
public sealed class TestSpellCardUnlockSource : ISpellCardUnlockSource
{
    private IReadOnlyList<SpellCardDefinition> _cards = [];

    /// <summary>以一份不可变快照替换当前已悟集合，避免调用方随后修改传入数组。</summary>
    public void SetCards(params SpellCardDefinition[] cards)
    {
        ArgumentNullException.ThrowIfNull(cards);
        if (cards.Any(card => card is null))
        {
            throw new ArgumentException("Unlocked test cards cannot contain null.", nameof(cards));
        }

        _cards = cards.ToArray();
    }

    /// <summary>返回当前已悟奥义的稳定只读快照，顺序与测试设置时完全一致。</summary>
    public IReadOnlyList<SpellCardDefinition> GetUnlockedCards() => _cards;
}
