namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;

/// <summary>
/// 区分直接造成伤害的主攻奥义与提供护身收益的护持奥义，供构筑容量独立计数。
/// </summary>
public enum SpellCardSlotKind
{
    Offensive,
    Support,
}
