using TouhouWuxiaSurvivor.Gameplay.SpellCards.Contracts;

namespace TouhouWuxiaSurvivor.Tests.Support;

/// <summary>
/// 提供可控受击序号与敌群数量的触发环境替身，并记录范围查询次数以验证节流契约。
/// </summary>
public sealed class TestSpellCardTriggerEnvironment : ISpellCardTriggerEnvironment
{
    public long DamageRevision { get; private set; }
    public float CrowdEvaluationIntervalSeconds { get; set; } = 0.2f;
    public int NearbyEnemyCount { get; set; }
    public int CrowdQueryCount { get; private set; }
    public float LastQueryRange { get; private set; }

    /// <summary>模拟一次实际受击，以单调序号保证多个观察者都能无损发现同一事件。</summary>
    public void ReportDamage() => DamageRevision++;

    /// <summary>返回测试指定敌人数并记录调用，使测试能发现逐帧或错误范围查询。</summary>
    public int CountEnemiesInRange(float effectRange)
    {
        CrowdQueryCount++;
        LastQueryRange = effectRange;
        return NearbyEnemyCount;
    }
}
