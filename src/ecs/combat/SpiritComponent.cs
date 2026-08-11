using Godot;
using TouhouWuxiaSurvivor.Ecs.Core;

namespace TouhouWuxiaSurvivor.Ecs.Combat;

/// <summary>保存可合并、可吸附的经验灵息数据。</summary>
public struct SpiritComponent
{
    /// <summary>创建一份最低价值为一点的灵息数据。</summary>
    public SpiritComponent(EcsEntity entity, Vector2 position, int value)
    {
        Entity = entity;
        Position = position;
        Value = Math.Max(1, value);
        PulseTime = 0.0f;
    }

    /// <summary>获取实体句柄。</summary>
    public EcsEntity Entity;

    /// <summary>获取或设置位置。</summary>
    public Vector2 Position;

    /// <summary>获取或设置累计经验值。</summary>
    public int Value;

    /// <summary>获取或设置呼吸动画相位。</summary>
    public float PulseTime;
}
