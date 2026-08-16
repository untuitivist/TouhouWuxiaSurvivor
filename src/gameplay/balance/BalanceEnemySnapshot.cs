namespace TouhouWuxiaSurvivor.Gameplay.Balance;

/// <summary>
/// 保存当前内容与时间下的加权敌群样本，避免时间轴模拟器重复理解敌人目录字段。
/// </summary>
public readonly record struct BalanceEnemySnapshot(
    int AvailableDefinitionCount,
    double AverageHealth,
    double AverageSpiritValue,
    double AverageThreat,
    double SpawnSupplyPerSecond,
    double ProjectedAliveCount,
    double Pressure);
