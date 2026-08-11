using Godot;
using TouhouWuxiaSurvivor.Ecs.Core;

namespace TouhouWuxiaSurvivor.Ecs.Combat.Projectiles;

/// <summary>
/// 保存一颗带阵营投射物的纯数据；高频更新不依赖 Godot 节点和信号。
/// </summary>
public struct ProjectileComponent
{
    /// <summary>
    /// 创建投射物的初始位置、速度、伤害和碰撞半径。
    /// </summary>
    public ProjectileComponent(
        EcsEntity entity,
        Vector2 position,
        Vector2 velocity,
        int damage,
        float lifetime,
        float radius,
        ProjectileFaction faction = ProjectileFaction.Player,
        int visualVariant = 0)
    {
        Entity = entity;
        Position = position;
        Velocity = velocity;
        Damage = damage;
        Lifetime = lifetime;
        Radius = radius;
        Faction = faction;
        VisualVariant = Math.Max(0, visualVariant);
    }

    /// <summary>获取投射物对应的实体句柄。</summary>
    public EcsEntity Entity;

    /// <summary>获取或设置投射物的局部世界位置。</summary>
    public Vector2 Position;

    /// <summary>获取或设置每秒位移向量。</summary>
    public Vector2 Velocity;

    /// <summary>获取命中敌人时应用的伤害值。</summary>
    public int Damage;

    /// <summary>获取剩余寿命，归零后由生命周期系统回收。</summary>
    public float Lifetime;

    /// <summary>获取用于圆形距离检测的投射物半径。</summary>
    public float Radius;

    /// <summary>获取投射物的伤害阵营，碰撞系统不会允许同阵营误伤。</summary>
    public ProjectileFaction Faction;

    /// <summary>获取弹幕图集的稳定视觉变体，用于区分玩家弹和多种敌方弹幕。</summary>
    public int VisualVariant;
}
