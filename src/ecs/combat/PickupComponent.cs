using Godot;
using TouhouWuxiaSurvivor.Actors.Pickups;
using TouhouWuxiaSurvivor.Ecs.Core;

namespace TouhouWuxiaSurvivor.Ecs.Combat;

/// <summary>保存临时强化掉落物的位置、定义和剩余寿命。</summary>
public struct PickupComponent
{
    /// <summary>创建一份默认十四秒寿命的强化掉落数据。</summary>
    public PickupComponent(EcsEntity entity, Vector2 position, PickupDefinition definition)
    {
        Entity = entity;
        Position = position;
        Definition = definition;
        Lifetime = 14.0f;
        BlinkTime = 0.0f;
    }

    /// <summary>获取实体句柄。</summary>
    public EcsEntity Entity;

    /// <summary>获取或设置位置。</summary>
    public Vector2 Position;

    /// <summary>获取强化定义。</summary>
    public PickupDefinition Definition;

    /// <summary>获取或设置剩余寿命。</summary>
    public float Lifetime;

    /// <summary>获取或设置闪烁相位。</summary>
    public float BlinkTime;
}
