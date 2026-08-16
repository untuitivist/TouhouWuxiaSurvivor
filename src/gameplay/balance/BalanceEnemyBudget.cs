using TouhouWuxiaSurvivor.Actors.Enemies;
using TouhouWuxiaSurvivor.Content;
using TouhouWuxiaSurvivor.Gameplay.Difficulty;
using TouhouWuxiaSurvivor.Gameplay.Progression.Runtime;

namespace TouhouWuxiaSurvivor.Gameplay.Balance;

/// <summary>
/// 从正式敌人目录与无尽曲线生成生态平均值，只用于策划预算而不伪装成逐实体物理模拟。
/// </summary>
internal static class BalanceEnemyBudget
{
    /// <summary>
    /// 按时间和内容筛选已解锁敌人，用刷怪权重计算耐久、灵息与威胁的可复现加权均值。
    /// </summary>
    public static BalanceEnemySnapshot Evaluate(
        double elapsedSeconds,
        ContentPackSelection content,
        EndlessDifficultySnapshot difficulty,
        double aliveCount,
        double acceptedSpawnsPerSecond)
    {
        EnemyDefinition[] available = EnemyCatalog.All.Where(enemy =>
            elapsedSeconds >= enemy.UnlockTime &&
            (enemy.RequiredContentPack is null || content.IsEnabled(enemy.RequiredContentPack)))
            .ToArray();
        if (available.Length == 0)
        {
            throw new InvalidOperationException("Enemy catalog has no available opening enemy.");
        }

        double totalWeight = available.Sum(enemy => Math.Max(0.01f, enemy.SpawnWeight));
        double baseHealth = WeightedAverage(available, totalWeight, enemy => enemy.MaxHealth);
        double spirit = WeightedAverage(available, totalWeight,
            enemy => SpiritValueCalculator.Calculate(enemy));
        double threat = WeightedAverage(available, totalWeight, CalculateThreat);
        double spawnSupply = Math.Max(0.0, acceptedSpawnsPerSecond);
        double pressure = difficulty.Intensity * Math.Sqrt(1.0 + Math.Max(0.0, aliveCount)) *
            (1.0 + threat * 0.35);
        return new BalanceEnemySnapshot(available.Length, baseHealth,
            spirit, threat, spawnSupply, Math.Max(0.0, aliveCount), pressure);
    }

    /// <summary>
    /// 使用目录刷怪权重计算一个投影维度的均值，避免作品敌人数量本身改变生态预算。
    /// </summary>
    private static double WeightedAverage(
        IEnumerable<EnemyDefinition> enemies,
        double totalWeight,
        Func<EnemyDefinition, double> selector) => enemies.Sum(enemy =>
            selector(enemy) * Math.Max(0.01f, enemy.SpawnWeight)) / totalWeight;

    /// <summary>
    /// 以耐久、接触伤害、移动和远程能力构成单体威胁；掉落率与作品编号不计入战斗强度。
    /// </summary>
    private static double CalculateThreat(EnemyDefinition enemy)
    {
        double durability = Math.Sqrt(Math.Max(1, enemy.MaxHealth) / 3.0);
        double contact = enemy.ContactDamage * 0.55;
        double movement = enemy.MoveSpeed / 70.0;
        double projectile = enemy.ProjectileProfile.Damage > 0 ? 0.45 : 0.0;
        return durability + contact + movement + projectile;
    }
}
