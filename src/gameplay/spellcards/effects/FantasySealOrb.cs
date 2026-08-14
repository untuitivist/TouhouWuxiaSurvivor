using Godot;

namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Effects;

/// <summary>
/// 表示梦想封印的一枚文字灵玉，持续追踪指定敌人并在抵达时走正常伤害接口。
/// </summary>
public partial class FantasySealOrb : Node2D
{
    private SpellCardCombatBackend? _backend;
    private Vector2 _targetPosition;
    private int _damage = 1;
    private float _speed = 420.0f;
    private float _impactRange = 12.0f;
    private double _lifetimeLeft = 2.0;
    private int _visualVariant;
    private float _curvature;
    private SpellCardTargetReference? _trackingTarget;
    private bool _lostTrackingTarget;
    private string _sourcePackId = "th06_eosd";
    private string _spellCardName = "灵符「梦想封印」";
    private InternalSpellBulletVisual? _visual;
    private Label? _fallbackLabel;

    /// <summary>
    /// 注入统一战斗后端和固定命中点，使五种几何可以在 ECS 与兼容节点模式中复用相同飞行节点。
    /// </summary>
    public void ConfigureImpact(
        SpellCardCombatBackend backend,
        Vector2 targetPosition,
        int damage,
        float speed,
        float impactRange,
        float lifetimeSeconds,
        int visualVariant,
        string sourcePackId,
        string spellCardName,
        float curvature,
        SpellCardTargetReference? trackingTarget = null)
    {
        _backend = backend ?? throw new ArgumentNullException(nameof(backend));
        _targetPosition = targetPosition;
        _damage = Math.Max(1, damage);
        _speed = Math.Max(1.0f, speed);
        _impactRange = Math.Max(1.0f, impactRange);
        _lifetimeLeft = Math.Max(0.1f, lifetimeSeconds);
        _visualVariant = visualVariant;
        _sourcePackId = sourcePackId;
        _spellCardName = spellCardName;
        _curvature = curvature;
        _trackingTarget = trackingTarget;
        _lostTrackingTarget = false;
    }

    /// <summary>
    /// 配置内部弹幕图集视觉；公开包中图集不可用时保留原有中文“灵”作为安全回退。
    /// </summary>
    public override void _Ready()
    {
        _visual = GetNode<InternalSpellBulletVisual>("Visual");
        _fallbackLabel = GetNode<Label>("FallbackLabel");
        _visual.Configure(_sourcePackId, _spellCardName, _visualVariant);
        _fallbackLabel.Visible = !_visual.Visible;
    }

    /// <summary>
    /// 每个物理帧向目标逼近；目标失效、命中或寿命耗尽时立即回收灵玉。
    /// </summary>
    public override void _PhysicsProcess(double delta)
    {
        _lifetimeLeft -= delta;
        if (_lifetimeLeft <= 0.0 || _backend is null)
        {
            QueueFree();
            return;
        }

        RefreshTrackingPosition();

        if (GlobalPosition.DistanceSquaredTo(_targetPosition) <= _impactRange * _impactRange)
        {
            ResolveImpact();
            QueueFree();
            return;
        }

        Vector2 direction = GlobalPosition.DirectionTo(_targetPosition);
        if (!direction.IsZeroApprox() && !Mathf.IsZeroApprox(_curvature))
        {
            direction = direction.Rotated(_curvature);
            _curvature = Mathf.MoveToward(_curvature, 0.0f, 3.4f * (float)delta);
        }

        GlobalPosition += direction * _speed * (float)delta;
        Rotation += (float)delta * 5.0f;
    }

    /// <summary>目标存活时刷新最新位置；失效后解除引用并保留最后一次有效落点继续飞行。</summary>
    private void RefreshTrackingPosition()
    {
        if (_trackingTarget is null)
        {
            return;
        }

        if (_backend!.TryGetTargetPosition(_trackingTarget, out Vector2 position))
        {
            _targetPosition = position;
            return;
        }

        _trackingTarget = null;
        _lostTrackingTarget = true;
    }

    /// <summary>活目标按身份命中；固定落点使用邻近伤害，已失效目标不转而误伤占位敌人。</summary>
    private void ResolveImpact()
    {
        if (_trackingTarget is not null)
        {
            _backend!.DamageTarget(_trackingTarget, _damage);
            return;
        }

        if (!_lostTrackingTarget)
        {
            _backend!.DamageAt(_targetPosition, _impactRange, _damage);
        }
    }
}
