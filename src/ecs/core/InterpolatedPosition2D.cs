using Godot;

namespace TouhouWuxiaSurvivor.Ecs.Core;

/// <summary>
/// 保存固定物理步的前后二维位置，并为高刷新率渲染提供无副作用的插值取样。
/// </summary>
public struct InterpolatedPosition2D
{
    /// <summary>使用同一初始位置建立前后状态，保证新实体首帧不会从原点飞入。</summary>
    public InterpolatedPosition2D(Vector2 position)
    {
        Previous = position;
        Current = position;
    }

    /// <summary>获取上一固定物理步完成时的位置。</summary>
    public Vector2 Previous { get; private set; }

    /// <summary>获取或设置碰撞、索敌和下一固定物理步使用的权威位置。</summary>
    public Vector2 Current { get; set; }

    /// <summary>在系统修改当前位置前保存快照，使一个物理步内只产生一段稳定轨迹。</summary>
    public void BeginPhysicsStep() => Previous = Current;

    /// <summary>将前后位置同时平移，用于无限世界重定位且不制造跨屏插值残影。</summary>
    public void Translate(Vector2 offset)
    {
        Previous += offset;
        Current += offset;
    }

    /// <summary>按引擎给出的物理插值比例返回绘制位置，并钳制异常比例防止外推。</summary>
    public readonly Vector2 Sample(float fraction) =>
        Previous.Lerp(Current, Mathf.Clamp(fraction, 0.0f, 1.0f));
}
