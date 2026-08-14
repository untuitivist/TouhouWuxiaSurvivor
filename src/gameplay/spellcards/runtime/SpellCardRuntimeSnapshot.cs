using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;

namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Runtime;

/// <summary>
/// 封装当前已悟奥义及其独立定时状态，供 HUD 与属性页稳定投影。
/// </summary>
public sealed class SpellCardRuntimeSnapshot
{
    public IReadOnlyList<SpellCardDefinition> UnlockedCards { get; }
    public IReadOnlyList<SpellCardTimerSnapshot> Timers { get; }
    public IReadOnlyList<SpellCardTimerSnapshot> PresentationTimers { get; }
    public string NextCardName { get; }
    public float NextCastRemaining { get; }
    public bool NextCardIsWaitingForCondition { get; }
    public bool HasUnlockedCard => UnlockedCards.Count > 0;
    public bool HasTriggeredCard => Timers.Any(timer => timer.IsTriggered);

    /// <summary>
    /// 建立单帧符卡快照，复制已悟得目录以免界面观察到运行集合的后续变化。
    /// </summary>
    public SpellCardRuntimeSnapshot(
        IReadOnlyList<SpellCardDefinition> unlockedCards,
        IReadOnlyList<SpellCardTimerSnapshot> timers)
    {
        UnlockedCards = unlockedCards.ToArray();
        Timers = timers.OrderBy(timer => timer.RemainingSeconds).ToArray();
        PresentationTimers = BuildPresentationTimers(UnlockedCards, timers);
        NextCardName = Timers.FirstOrDefault()?.Card.ShortName ?? "尚未悟得";
        NextCardIsWaitingForCondition = Timers.FirstOrDefault()?.IsWaitingForCondition ?? false;
        NextCastRemaining = Timers.Count == 0
            ? 0.0f
            : Math.Max(0.0f, Timers[0].RemainingSeconds);
    }

    /// <summary>
    /// 按主攻、护持与目录顺序建立稳定展示序列，避免 HUD 图标随着倒计时先后不断换位。
    /// </summary>
    private static IReadOnlyList<SpellCardTimerSnapshot> BuildPresentationTimers(
        IReadOnlyList<SpellCardDefinition> unlockedCards,
        IReadOnlyList<SpellCardTimerSnapshot> timers)
    {
        var catalogOrder = unlockedCards.Select((card, index) => (card.Id, index))
            .ToDictionary(item => item.Id, item => item.index, StringComparer.Ordinal);
        return timers.OrderBy(timer => SpellCardSlotPolicy.Classify(timer.Card))
            .ThenBy(timer => catalogOrder.GetValueOrDefault(timer.Card.Id, int.MaxValue))
            .ThenBy(timer => timer.Card.Id, StringComparer.Ordinal)
            .ToArray();
    }
}
