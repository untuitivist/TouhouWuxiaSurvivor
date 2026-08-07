using Godot;
using TouhouWuxiaSurvivor.Actors.Enemies;
using TouhouWuxiaSurvivor.Actors.Player;
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
        PackedScene circleScene)
    {
        _player = player;
        _health = health;
        _enemies = enemies;
        _effects = effects;
        _orbScene = orbScene;
        _circleScene = circleScene;
    }

    /// <summary>
    /// 按符卡效果类型施放对应奥义；无有效追踪目标时梦想封印不会消耗资源。
    /// </summary>
    public bool TryCast(SpellCardDefinition card) => card.EffectKind switch
    {
        SpellCardEffectKind.FantasySeal => CastFantasySeal(card),
        SpellCardEffectKind.EvilSealingCircle => CastEvilSealingCircle(card),
        _ => false,
    };

    /// <summary>
    /// 判断当前战况是否值得自动消耗符卡：追踪奥义要求三名目标，护身阵要求近身受围。
    /// </summary>
    public bool ShouldAutoCast(SpellCardDefinition card)
    {
        int nearby = card.EffectKind switch
        {
            SpellCardEffectKind.FantasySeal => SpellCardTargetSelector.SelectNearest(
                _enemies,
                _player.GlobalPosition,
                card.Combat.EffectRange,
                card.Combat.TargetCount).Count,
            SpellCardEffectKind.EvilSealingCircle => SpellCardTargetSelector.SelectInRange(
                _enemies,
                _player.GlobalPosition,
                card.Combat.EffectRange).Count,
            _ => 0,
        };
        return nearby >= 3 ||
            (card.EffectKind == SpellCardEffectKind.EvilSealingCircle &&
                nearby > 0 && _health.CurrentHealth * 2 <= _health.MaxHealth);
    }

    /// <summary>
    /// 为射程内最近的不同目标各生成一枚追踪灵玉，并以环形起点避免视觉重叠。
    /// </summary>
    private bool CastFantasySeal(SpellCardDefinition card)
    {
        IReadOnlyList<EnemyActor> targets =
            SpellCardTargetSelector.SelectNearest(
                _enemies,
                _player.GlobalPosition,
                card.Combat.EffectRange,
                card.Combat.TargetCount);
        if (targets.Count == 0)
        {
            return false;
        }

        for (int index = 0; index < targets.Count; index++)
        {
            float angle = Mathf.Tau * index / targets.Count;
            var orb = _orbScene.Instantiate<FantasySealOrb>();
            orb.Configure(targets[index], card.Combat.Damage, 440.0f, index);
            _effects.AddChild(orb);
            orb.GlobalPosition = _player.GlobalPosition + Vector2.FromAngle(angle) * 24.0f;
        }

        return true;
    }

    /// <summary>
    /// 对结界范围内全部存活敌人结算伤害、延长玩家无敌并生成一次结界演出。
    /// </summary>
    private bool CastEvilSealingCircle(SpellCardDefinition card)
    {
        foreach (EnemyActor enemy in SpellCardTargetSelector.SelectInRange(
            _enemies,
            _player.GlobalPosition,
            card.Combat.EffectRange))
        {
            enemy.ReceiveDamage(card.Combat.Damage);
        }

        _health.GrantInvincibility(card.Combat.DefenseSeconds);
        var effect = _circleScene.Instantiate<SealingCircleEffect>();
        _effects.AddChild(effect);
        effect.GlobalPosition = _player.GlobalPosition;
        return true;
    }
}
