using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;

namespace TouhouWuxiaSurvivor.Ui.Stats.Build;

/// <summary>
/// 将符卡空间策略投影为玩家可理解的弹幕形态，界面不依赖策略类名称或英文清单值。
/// </summary>
public static class SpellCardGeometryText
{
    /// <summary>返回适合构筑节点和图鉴属性的简短形态名称。</summary>
    public static string GetName(SpellCardGeometryKind kind) => kind switch
    {
        SpellCardGeometryKind.Orbit => "环身巡游",
        SpellCardGeometryKind.Fan => "扇面齐射",
        SpellCardGeometryKind.Line => "贯线突进",
        SpellCardGeometryKind.Ring => "圆环扩散",
        SpellCardGeometryKind.Backstab => "背后夹击",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };
}
