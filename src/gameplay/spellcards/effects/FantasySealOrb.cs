using Godot;
using TouhouWuxiaSurvivor.Actors.Enemies;
using TouhouWuxiaSurvivor.Ecs.Combat;

namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Effects;

/// <summary>
/// 表示梦想封印的一枚文字灵玉，持续追踪指定敌人并在抵达时走正常伤害接口。
/// </summary>
public partial class FantasySealOrb : Node2D
{
    private EnemyActor? _target;
    private EcsCombatWorld? _ecsWorld;
    private Vector2 _ecsTargetPosition;
    private int _damage = 1;
    private float _speed = 420.0f;
    private double _lifetimeLeft = 2.0;
    private int _visualVariant;
    private InternalSpellBulletVisual? _visual;
    private Label? _fallbackLabel;

    /// <summary>
    /// 注入唯一追踪目标、伤害和飞行速度；无效数值会被限制到安全下限。
    /// </summary>
    public void Configure(EnemyActor target, int damage, float speed, int visualVariant)
    {
        _target = target;
        _damage = Math.Max(1, damage);
        _speed = Math.Max(1.0f, speed);
        _visualVariant = visualVariant;
    }

    /// <summary>配置 ECS 目标位置；低数量符卡视觉仍可作为独立节点播放。</summary>
    public void ConfigureEcs(EcsCombatWorld world, Vector2 targetPosition, int damage, float speed, int visualVariant)
    {
        _ecsWorld = world;
        _ecsTargetPosition = targetPosition;
        _damage = Math.Max(1, damage);
        _speed = Math.Max(1.0f, speed);
        _visualVariant = visualVariant;
    }

    /// <summary>
    /// 配置内部弹幕图集视觉；公开包中图集不可用时保留原有中文“灵”作为安全回退。
    /// </summary>
    public override void _Ready()
    {
        _visual = GetNode<InternalSpellBulletVisual>("Visual");
        _fallbackLabel = GetNode<Label>("FallbackLabel");
        _visual.Configure("灵符「梦想封印」", _visualVariant);
        _fallbackLabel.Visible = !_visual.Visible;
    }

    /// <summary>
    /// 每个物理帧向目标逼近；目标失效、命中或寿命耗尽时立即回收灵玉。
    /// </summary>
    public override void _PhysicsProcess(double delta)
    {
        _lifetimeLeft -= delta;
        if (_lifetimeLeft <= 0.0 || (_ecsWorld is null &&
            (!GodotObject.IsInstanceValid(_target) || !_target!.IsAlive)))
        {
            QueueFree();
            return;
        }

        Vector2 targetPosition = _ecsWorld is not null ? _ecsTargetPosition : _target!.GlobalPosition;
        if (GlobalPosition.DistanceSquaredTo(targetPosition) <= 144.0f)
        {
            if (_ecsWorld is not null)
            {
                _ecsWorld.DamageEnemies(targetPosition, 12.0f, _damage);
            }
            else
            {
                _target!.ReceiveDamage(_damage);
            }
            QueueFree();
            return;
        }

        GlobalPosition = GlobalPosition.MoveToward(targetPosition, _speed * (float)delta);
        Rotation += (float)delta * 5.0f;
    }
}
