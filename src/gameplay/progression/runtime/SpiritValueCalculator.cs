using TouhouWuxiaSurvivor.Actors.Enemies;

namespace TouhouWuxiaSurvivor.Gameplay.Progression.Runtime;

/// <summary>
/// 将敌人耐久转换为确定性灵息价值，使经验奖励不依赖临时道具掉落概率。
/// </summary>
public static class SpiritValueCalculator
{
    /// <summary>
    /// 对生命取平方根并向上取整，再限制到 1 至 8，温和奖励高耐久敌人。
    /// </summary>
    public static int Calculate(EnemyDefinition enemy) =>
        Math.Clamp((int)MathF.Ceiling(MathF.Sqrt(Math.Max(1, enemy.MaxHealth))), 1, 8);
}
