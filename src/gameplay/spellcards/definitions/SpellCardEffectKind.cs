namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;

/// <summary>
/// 标识可复用的符卡效果原型；新增数据只组合原型与数值，真正的新机制才需要增加执行类。
/// </summary>
public enum SpellCardEffectKind
{
    HomingVolley,
    FocusedVolley,
    AreaBurst,
    GuardField,
}
