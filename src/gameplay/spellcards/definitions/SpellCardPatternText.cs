namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;

/// <summary>
/// 为原作演出语法提供统一中文名称，使图鉴和构筑页面不各自维护一套易漂移文案。
/// </summary>
public static class SpellCardPatternText
{
    /// <summary>返回玩家可读的演出名称；旧库存会明确显示尚未逐卡校对。</summary>
    public static string GetName(SpellCardPatternKind kind) => kind switch
    {
        SpellCardPatternKind.LegacyGeometry => "通用几何（待校对）",
        SpellCardPatternKind.HomingOrbit => "多玉追踪",
        SpellCardPatternKind.SealPulse => "结界脉冲",
        SpellCardPatternKind.StraightBeam => "贯通魔炮",
        SpellCardPatternKind.StardustFan => "星屑扇流",
        SpellCardPatternKind.AimedArc => "瞄准弧列",
        SpellCardPatternKind.FreezeRelease => "冻结再启动",
        SpellCardPatternKind.RotatingStream => "旋流连射",
        SpellCardPatternKind.ElementalCycle => "五行轮换",
        SpellCardPatternKind.TimeStopRedirect => "时停转向",
        SpellCardPatternKind.AimedTrail => "主弹曳尾",
        SpellCardPatternKind.SweepingBeam => "扫掠光束",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };
}
