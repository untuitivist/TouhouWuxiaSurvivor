using TouhouWuxiaSurvivor.Actors.Enemies;
using TouhouWuxiaSurvivor.Gameplay.Difficulty;

namespace TouhouWuxiaSurvivor.Gameplay.Progression.Runtime;

/// <summary>
/// 将敌人的未缩放耐久转换为确定性灵息价值，并统一应用一次无尽时间奖励倍率。
/// </summary>
public static class SpiritValueCalculator
{
    /// <summary>
    /// 普通敌人对生命取平方根并限制到一至八点；Boss 使用十二至六十四点的独立基数，
    /// 避免角色 Boss 与普通杂兵共享八点封顶，同时保留固定输入对应固定奖励的可复现性。
    /// </summary>
    public static int Calculate(EnemyDefinition enemy)
    {
        ArgumentNullException.ThrowIfNull(enemy);
        return CalculateDurabilityValue(enemy.BaseMaxHealth, enemy.IsBoss);
    }

    /// <summary>
    /// 将基于未缩放耐久得到的灵息乘以共享无尽奖励倍率；无论输入是目录敌人还是出生时
    /// 已提高生命的运行时敌人，生命倍率都不会再被平方根奖励公式隐式计算第二次。
    /// </summary>
    public static int CalculateForElapsedTime(EnemyDefinition enemy, double elapsedSeconds)
    {
        ArgumentNullException.ThrowIfNull(enemy);
        EndlessDifficultySnapshot difficulty = EndlessDifficultyCurve.EvaluateSeconds(
            elapsedSeconds, int.MaxValue);
        return ApplyRewardMultiplier(Calculate(enemy), difficulty.RewardMultiplier);
    }

    /// <summary>
    /// 将已还原的基础耐久映射到普通怪或 Boss 的独立奖励区间，集中维护两类敌人的奖励边界。
    /// </summary>
    private static int CalculateDurabilityValue(double health, bool isBoss)
    {
        int durabilityValue = (int)Math.Ceiling(Math.Sqrt(Math.Max(1.0, health)));
        return isBoss
            ? Math.Clamp(durabilityValue, 12, 64)
            : Math.Clamp(durabilityValue, 1, 8);
    }

    /// <summary>
    /// 在唯一位置完成奖励倍率和整数边界处理，供正式掉落与策划模拟共同调用同一入口。
    /// </summary>
    private static int ApplyRewardMultiplier(int baseValue, double multiplier) =>
        (int)Math.Clamp(
            Math.Floor(baseValue * Math.Max(1.0, multiplier)),
            1.0,
            int.MaxValue / 2.0);
}
