using TouhouWuxiaSurvivor.Gameplay.SpellCards.Contracts;

namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Triggers;

/// <summary>
/// 为尚未接入世界环境的旧组合入口提供无事件环境；定时奥义可运行，条件奥义保持等待。
/// </summary>
public sealed class EmptySpellCardTriggerEnvironment : ISpellCardTriggerEnvironment
{
    public static EmptySpellCardTriggerEnvironment Instance { get; } = new();
    public long DamageRevision => 0L;
    public float CrowdEvaluationIntervalSeconds => float.MaxValue;

    /// <summary>空环境中不存在敌人，因此条件型奥义不会被误触发。</summary>
    public int CountEnemiesInRange(float effectRange) => 0;

    /// <summary>限制空环境为共享实例，避免兼容入口为每局产生无意义分配。</summary>
    private EmptySpellCardTriggerEnvironment()
    {
    }
}
