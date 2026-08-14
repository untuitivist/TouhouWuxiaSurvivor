using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;

namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Runtime;

/// <summary>
/// 表示一张已悟奥义的只读周期与剩余时间，界面无需接触协调器的可变计时字典。
/// </summary>
public sealed class SpellCardTimerSnapshot
{
    public SpellCardDefinition Card { get; }
    public float IntervalSeconds { get; }
    public float RemainingSeconds { get; }
    public bool IsTriggered { get; }
    public bool IsReady => RemainingSeconds <= 0.0f;
    public bool IsWaitingForCondition => IsReady && !IsTriggered;

    /// <summary>建立单帧计时投影；周期保留当前属性换算值，剩余时间允许为负以表达超期。</summary>
    public SpellCardTimerSnapshot(
        SpellCardDefinition card,
        float intervalSeconds,
        float remainingSeconds,
        bool isTriggered)
    {
        Card = card ?? throw new ArgumentNullException(nameof(card));
        IntervalSeconds = Math.Max(0.0f, intervalSeconds);
        RemainingSeconds = remainingSeconds;
        IsTriggered = isTriggered;
    }
}
