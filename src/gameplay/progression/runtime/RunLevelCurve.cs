namespace TouhouWuxiaSurvivor.Gameplay.Progression.Runtime;

/// <summary>
/// 集中定义当前等级升至下一等级所需的灵息，便于数值测试和后续整体校准。
/// </summary>
public static class RunLevelCurve
{
    /// <summary>
    /// 使用二次主项配合平方根，使前两次选择仍迅速出现，之后逐步压低过密的升级与奥义成型速度。
    /// </summary>
    public static int GetRequiredExperience(int currentLevel)
    {
        int level = Math.Max(1, currentLevel);
        long offset = (long)level - 1L;
        long linearGrowth = offset * 3L;
        long quadraticGrowth = SaturatedSquare(offset) / 5L;
        long rootGrowth = (long)Math.Floor(2.0 * Math.Sqrt(offset));
        long required = 8L + linearGrowth + quadraticGrowth + rootGrowth;
        return (int)Math.Min(int.MaxValue, required);
    }

    /// <summary>
    /// 在长整型边界内计算平方项，极端等级直接饱和而不让乘法溢出为负数。
    /// </summary>
    private static long SaturatedSquare(long value) =>
        value > 3_037_000_499L ? long.MaxValue : value * value;
}
