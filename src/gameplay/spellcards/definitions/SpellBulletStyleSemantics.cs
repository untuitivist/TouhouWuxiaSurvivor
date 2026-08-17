namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;

/// <summary>
/// 集中解释奥义弹型的中文语义与姿态规则，使战斗绘制和图鉴说明读取同一契约。
/// </summary>
public static class SpellBulletStyleSemantics
{
    /// <summary>返回玩家可辨认的弹型名称，不暴露内容清单中的英文枚举值。</summary>
    public static string GetDisplayName(SpellBulletStyleKind style) => style switch
    {
        SpellBulletStyleKind.Orb => "灵玉",
        SpellBulletStyleKind.Amulet => "灵符",
        SpellBulletStyleKind.Needle => "御针",
        SpellBulletStyleKind.Knife => "飞刀",
        SpellBulletStyleKind.Star => "星弹",
        SpellBulletStyleKind.Flame => "焰弹",
        SpellBulletStyleKind.Butterfly => "蝶弹",
        SpellBulletStyleKind.Laser => "光束",
        SpellBulletStyleKind.Shard => "碎晶",
        SpellBulletStyleKind.LargeOrb => "大玉",
        _ => throw new ArgumentOutOfRangeException(nameof(style)),
    };

    /// <summary>以显式跳转表检查高频视觉编号，避免每颗弹调用反射或误收未来枚举空洞。</summary>
    public static bool IsDefined(int value) => (SpellBulletStyleKind)value is
        SpellBulletStyleKind.Orb or
        SpellBulletStyleKind.Amulet or
        SpellBulletStyleKind.Needle or
        SpellBulletStyleKind.Knife or
        SpellBulletStyleKind.Star or
        SpellBulletStyleKind.Flame or
        SpellBulletStyleKind.Butterfly or
        SpellBulletStyleKind.Laser or
        SpellBulletStyleKind.Shard or
        SpellBulletStyleKind.LargeOrb;

    /// <summary>判断轮廓是否具有明确前后方向，方向型弹丸必须随实时速度转身。</summary>
    public static bool IsDirectional(SpellBulletStyleKind style) => style is
        SpellBulletStyleKind.Amulet or
        SpellBulletStyleKind.Needle or
        SpellBulletStyleKind.Knife or
        SpellBulletStyleKind.Flame or
        SpellBulletStyleKind.Butterfly or
        SpellBulletStyleKind.Laser or
        SpellBulletStyleKind.Shard;

    /// <summary>返回图鉴中的姿态说明，区分实时朝向与对称轮廓的稳定朝向。</summary>
    public static string DescribePose(SpellBulletStyleKind style) => IsDirectional(style)
        ? "姿态随实时速度转向"
        : "对称轮廓保持稳定朝向";
}
