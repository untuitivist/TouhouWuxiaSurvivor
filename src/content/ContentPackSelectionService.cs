namespace TouhouWuxiaSurvivor.Content;

/// <summary>
/// 保存主菜单为下一局选择的内容快照，并让世界组合根在切换场景后读取同一结果。
/// </summary>
public static class ContentPackSelectionService
{
    public static ContentPackSelection Current { get; private set; } = ContentPackSelection.BaseOnly;

    /// <summary>
    /// 用新的不可变选择替换下一局配置，不修改正在运行世界已持有的旧快照。
    /// </summary>
    public static void Apply(ContentPackSelection selection) => Current = selection;
}
