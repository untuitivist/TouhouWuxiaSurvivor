namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;

/// <summary>
/// 区分原作正式符卡与 PC-98 时代按原作攻击意象拟制的武侠化招式，避免来源描述误导玩家。
/// </summary>
public enum SpellCardCanonLevel
{
    Official,
    AdaptedPreSpellCard,
}
