using Godot;
using TouhouWuxiaSurvivor.Actors.Enemies;
using TouhouWuxiaSurvivor.Ecs.Core;

namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Effects;

/// <summary>
/// 保存一次符卡选敌得到的稳定身份与施放瞬间坐标；正式 ECS 使用实体句柄，兼容模式使用节点实例。
/// </summary>
public sealed class SpellCardTargetReference
{
    private SpellCardTargetReference(
        Vector2 initialPosition,
        EcsEntity ecsEntity,
        EnemyActor? legacyActor)
    {
        InitialPosition = initialPosition;
        EcsEntity = ecsEntity;
        LegacyActor = legacyActor;
    }

    /// <summary>获取几何规划读取的施放瞬间坐标；后续移动不会反向修改已经完成的规划。</summary>
    public Vector2 InitialPosition { get; }

    /// <summary>获取正式 ECS 目标的稳定实体句柄；兼容节点目标在此保存无效句柄。</summary>
    internal EcsEntity EcsEntity { get; }

    /// <summary>获取兼容模式目标的稳定节点实例；正式 ECS 目标在此保持为空。</summary>
    internal EnemyActor? LegacyActor { get; }

    /// <summary>为正式 ECS 敌人建立跨帧引用，不保存会被尾部交换改变的池索引。</summary>
    public static SpellCardTargetReference FromEcs(
        EcsEntity entity,
        Vector2 initialPosition)
    {
        if (!entity.IsValid)
        {
            throw new ArgumentException("Tracking target requires a valid ECS entity.",
                nameof(entity));
        }

        return new SpellCardTargetReference(initialPosition, entity, null);
    }

    /// <summary>为旧场景敌人建立节点身份引用，不依赖可重复的显示名或瞬时坐标。</summary>
    public static SpellCardTargetReference FromLegacy(EnemyActor actor)
    {
        ArgumentNullException.ThrowIfNull(actor);
        return new SpellCardTargetReference(actor.GlobalPosition, default, actor);
    }
}
