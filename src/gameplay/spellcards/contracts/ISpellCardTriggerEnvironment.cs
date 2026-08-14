namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Contracts;

/// <summary>
/// 暴露自动奥义触发所需的最小世界信息，使触发策略不依赖玩家、生命或 ECS 的具体实现。
/// </summary>
public interface ISpellCardTriggerEnvironment
{
    long DamageRevision { get; }
    float CrowdEvaluationIntervalSeconds { get; }

    /// <summary>统计指定世界距离内仍有效的敌人数量，调用方负责按短评估间隔限流。</summary>
    int CountEnemiesInRange(float effectRange);
}
