using Godot;
using TouhouWuxiaSurvivor.Actors.Enemies;
using TouhouWuxiaSurvivor.Ecs.Core;

namespace TouhouWuxiaSurvivor.Ecs.Combat;

/// <summary>
/// 保存高数量敌人的运行时数据；定义对象只作为不可变平衡数据被引用。
/// </summary>
public struct EnemyComponent
{
    /// <summary>创建一份可直接加入连续敌人池的数据。</summary>
    public EnemyComponent(EcsEntity entity, Vector2 position, EnemyDefinition definition)
    {
        Entity = entity;
        Position = position;
        Definition = definition;
        Health = definition.MaxHealth;
        DeathTime = 0.0f;
        HurtTime = 0.0f;
        TouchCooldown = 0.0f;
        Alive = true;
    }

    /// <summary>获取实体句柄。</summary>
    public EcsEntity Entity;

    /// <summary>获取或设置局部世界位置。</summary>
    public Vector2 Position;

    /// <summary>获取敌人平衡定义。</summary>
    public EnemyDefinition Definition;

    /// <summary>获取或设置当前生命值。</summary>
    public int Health;

    /// <summary>获取或设置受击反馈剩余时间。</summary>
    public float HurtTime;

    /// <summary>获取或设置死亡文字反馈剩余时间。</summary>
    public float DeathTime;

    /// <summary>获取或设置接触伤害冷却。</summary>
    public float TouchCooldown;

    /// <summary>获取是否仍然可以移动、受伤和被索敌。</summary>
    public bool Alive;
}
