namespace TouhouWuxiaSurvivor.Gameplay.Progression.Runtime;

/// <summary>
/// 集中定义当前等级升至下一等级所需的灵息，便于数值测试和后续整体校准。
/// </summary>
public static class RunLevelCurve
{
    /// <summary>
    /// 保留前两次八点与十三点需求，从第三级起用更陡二次项吸收多弹清场带来的灵息增长。
    /// </summary>
    public static int GetRequiredExperience(int currentLevel)
    {
        int level = Math.Max(1, currentLevel);
        long offset = (long)level - 1L;
        long linearGrowth = offset * 3L;
        long rootGrowth = (long)Math.Floor(2.0 * Math.Sqrt(offset));
        long curveOffset = Math.Max(0L, offset - 1L);
        decimal quadraticGrowth = (decimal)curveOffset * curveOffset * 1.5m;
        decimal required = 8m + linearGrowth + decimal.Floor(quadraticGrowth) + rootGrowth;
        if (required >= int.MaxValue)
        {
            return int.MaxValue;
        }

        return (int)required;
    }
}
