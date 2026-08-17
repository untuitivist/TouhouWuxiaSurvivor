namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;

/// <summary>
/// 描述符卡跨越多帧和多波次的原作演出语法；效果、空间几何与弹型仍由各自字段独立负责。
/// </summary>
public enum SpellCardPatternKind
{
    LegacyGeometry,
    HomingOrbit,
    SealPulse,
    StraightBeam,
    StardustFan,
    AimedArc,
    FreezeRelease,
    RotatingStream,
    ElementalCycle,
    TimeStopRedirect,
    AimedTrail,
    SweepingBeam,
}
