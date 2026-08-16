namespace TouhouWuxiaSurvivor.Gameplay.Pacing;

/// <summary>
/// 保存给定难度进度的连续刷新率、强度配比和离散档位，供刷怪器与数值模拟共享。
/// </summary>
public readonly record struct EnemyPressureSnapshot(
    int GearIndex,
    double SpawnRatePerSecond,
    EnemyTierMix TierMix)
{
    /// <summary>
    /// 以刷新率乘敌群档位权重得到只读观察指标；该值不会参与换档或改写任何敌人属性。
    /// </summary>
    public double DifficultyIndex => SpawnRatePerSecond *
        (TierMix.Common + TierMix.Veteran * 1.6 +
            TierMix.Elite * 2.5 + TierMix.Champion * 3.6);
}
