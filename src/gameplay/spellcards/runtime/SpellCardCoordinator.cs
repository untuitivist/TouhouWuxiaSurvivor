using Godot;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Contracts;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;

namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Runtime;

/// <summary>
/// 协调已悟奥义的独立定时轮转与效果施放，不承担敌人、输入或界面生命周期。
/// </summary>
public partial class SpellCardCoordinator : Node
{
    private ISpellCardEffectExecutor? _executor;
    private ISpellCardUnlockSource? _unlockSource;
    private ISpellCardTriggerFactory? _triggerFactory;
    private ISpellCardTriggerEnvironment? _triggerEnvironment;
    private readonly Dictionary<string, SpellCardTimerState> _timers = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ISpellCardTrigger> _triggers = new(StringComparer.Ordinal);
    private float _castLockRemaining;
    private bool _runEndBlocked;

    public float NextCastRemaining => CreateSnapshot().NextCastRemaining;
    public bool IsRunEndBlocked => _runEndBlocked;

    /// <summary>
    /// 注入执行、已悟来源、触发工厂和世界窄接口，使协调器只承担独立计时与公平编排。
    /// </summary>
    public void Configure(
        ISpellCardEffectExecutor executor,
        ISpellCardUnlockSource unlockSource,
        ISpellCardTriggerFactory triggerFactory,
        ISpellCardTriggerEnvironment triggerEnvironment)
    {
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        _unlockSource = unlockSource ?? throw new ArgumentNullException(nameof(unlockSource));
        _triggerFactory = triggerFactory ?? throw new ArgumentNullException(nameof(triggerFactory));
        _triggerEnvironment = triggerEnvironment ??
            throw new ArgumentNullException(nameof(triggerEnvironment));
        _timers.Clear();
        _triggers.Clear();
        _castLockRemaining = 0.0f;
        _runEndBlocked = false;
    }

    /// <summary>
    /// 按正常游戏时间推进每张奥义的独立周期，并在起手锁结束后自动施展一张到期奥义。
    /// </summary>
    public override void _Process(double delta)
    {
        float elapsed = Math.Max(0.0f, (float)delta);
        SynchronizeTimers();
        foreach (SpellCardTimerState timer in _timers.Values)
        {
            timer.Advance(elapsed);
        }

        AdvanceTriggers(elapsed);
        _castLockRemaining = Math.Max(0.0f, _castLockRemaining - elapsed);
        TryAutoCast();
    }

    /// <summary>
    /// 从已到期奥义中选择相对超期最久的一张施展，成功后只重置该卡自己的周期。
    /// </summary>
    public bool TryAutoCast()
    {
        if (_runEndBlocked || _executor is null || _unlockSource is null ||
            _triggerEnvironment is null ||
            _castLockRemaining > 0.0f)
        {
            return false;
        }

        SynchronizeTimers();
        AdvanceTriggers(0.0f);
        foreach (SpellCardDefinition card in GetUnlockedCards()
            .Where(card => _triggers[card.Id].IsTriggered)
            .OrderBy(card => _timers[card.Id].RemainingSeconds /
                Math.Max(0.001f, _executor.Resolve(card).IntervalSeconds))
            .ThenBy(card => card.Id, StringComparer.Ordinal))
        {
            ResolvedSpellCardCombat resolved = _executor.Resolve(card);
            if (!_executor.TryCast(card, resolved))
            {
                _triggers[card.Id].Consume();
                _timers[card.Id].Retry(resolved.CastLockSeconds);
                continue;
            }

            _triggers[card.Id].Consume();
            _castLockRemaining = resolved.CastLockSeconds;
            _timers[card.Id].Restart(resolved.IntervalSeconds);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 捕获已悟奥义及其实时换算周期，供 HUD 和属性面板读取而不暴露可变计时字典。
    /// </summary>
    public SpellCardRuntimeSnapshot CreateSnapshot()
    {
        SynchronizeTimers();
        IReadOnlyList<SpellCardDefinition> cards = GetUnlockedCards();
        return new SpellCardRuntimeSnapshot(cards, cards.Select(card =>
        {
            ResolvedSpellCardCombat resolved = _executor?.Resolve(card) ??
                throw new InvalidOperationException("Spell caster is not configured.");
            return new SpellCardTimerSnapshot(
                card,
                resolved.IntervalSeconds,
                _timers[card.Id].RemainingSeconds,
                _triggers[card.Id].IsTriggered);
        }).ToArray());
    }

    /// <summary>
    /// 根据本局构筑返回已经悟得的全部启用奥义，目录顺序保持稳定以便界面与测试比较。
    /// </summary>
    private IReadOnlyList<SpellCardDefinition> GetUnlockedCards()
    {
        return _unlockSource?.GetUnlockedCards() ?? [];
    }

    /// <summary>
    /// 为新悟奥义建立完整首周期，并移除内容切换后不可用条目，保证计时状态与构筑完全对应。
    /// </summary>
    private void SynchronizeTimers()
    {
        if (_executor is null || _triggerFactory is null)
        {
            return;
        }

        IReadOnlyList<SpellCardDefinition> cards = GetUnlockedCards();
        var activeIds = cards.Select(card => card.Id).ToHashSet(StringComparer.Ordinal);
        foreach (string staleId in _timers.Keys.Where(
            id => !activeIds.Contains(id)).ToArray())
        {
            _timers.Remove(staleId);
            _triggers.Remove(staleId);
        }

        foreach (SpellCardDefinition card in cards)
        {
            float interval = _executor.Resolve(card).IntervalSeconds;
            if (!_timers.TryGetValue(card.Id, out SpellCardTimerState? timer))
            {
                _timers.Add(card.Id, new SpellCardTimerState(interval));
                _triggers.Add(card.Id, _triggerFactory.Create(card));
            }
            else
            {
                timer.Rescale(interval);
            }
        }
    }

    /// <summary>
    /// 以单帧实效数值推进每张卡的独立触发策略；环境查询和事件语义均由策略自行约束。
    /// </summary>
    private void AdvanceTriggers(float elapsedSeconds)
    {
        if (_executor is null || _triggerEnvironment is null)
        {
            return;
        }

        foreach (SpellCardDefinition card in GetUnlockedCards())
        {
            ResolvedSpellCardCombat combat = _executor.Resolve(card);
            SpellCardTimerState timer = _timers[card.Id];
            _triggers[card.Id].Advance(new SpellCardTriggerContext(
                card, combat, _triggerEnvironment, elapsedSeconds, timer.IsReady));
        }
    }

    /// <summary>
    /// 本局结束后永久阻止继续切换或施放，避免结算期间生成新战斗实体。
    /// </summary>
    public void BlockForRunEnd() => _runEndBlocked = true;

    /// <summary>
    /// 节点退出场景时清除独立计时状态，避免场景重载后复用上一局的周期进度。
    /// </summary>
    public override void _ExitTree()
    {
        _timers.Clear();
        _triggers.Clear();
    }
}
