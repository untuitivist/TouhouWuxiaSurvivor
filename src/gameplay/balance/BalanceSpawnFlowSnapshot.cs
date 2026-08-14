namespace TouhouWuxiaSurvivor.Gameplay.Balance;

/// <summary>
/// 保存一次敌群供给推进后的真实约束结果，区分理论刷新率、实际接纳率、击破率和剩余存活数。
/// </summary>
public readonly record struct BalanceSpawnFlowSnapshot(
    double ScheduledSpawnsPerSecond,
    double AcceptedSpawnsPerSecond,
    double DefeatsPerSecond,
    double AliveCount,
    int AliveLimit);
