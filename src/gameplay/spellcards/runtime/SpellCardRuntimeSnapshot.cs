using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;

namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Runtime;

/// <summary>
/// 封装当前已悟奥义及其独立定时状态，供 HUD 与属性页稳定投影。
/// </summary>
public sealed class SpellCardRuntimeSnapshot
{
    public IReadOnlyList<SpellCardDefinition> UnlockedCards { get; }
    public IReadOnlyList<SpellCardTimerSnapshot> Timers { get; }
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
        NextCardName = Timers.FirstOrDefault()?.Card.ShortName ?? "尚未悟得";
        NextCardIsWaitingForCondition = Timers.FirstOrDefault()?.IsWaitingForCondition ?? false;
        NextCastRemaining = Timers.Count == 0
            ? 0.0f
            : Math.Max(0.0f, Timers[0].RemainingSeconds);
    }
}
