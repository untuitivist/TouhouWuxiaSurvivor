namespace TouhouWuxiaSurvivor.Gameplay.Progression.Runtime;

/// <summary>
/// 集中定义当前等级升至下一等级所需的灵息，便于数值测试和后续整体校准。
/// </summary>
public static class RunLevelCurve
{
    /// <summary>
    /// 使用长整型线性项配合平方根和对数细化，使开局仍需八、十点，并保证百万级乃至 int 全域不溢出。
    /// </summary>
    public static int GetRequiredExperience(int currentLevel)
    {
        int level = Math.Max(1, currentLevel);
        long offset = (long)level - 1L;
        long linearGrowth = offset * 19L / 20L;
        double rootGrowth = Math.Floor(2.0 * Math.Sqrt(offset));
        double longRunGrowth = Math.Floor(0.5 * Math.Pow(Math.Log2(level), 2.0));
        long required = 8L + linearGrowth + (long)rootGrowth + (long)longRunGrowth;
        return (int)required;
    }
}
