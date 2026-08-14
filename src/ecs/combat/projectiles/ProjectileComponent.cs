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
        int visualVariant = 0,
        int maximumHits = 1,
        int secondaryHitDamage = -1,
        float hitDamageDecay = ProjectileDamageBudget.SecondaryHitMultiplier)
    {
        Entity = entity;
        Position = position;
        Velocity = velocity;
        Damage = damage;
        Lifetime = lifetime;
        Radius = radius;
        Faction = faction;
        VisualVariant = Math.Max(0, visualVariant);
        RemainingHits = Math.Max(1, maximumHits);
        NextHitDamage = RemainingHits > 1
            ? secondaryHitDamage >= 0
                ? secondaryHitDamage
                : ProjectileDamageBudget.ScaleDamage(Damage, hitDamageDecay)
            : 0;
        HitDamageDecay = float.IsFinite(hitDamageDecay)
            ? Math.Clamp(hitDamageDecay, 0.0f, 1.0f)
            : ProjectileDamageBudget.SecondaryHitMultiplier;
        LastHitIdentity = 0;
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

    /// <summary>获取弹丸在回收前仍可造成伤害的次数，普通弹为一，贯穿弹大于一。</summary>
    public int RemainingHits;

    /// <summary>获取下一名敌人命中时使用的已分配伤害，首击后才会移入当前伤害。</summary>
    public int NextHitDamage;

    /// <summary>获取第三次及以后命中相对上一次伤害的衰减比例。</summary>
    public float HitDamageDecay;

    /// <summary>记录上一名命中对象的稳定身份，防止连续帧对同一重叠目标重复扣血。</summary>
    public ulong LastHitIdentity;

    /// <summary>
    /// 消费一次命中后推进到次级伤害；后续每次继续衰减，且零伤害不会重新抬升为一点。
    /// </summary>
    public void AdvanceHitDamage()
    {
        Damage = NextHitDamage;
        NextHitDamage = ProjectileDamageBudget.ScaleDamage(
            NextHitDamage, HitDamageDecay);
    }
}
