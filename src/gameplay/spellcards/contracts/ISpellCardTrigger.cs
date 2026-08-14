namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Contracts;

/// <summary>
/// 定义单张奥义的有状态自动触发判定；策略只观察上下文，不施放效果也不重启周期。
/// </summary>
public interface ISpellCardTrigger
{
    bool IsTriggered { get; }

    /// <summary>推进一次触发观察，并在满足策略条件时锁存待施展信号。</summary>
    void Advance(SpellCardTriggerContext context);

    /// <summary>施展尝试已经消费当前信号后清除锁存，避免同一事件被重复使用。</summary>
    void Consume();
}
