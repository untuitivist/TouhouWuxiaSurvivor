using Godot;
using TouhouWuxiaSurvivor.Ui.Stats.Build;

namespace TouhouWuxiaSurvivor.Ui.Stats;

/// <summary>
/// 保存一次构筑图布局中的节点、屏幕矩形和所属泳道，供绘制与命中测试共享。
/// </summary>
public sealed class CharacterBuildGraphItem
{
    public CharacterBuildNodeView Node { get; }
    public Rect2 Rect { get; }
    public int Lane { get; }

    /// <summary>
    /// 建立不可变布局项；矩形使用图谱局部坐标，视图变换由图谱控件统一处理。
    /// </summary>
    public CharacterBuildGraphItem(CharacterBuildNodeView node, Rect2 rect, int lane)
    {
        Node = node;
        Rect = rect;
        Lane = Math.Max(0, lane);
    }
}
