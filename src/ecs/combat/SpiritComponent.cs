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
        _position = new InterpolatedPosition2D(position);
        Value = Math.Max(1, value);
        PulseTime = 0.0f;
    }

    /// <summary>获取实体句柄。</summary>
    public EcsEntity Entity;

    private InterpolatedPosition2D _position;

    /// <summary>获取或设置拾取判定使用的权威位置。</summary>
    public Vector2 Position
    {
        get => _position.Current;
        set => _position.Current = value;
    }

    /// <summary>在吸附移动前保存上一物理位置。</summary>
    public void BeginPhysicsStep() => _position.BeginPhysicsStep();

    /// <summary>同步平移前后位置，避免世界重定位产生视觉拖影。</summary>
    public void Translate(Vector2 offset) => _position.Translate(offset);

    /// <summary>按当前渲染帧比例读取平滑位置，不改变经验拾取逻辑。</summary>
    public readonly Vector2 GetRenderPosition(float fraction) => _position.Sample(fraction);

    /// <summary>获取或设置累计经验值。</summary>
    public int Value;

    /// <summary>获取或设置呼吸动画相位。</summary>
    public float PulseTime;
}
