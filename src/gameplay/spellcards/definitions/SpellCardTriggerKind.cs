namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;

/// <summary>
/// 标识符卡自动决策条件，保证所有奥义只由战况触发而不增加玩家主动按键。
/// </summary>
public enum SpellCardTriggerKind
{
    Crowd,
    Danger,
    SingleTarget,
}
