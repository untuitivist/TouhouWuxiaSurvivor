namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;

/// <summary>
/// 标识无资源消耗奥义的自动运转方式；只决定施展时机，不保存费用、最终数值或效果逻辑。
/// </summary>
public enum SpellCardActivationKind
{
    Periodic,
    Crowd,
    OnDamaged,
}
