namespace TouhouWuxiaSurvivor.Ecs.Combat.Projectiles;

/// <summary>
/// 标识投射物的伤害阵营，确保玩家弹只命中敌方、敌弹只命中玩家。
/// </summary>
public enum ProjectileFaction
{
    /// <summary>由自机武学或符卡生成、只伤害敌人的投射物。</summary>
    Player,

    /// <summary>由普通敌人或角色 Boss 生成、只伤害玩家的投射物。</summary>
    Enemy,
}
