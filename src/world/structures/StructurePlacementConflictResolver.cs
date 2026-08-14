namespace TouhouWuxiaSurvivor.World.Structures;

/// <summary>
/// 以候选实例的稳定全序执行局部冲突裁决，使结果不受查询窗口、区块和加载次序影响。
/// </summary>
public static class StructurePlacementConflictResolver
{
    public const int QueryMargin = 64;

    /// <summary>
    /// 删除邻近范围内优先级较低的候选；保留项之间必定满足 footprint 与异类硬间距。
    /// </summary>
    public static IReadOnlyList<StructurePlacement> Resolve(
        IEnumerable<StructurePlacement> candidates)
    {
        StructurePlacement[] source = candidates
            .GroupBy(item => item.InstanceId)
            .Select(group => group.First()).ToArray();
        return source.Where(current => !source.Any(other =>
                other.InstanceId != current.InstanceId &&
                HasPriority(other, current) && Conflicts(other, current)))
            .ToArray();
    }

    /// <summary>
    /// 取双方配置中更严格的异类间距，并保证模板占地之间至少留出三格地表缓冲。
    /// </summary>
    private static bool Conflicts(StructurePlacement left, StructurePlacement right)
    {
        StructureDefinition leftDefinition = StructureCatalog.GetRequired(left.Id);
        StructureDefinition rightDefinition = StructureCatalog.GetRequired(right.Id);
        int required = Math.Max(leftDefinition.Placement.ForeignSeparation,
            rightDefinition.Placement.ForeignSeparation);
        required = Math.Max(required, left.FootprintRadius + right.FootprintRadius + 3);
        double dx = (double)left.X - right.X;
        double dy = (double)left.Y - right.Y;
        return dx * dx + dy * dy < (double)required * required;
    }

    /// <summary>
    /// 用实例哈希、结构枚举和坐标形成全序，极端哈希碰撞也不会出现双方互相保留。
    /// </summary>
    private static bool HasPriority(StructurePlacement left, StructurePlacement right) =>
        left.InstanceId < right.InstanceId ||
        left.InstanceId == right.InstanceId && (left.Id < right.Id ||
            left.Id == right.Id && (left.Y < right.Y || left.Y == right.Y && left.X < right.X));
}
