using TouhouWuxiaSurvivor.Actors.Enemies;

namespace TouhouWuxiaSurvivor.Gameplay.Progression.Runtime;

/// <summary>
/// 将敌人种类定义中的固定耐久转换为确定性灵息价值；阶段变化不会改变同种怪物奖励。
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
    /// 将已还原的基础耐久映射到普通怪或 Boss 的独立奖励区间，集中维护两类敌人的奖励边界。
    /// </summary>
    private static int CalculateDurabilityValue(double health, bool isBoss)
    {
        int durabilityValue = (int)Math.Ceiling(Math.Sqrt(Math.Max(1.0, health)));
        return isBoss
            ? Math.Clamp(durabilityValue, 12, 64)
            : Math.Clamp(durabilityValue, 1, 8);
    }

}
