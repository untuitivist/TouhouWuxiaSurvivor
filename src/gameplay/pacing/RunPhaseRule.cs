namespace TouhouWuxiaSurvivor.Gameplay.Pacing;

/// <summary>
/// 定义一个三十秒战力验证档位的刷新率和敌人强度配比；不携带计时兜底或存活数量条件。
/// </summary>
public sealed class RunPhaseRule
{
    public RunPhaseId PhaseId { get; }
    public double SpawnRatePerSecond { get; }
    public EnemyTierMix TierMix { get; }

    /// <summary>
    /// 建立完整规则；刷新率必须为有限正数，配比由值对象自行保证归一化。
    /// </summary>
    public RunPhaseRule(
        RunPhaseId phaseId,
        double spawnRatePerSecond,
        EnemyTierMix tierMix)
    {
        if (!double.IsFinite(spawnRatePerSecond) || spawnRatePerSecond <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(spawnRatePerSecond),
                "Enemy spawn rate must be finite and positive.");
        }

        PhaseId = phaseId;
        SpawnRatePerSecond = spawnRatePerSecond;
        TierMix = tierMix;
    }
}
