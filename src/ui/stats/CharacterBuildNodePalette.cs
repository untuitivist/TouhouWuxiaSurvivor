using Godot;
using TouhouWuxiaSurvivor.Ui.Stats.Build;

namespace TouhouWuxiaSurvivor.Ui.Stats;

/// <summary>
/// 集中提供构筑节点与关系线的武侠风颜色，避免绘图控件散落状态判断。
/// </summary>
public static class CharacterBuildNodePalette
{
    /// <summary>
    /// 按当前节点状态返回墨底、朱砂、铜色或灰封配色。
    /// </summary>
    public static Color Fill(CharacterBuildNodeView node) => node.State switch
    {
        CharacterBuildNodeState.Mastered => new Color("7d2d25"),
        CharacterBuildNodeState.Learned => new Color("48231d"),
        CharacterBuildNodeState.Available => new Color("20382b"),
        CharacterBuildNodeState.LockedExclusion => new Color("271d1d"),
        CharacterBuildNodeState.LockedContent => new Color("171c19"),
        _ => new Color("1b211d"),
    };

    /// <summary>
    /// 已取得与可进阶节点使用高对比边框，其余节点保持可读但明确退后。
    /// </summary>
    public static Color Border(CharacterBuildNodeView node) => node.State switch
    {
        CharacterBuildNodeState.Mastered => new Color("e2b85c"),
        CharacterBuildNodeState.Learned => new Color("d95543"),
        CharacterBuildNodeState.Available => new Color("7da277"),
        CharacterBuildNodeState.LockedExclusion => new Color("70433d"),
        _ => new Color("465049"),
    };

    /// <summary>
    /// 选中节点保持亮字，锁定节点降低对比；颜色并非唯一状态提示，节点仍绘制文字标记。
    /// </summary>
    public static Color Text(CharacterBuildNodeView node) =>
        node.IsLearned || node.IsAvailable
            ? new Color("f3ead2")
            : new Color("8e978c");
}
