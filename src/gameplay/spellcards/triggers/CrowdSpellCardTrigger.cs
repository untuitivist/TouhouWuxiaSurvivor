using TouhouWuxiaSurvivor.Gameplay.SpellCards.Contracts;

namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Triggers;

/// <summary>
/// 在恢复完成后按受控间隔评估周围敌群，达到实效阈值才锁存自动施展信号。
/// </summary>
public sealed class CrowdSpellCardTrigger : ISpellCardTrigger
{
    private float _evaluationRemaining;

    public bool IsTriggered { get; private set; }

    /// <summary>
    /// 周期未就绪时停止查询；就绪后立即评估一次，未满足时按照环境给出的间隔继续检查。
    /// </summary>
    public void Advance(SpellCardTriggerContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!context.IsCooldownReady)
        {
            IsTriggered = false;
            _evaluationRemaining = 0.0f;
            return;
        }

        if (IsTriggered)
        {
            return;
        }

        _evaluationRemaining -= context.ElapsedSeconds;
        if (_evaluationRemaining > 0.0f)
        {
            return;
        }

        float interval = context.Environment.CrowdEvaluationIntervalSeconds;
        _evaluationRemaining = RequirePositive(interval);
        int nearbyEnemies = Math.Max(
            0, context.Environment.CountEnemiesInRange(context.Combat.EffectRange));
        int threshold = RequirePositive(context.Combat.ActivationThreshold);
        IsTriggered = nearbyEnemies >= threshold;
    }

    /// <summary>消费已满足的敌群信号，并允许下一次独立恢复结束后重新评估环境。</summary>
    public void Consume() => IsTriggered = false;

    /// <summary>拒绝无效评估周期，防止损坏配置把范围查询退化为逐帧高开销操作。</summary>
    private static float RequirePositive(float value) =>
        float.IsFinite(value) && value > 0.0f
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value));

    /// <summary>拒绝非正触发阈值，避免损坏数值让敌群奥义在空场景自动施展。</summary>
    private static int RequirePositive(int value) =>
        value > 0 ? value : throw new ArgumentOutOfRangeException(nameof(value));
}
