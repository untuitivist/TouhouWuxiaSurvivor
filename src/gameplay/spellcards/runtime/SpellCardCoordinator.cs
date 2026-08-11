using Godot;
using TouhouWuxiaSurvivor.Actors.Player;
using TouhouWuxiaSurvivor.Gameplay.Spawning;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Effects;
using TouhouWuxiaSurvivor.Gameplay.Progression.Definitions;
using TouhouWuxiaSurvivor.Gameplay.Progression.Runtime;
using TouhouWuxiaSurvivor.Ecs.Combat;
using TouhouWuxiaSurvivor.Content;

namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Runtime;

/// <summary>
/// 协调当前符卡、共享灵力、冷却、输入和效果施放，不承担敌人或界面生命周期。
/// </summary>
public partial class SpellCardCoordinator : Node
{
    private SpellCardEffectCaster? _caster;
    private SpiritDropSpawner? _spiritSpawner;
    private RunBuildState? _build;
    private float _cooldownRemaining;
    private float _decisionCooldown;
    private bool _runEndBlocked;
    private ContentPackSelection _content = ContentPackSelection.BaseOnly;

    [Export]
    public PackedScene? FantasySealOrbScene { get; set; }

    [Export]
    public PackedScene? SealingCircleScene { get; set; }

    public SpellPowerState Power { get; } = new();
    public float CooldownRemaining => _cooldownRemaining;
    public bool IsRunEndBlocked => _runEndBlocked;

    /// <summary>
    /// 注入战斗节点与灵息来源，建立效果执行器并订阅实际拾取事件进行灵力回充。
    /// </summary>
    public void Configure(
        Node2D player,
        PlayerHealth health,
        Node2D enemies,
        Node2D effects,
        SpiritDropSpawner spiritSpawner,
        RunBuildState build,
        EcsCombatWorld? ecsWorld = null,
        ContentPackSelection? content = null)
    {
        if (FantasySealOrbScene is null || SealingCircleScene is null)
        {
            throw new InvalidOperationException("Spell card effect scenes must be assigned.");
        }

        _caster = new SpellCardEffectCaster(
            player,
            health,
            enemies,
            effects,
            FantasySealOrbScene,
            SealingCircleScene,
            ecsWorld);
        _spiritSpawner = spiritSpawner;
        _build = build;
        _content = content ?? ContentPackSelectionService.Current;
        _spiritSpawner.SpiritCollected += GainFromSpirit;
    }

    /// <summary>
    /// 按正常游戏时间递减公共冷却，并低频评估一次已悟得符卡的自动施放条件。
    /// </summary>
    public override void _Process(double delta)
    {
        _cooldownRemaining = Math.Max(0.0f, _cooldownRemaining - (float)delta);
        _decisionCooldown -= (float)delta;
        if (_decisionCooldown <= 0.0f)
        {
            TryAutoCast();
            _decisionCooldown = 0.2f;
        }
    }

    /// <summary>
    /// 把收取的灵息价值转换为共享灵力，供事件订阅和集成测试使用同一规则。
    /// </summary>
    public void GainFromSpirit(int spiritValue) => Power.GainFromSpirit(spiritValue);

    /// <summary>
    /// 按护身优先、追封其次的顺序评估已悟得符卡，成功后原子扣费并启动公共冷却。
    /// </summary>
    public bool TryAutoCast()
    {
        if (_runEndBlocked || _caster is null || _build is null || _cooldownRemaining > 0.0f)
        {
            return false;
        }

        foreach (SpellCardDefinition card in GetUnlockedCards().OrderBy(
            definition => definition.TriggerKind == SpellCardTriggerKind.Danger ? 0 : 1))
        {
            if (Power.CurrentPower < card.Combat.PowerCost ||
                !_caster.ShouldAutoCast(card) || !_caster.TryCast(card) ||
                !Power.TrySpend(card.Combat.PowerCost))
            {
                continue;
            }

            _cooldownRemaining = card.Combat.CooldownSeconds;
            return true;
        }

        return false;
    }

    /// <summary>
    /// 捕获当前符卡、资源与冷却，供 HUD 和属性面板读取而不暴露可变内部字段。
    /// </summary>
    public SpellCardRuntimeSnapshot CreateSnapshot() => new(
        GetUnlockedCards(),
        Power.CurrentPower,
        SpellPowerState.MaximumPower,
        _cooldownRemaining);

    /// <summary>
    /// 根据本局构筑返回已经悟得的灵梦符卡，目录顺序保持稳定以便界面与测试比较。
    /// </summary>
    private IReadOnlyList<SpellCardDefinition> GetUnlockedCards()
    {
        if (_build is null)
        {
            return [];
        }

        return SpellCardCatalog.GetEnabled(_content)
            .Where(card => _build.GetRank(card.UnlockUpgradeId) > 0)
            .ToArray();
    }

    /// <summary>
    /// 本局结束后永久阻止继续切换或施放，避免结算期间生成新战斗实体。
    /// </summary>
    public void BlockForRunEnd() => _runEndBlocked = true;

    /// <summary>
    /// 节点退出场景时解除灵息事件订阅，避免重载场景后旧协调器继续接收回调。
    /// </summary>
    public override void _ExitTree()
    {
        if (_spiritSpawner is not null)
        {
            _spiritSpawner.SpiritCollected -= GainFromSpirit;
        }
    }
}
