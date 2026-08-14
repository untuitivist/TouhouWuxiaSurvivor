using TouhouWuxiaSurvivor.Gameplay.SpellCards.Contracts;

namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Triggers;

/// <summary>
/// 在独立恢复周期结束时立即锁存施展信号，实现完全自动且不消耗任何战斗资源的定时奥义。
/// </summary>
public sealed class PeriodicSpellCardTrigger : ISpellCardTrigger
{
    public bool IsTriggered { get; private set; }

    /// <summary>同步周期就绪状态；周期未结束时主动清除旧信号以避免跨周期残留。</summary>
    public void Advance(SpellCardTriggerContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        IsTriggered = context.IsCooldownReady;
    }

    /// <summary>消费本周期的定时信号；若施展失败，短重试周期结束后会重新产生信号。</summary>
    public void Consume() => IsTriggered = false;
}
