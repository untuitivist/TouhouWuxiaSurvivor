using TouhouWuxiaSurvivor.Gameplay.SpellCards.Contracts;

namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Triggers;

/// <summary>
/// 以单调受击序号观察事件，仅锁存本卡恢复完成后发生的新受击，避免冷却期事件被延迟消费。
/// </summary>
public sealed class OnDamagedSpellCardTrigger : ISpellCardTrigger
{
    private long _observedDamageRevision;
    private bool _wasCooldownReady;
    private bool _initialized;

    public bool IsTriggered { get; private set; }

    /// <summary>
    /// 持续追平冷却期受击序号；刚进入就绪态时先建立基线，之后的新序号才成为待施展信号。
    /// </summary>
    public void Advance(SpellCardTriggerContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        long currentRevision = context.Environment.DamageRevision;
        if (!_initialized || !context.IsCooldownReady)
        {
            _observedDamageRevision = currentRevision;
            _initialized = true;
            _wasCooldownReady = context.IsCooldownReady;
            IsTriggered = false;
            return;
        }

        if (IsTriggered)
        {
            _observedDamageRevision = Math.Max(_observedDamageRevision, currentRevision);
            return;
        }

        if (!_wasCooldownReady)
        {
            _observedDamageRevision = currentRevision;
            _wasCooldownReady = true;
            IsTriggered = false;
            return;
        }

        if (!IsTriggered && currentRevision > _observedDamageRevision)
        {
            IsTriggered = true;
        }

        _observedDamageRevision = Math.Max(_observedDamageRevision, currentRevision);
    }

    /// <summary>消费当前受击信号；下一次施展必须等待本卡恢复并观察到更新的受击序号。</summary>
    public void Consume() => IsTriggered = false;
}
