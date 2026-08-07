namespace TouhouWuxiaSurvivor.Gameplay.Progression.Runtime;

/// <summary>
/// 集中定义当前等级升至下一等级所需的灵息，便于数值测试和后续整体校准。
/// </summary>
public static class RunLevelCurve
{
    /// <summary>
    /// 采用线性增长并每五级增加一次台阶，使开局快速成形、后期升级逐渐放缓。
    /// </summary>
    public static int GetRequiredExperience(int currentLevel)
    {
        int level = Math.Max(1, currentLevel);
        return 6 + level * 2 + (level - 1) / 5 * 5;
    }
}
