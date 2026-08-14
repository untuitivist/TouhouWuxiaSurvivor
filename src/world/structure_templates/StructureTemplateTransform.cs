namespace TouhouWuxiaSurvivor.World.StructureTemplates;

/// <summary>
/// 将模板局部坐标应用四向旋转和镜像变体，保证地表与上层图像使用相同变换。
/// </summary>
public static class StructureTemplateTransform
{
    /// <summary>
    /// 先按变体奇偶镜像 X，再顺时针旋转若干个四分之一周，输出规范模板坐标。
    /// </summary>
    public static (int X, int Y) ToCanonical(int x, int y, int quarterTurns, int variant)
    {
        int transformedX = (variant & 1) == 0 ? x : -x;
        return (((quarterTurns % 4) + 4) % 4) switch
        {
            1 => (y, -transformedX),
            2 => (-transformedX, -y),
            3 => (-y, transformedX),
            _ => (transformedX, y),
        };
    }
}
