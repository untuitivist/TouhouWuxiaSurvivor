namespace TouhouWuxiaSurvivor.Gameplay.Meta.Runtime;

/// <summary>
/// 把生存、击破和境界统一换算为有上限的幻想乡通用钱财，不依赖场景或存档。
/// </summary>
public static class RunRewardCalculator
{
    /// <summary>
    /// 计算单局收入，短于四十五秒且没有战绩的立即死亡不会产生可刷取钱财。
    /// </summary>
    public static int Calculate(double survivalSeconds, int defeatedEnemies, int finalLevel)
    {
        int survivalReward = (int)Math.Floor(Math.Max(0.0, survivalSeconds) / 45.0);
        int combatReward = Math.Max(0, defeatedEnemies) / 12;
        int levelReward = Math.Max(0, finalLevel - 1);
        return Math.Clamp(survivalReward + combatReward + levelReward, 0, 80);
    }
}
