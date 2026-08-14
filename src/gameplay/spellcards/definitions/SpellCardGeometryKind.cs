namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;

/// <summary>
/// 描述符卡如何在空间中组织同一份伤害预算；几何只改变选敌、起手与轨迹，不额外增加伤害或目标数。
/// </summary>
public enum SpellCardGeometryKind
{
    Orbit,
    Fan,
    Line,
    Ring,
    Backstab,
}
