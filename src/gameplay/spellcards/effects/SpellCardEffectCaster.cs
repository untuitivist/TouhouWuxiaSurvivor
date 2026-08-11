using Godot;
using TouhouWuxiaSurvivor.Actors.Enemies;
using TouhouWuxiaSurvivor.Actors.Player;
using TouhouWuxiaSurvivor.Ecs.Combat;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;

namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Effects;

/// <summary>
/// 把符卡定义转换为具体战斗实体与伤害，同时保持运行协调器不依赖效果细节。
/// </summary>
public sealed class SpellCardEffectCaster
{
    private readonly Node2D _player;
    private readonly PlayerHealth _health;
    private readonly Node2D _enemies;
    private readonly EcsCombatWorld? _ecsWorld;
    private readonly Node2D _effects;
    private readonly PackedScene _orbScene;
    private readonly PackedScene _circleScene;

    /// <summary>
    /// 注入施法者、生命、敌人和效果容器，以及两类可实例化视觉场景。
    /// </summary>
    public SpellCardEffectCaster(
        Node2D player,
        PlayerHealth health,
        Node2D enemies,
        Node2D effects,
        PackedScene orbScene,
        PackedScene circleScene,
        EcsCombatWorld? ecsWorld = null)
    {
        _player = player;
        _health = health;
        _enemies = enemies;
        _effects = effects;
        _orbScene = orbScene;
        _circleScene = circleScene;
        _ecsWorld = ecsWorld;
    }

    /// <summary>
    /// 按符卡效果类型施放对应奥义；无有效追踪目标时梦想封印不会消耗资源。
    /// </summary>
    public bool TryCast(SpellCardDefinition card) => card.EffectKind switch
    {
        SpellCardEffectKind.HomingVolley => CastTargetedVolley(card),
        SpellCardEffectKind.FocusedVolley => CastTargetedVolley(card),
        SpellCardEffectKind.AreaBurst => CastArea(card, false),
        SpellCardEffectKind.GuardField => CastArea(card, true),
        _ => false,
    };

    /// <summary>
    /// 判断当前战况是否值得自动消耗符卡：追踪奥义要求三名目标，护身阵要求近身受围。
    /// </summary>
    public bool ShouldAutoCast(SpellCardDefinition card)
    {
        int nearby = SelectInRangePositions(card.Combat.EffectRange).Count;
        return card.TriggerKind switch
        {
            SpellCardTriggerKind.Crowd => nearby >= 3,
            SpellCardTriggerKind.Danger => nearby >= 3 ||
                (nearby > 0 && _health.CurrentHealth * 2 <= _health.MaxHealth),
            SpellCardTriggerKind.SingleTarget => nearby > 0,
            _ => false,
        };
    }

    /// <summary>
    /// 为射程内最近的不同目标各生成一枚追踪灵玉，并以环形起点避免视觉重叠。
    /// </summary>
    private bool CastTargetedVolley(SpellCardDefinition card)
    {
        IReadOnlyList<Vector2> targets = SelectNearestPositions(
            card.Combat.EffectRange, card.Combat.TargetCount);
        if (targets.Count == 0)
        {
            return false;
        }

        for (int index = 0; index < targets.Count; index++)
        {
            float angle = Mathf.Tau * index / targets.Count;
            if (_ecsWorld is not null)
            {
                var orb = _orbScene.Instantiate<FantasySealOrb>();
                orb.ConfigureEcs(_ecsWorld, targets[index], card.Combat.Damage, 440.0f,
                    index, card.SourcePackId, card.FullName);
                _effects.AddChild(orb);
                orb.GlobalPosition = _player.GlobalPosition + Vector2.FromAngle(angle) * 24.0f;
                _ecsWorld.DamageEnemies(targets[index], 16.0f, card.Combat.Damage);
                continue;
            }

            EnemyActor? target = SpellCardTargetSelector.SelectNearest(
                _enemies, targets[index], 1.0f, 1).FirstOrDefault();
            if (target is null)
            {
                continue;
            }

            var legacyOrb = _orbScene.Instantiate<FantasySealOrb>();
            legacyOrb.Configure(target, card.Combat.Damage, 440.0f, index,
                card.SourcePackId, card.FullName);
            _effects.AddChild(legacyOrb);
            legacyOrb.GlobalPosition = _player.GlobalPosition + Vector2.FromAngle(angle) * 24.0f;
        }

        return true;
    }

    /// <summary>
    /// 对结界范围内全部存活敌人结算伤害、延长玩家无敌并生成一次结界演出。
    /// </summary>
    private bool CastArea(SpellCardDefinition card, bool grantDefense)
    {
        if (_ecsWorld is not null)
        {
            _ecsWorld.DamageEnemies(
                _player.GlobalPosition, card.Combat.EffectRange, card.Combat.Damage);
        }
        else
        {
            foreach (EnemyActor enemy in SpellCardTargetSelector.SelectInRange(
                _enemies,
                _player.GlobalPosition,
                card.Combat.EffectRange))
            {
                enemy.ReceiveDamage(card.Combat.Damage);
            }
        }

        if (grantDefense && card.Combat.DefenseSeconds > 0.0f)
        {
            _health.GrantInvincibility(card.Combat.DefenseSeconds);
        }

        var effect = _circleScene.Instantiate<SealingCircleEffect>();
        effect.Configure(card.SourcePackId, card.FullName);
        _effects.AddChild(effect);
        effect.GlobalPosition = _player.GlobalPosition;
        return true;
    }

    /// <summary>从 ECS 或兼容节点后端取得最近敌人的位置。</summary>
    private IReadOnlyList<Vector2> SelectNearestPositions(float range, int count) =>
        _ecsWorld is not null
            ? _ecsWorld.SelectEnemies(_player.GlobalPosition, range, count)
            : SpellCardTargetSelector.SelectNearest(
                _enemies, _player.GlobalPosition, range, count)
                .Select(enemy => enemy.GlobalPosition).ToArray();

    /// <summary>从 ECS 或兼容节点后端取得范围内敌人的位置。</summary>
    private IReadOnlyList<Vector2> SelectInRangePositions(float range) =>
        _ecsWorld is not null
            ? _ecsWorld.SelectEnemies(_player.GlobalPosition, range)
            : SpellCardTargetSelector.SelectInRange(
                _enemies, _player.GlobalPosition, range)
                .Select(enemy => enemy.GlobalPosition).ToArray();
}
