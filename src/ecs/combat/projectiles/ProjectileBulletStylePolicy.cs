using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;

namespace TouhouWuxiaSurvivor.Ecs.Combat.Projectiles;

/// <summary>
/// 为没有奥义身份的普通投射物分配美术语义，使双方通道共享规则而不复制图集坐标。
/// </summary>
public static class ProjectileBulletStylePolicy
{
    /// <summary>玩家普通弹与中心弹幕使用针和星；敌弹按变体轮换四种易辨认轮廓。</summary>
    public static SpellBulletStyleKind Resolve(ProjectileFaction faction, int visualVariant)
    {
        if (faction == ProjectileFaction.Player)
        {
            return PositiveModulo(visualVariant, 2) == 0
                ? SpellBulletStyleKind.Needle
                : SpellBulletStyleKind.Star;
        }

        return PositiveModulo(visualVariant, 4) switch
        {
            0 => SpellBulletStyleKind.Orb,
            1 => SpellBulletStyleKind.Shard,
            2 => SpellBulletStyleKind.Needle,
            _ => SpellBulletStyleKind.Star,
        };
    }

    /// <summary>返回不会因负视觉变体产生负通道索引的数学模。</summary>
    private static int PositiveModulo(int value, int divisor)
    {
        int result = value % divisor;
        return result < 0 ? result + divisor : result;
    }
}
