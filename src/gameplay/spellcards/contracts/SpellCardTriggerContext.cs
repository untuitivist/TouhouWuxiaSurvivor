using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;

namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Contracts;

/// <summary>
/// 汇集一次自动触发判定的只读输入，阻止策略跨边界读取协调器、玩家或 ECS 状态。
/// </summary>
public sealed class SpellCardTriggerContext
{
    public SpellCardDefinition Card { get; }
    public ResolvedSpellCardCombat Combat { get; }
    public ISpellCardTriggerEnvironment Environment { get; }
    public float ElapsedSeconds { get; }
    public bool IsCooldownReady { get; }

    /// <summary>建立单帧判定上下文，并把负时间收敛为零以保护策略内部计时。</summary>
    public SpellCardTriggerContext(
        SpellCardDefinition card,
        ResolvedSpellCardCombat combat,
        ISpellCardTriggerEnvironment environment,
        float elapsedSeconds,
        bool isCooldownReady)
    {
        Card = card ?? throw new ArgumentNullException(nameof(card));
        Combat = combat ?? throw new ArgumentNullException(nameof(combat));
        Environment = environment ?? throw new ArgumentNullException(nameof(environment));
        ElapsedSeconds = Math.Max(0.0f, elapsedSeconds);
        IsCooldownReady = isCooldownReady;
    }
}
