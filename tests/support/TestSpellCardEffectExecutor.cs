using TouhouWuxiaSurvivor.Gameplay.SpellCards.Contracts;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;

namespace TouhouWuxiaSurvivor.Tests.Support;

/// <summary>
/// 提供可调周期与可控失败的奥义执行器替身，用于只验证协调器的计时和状态契约。
/// </summary>
public sealed class TestSpellCardEffectExecutor : ISpellCardEffectExecutor
{
    private readonly Dictionary<string, float> _intervals = new(StringComparer.Ordinal);
    private readonly List<string> _attemptedCardIds = [];
    private readonly List<string> _successfulCardIds = [];

    public bool RejectCasts { get; set; }
    public float CastLockSeconds { get; set; } = 0.25f;
    public IReadOnlyList<string> AttemptedCardIds => _attemptedCardIds;
    public IReadOnlyList<string> SuccessfulCardIds => _successfulCardIds;

    /// <summary>
    /// 为指定奥义设置当前实效周期，使测试能在运行中模拟基础属性发生变化。
    /// </summary>
    public void SetInterval(string cardId, float seconds)
    {
        if (string.IsNullOrWhiteSpace(cardId))
        {
            throw new ArgumentException("Test spell card id cannot be empty.", nameof(cardId));
        }

        if (!float.IsFinite(seconds) || seconds <= 0.0f)
        {
            throw new ArgumentOutOfRangeException(nameof(seconds));
        }

        _intervals[cardId] = seconds;
    }

    /// <summary>
    /// 按测试设定返回完整解析结果，其余战斗维度使用固定合法值以隔离计时行为。
    /// </summary>
    public ResolvedSpellCardCombat Resolve(SpellCardDefinition card)
    {
        ArgumentNullException.ThrowIfNull(card);
        if (!_intervals.TryGetValue(card.Id, out float interval))
        {
            throw new InvalidOperationException($"Missing test interval for {card.Id}.");
        }

        return new ResolvedSpellCardCombat(
            interval, 100.0f, 10, 2, 2, 1.0f, 300.0f, 24.0f, 0.5f, 12.0f,
            CastLockSeconds);
    }

    /// <summary>
    /// 记录每次施展尝试，并按开关决定成功与否，便于断言短重试和成功轮转顺序。
    /// </summary>
    public bool TryCast(SpellCardDefinition card, ResolvedSpellCardCombat resolved)
    {
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(resolved);
        _attemptedCardIds.Add(card.Id);
        if (RejectCasts)
        {
            return false;
        }

        _successfulCardIds.Add(card.Id);
        return true;
    }

    /// <summary>清空施展历史但保留周期配置，供同一替身验证下一段独立行为。</summary>
    public void ClearHistory()
    {
        _attemptedCardIds.Clear();
        _successfulCardIds.Clear();
    }
}
