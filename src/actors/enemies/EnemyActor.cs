using Godot;
using TouhouWuxiaSurvivor.Actors.Player;

namespace TouhouWuxiaSurvivor.Actors.Enemies;

/// <summary>
/// 表示一个持续追逐玩家、能够承受投射物伤害并驱动独立视觉组件的敌人实体。
/// </summary>
public partial class EnemyActor : CharacterBody2D
{
    public const string GroupName = "combat_enemies";
    private EnemyDefinition? _definition;
    private Node2D? _target;
    private EnemyVisualController? _visual;
    private CollisionShape2D? _collisionShape;
    private Area2D? _touchDamageArea;
    private CollisionShape2D? _touchDamageShape;
    private PlayerHealth? _touchedHealth;
    private int _currentHealth;
    private double _hurtFlashLeft;
    private double _touchCooldownLeft;
    private double _deathTimeLeft;

    public event Action<Vector2, EnemyDefinition>? Defeated;
    public event Action? Damaged;
    public event Action? Exploded;
    public bool IsAlive { get; private set; } = true;
    public EnemyDefinition Definition => _definition
        ?? throw new InvalidOperationException("Enemy must be configured before use.");

    /// <summary>
    /// 在节点进入场景树之前注入敌人定义和追踪目标，使就绪阶段可一次性建立视觉与碰撞。
    /// </summary>
    public void Configure(EnemyDefinition definition, Node2D target)
    {
        _definition = definition;
        _target = target;
        _currentHealth = definition.MaxHealth;
    }

    /// <summary>
    /// 获取场景节点、配置共享敌人视觉、设置碰撞半径并注册到统一索敌组。
    /// </summary>
    public override void _Ready()
    {
        if (_definition is null)
        {
            _definition = EnemyCatalog.All[0];
            _currentHealth = _definition.MaxHealth;
        }

        _visual = GetNode<EnemyVisualController>("Visual");
        _collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
        _touchDamageArea = GetNode<Area2D>("TouchDamageArea");
        _touchDamageShape = GetNode<CollisionShape2D>("TouchDamageArea/CollisionShape2D");
        _visual.Configure(_definition);
        _touchDamageArea.BodyEntered += OnTouchBodyEntered;
        _touchDamageArea.BodyExited += OnTouchBodyExited;
        if (_collisionShape.Shape is CircleShape2D circle)
        {
            circle.Radius = _definition.CollisionRadius;
        }

        if (_touchDamageShape.Shape is CircleShape2D touchCircle)
        {
            touchCircle.Radius = _definition.CollisionRadius + 2.0f;
        }

        AddToGroup(GroupName);
    }

    /// <summary>
    /// 按当前追踪目标计算归一化速度并持续逼近，同时更新短暂的受击显色反馈。
    /// </summary>
    public override void _PhysicsProcess(double delta)
    {
        UpdateHurtFlash(delta);
        if (!IsAlive)
        {
            UpdateDeath(delta);
            return;
        }

        UpdateTouchDamage(delta);
        if (!GodotObject.IsInstanceValid(_target))
        {
            Velocity = Vector2.Zero;
            return;
        }

        Vector2 direction = GlobalPosition.DirectionTo(_target!.GlobalPosition);
        _visual?.SetFacing(direction.X);
        Velocity = direction * Definition.MoveSpeed;
        MoveAndSlide();
    }

    /// <summary>
    /// 接收一次正数伤害；生命归零时只触发一次击破事件并切入不可碰撞的文字死亡状态。
    /// </summary>
    public bool ReceiveDamage(int amount)
    {
        if (!IsAlive || amount <= 0)
        {
            return false;
        }

        _currentHealth -= amount;
        if (_currentHealth <= 0)
        {
            BeginDeath();
        }
        else
        {
            _hurtFlashLeft = 0.12;
            Damaged?.Invoke();
        }

        return true;
    }

    /// <summary>
    /// 禁止后续移动和命中，广播掉落位置，并开始短暂的中文消散或爆散反馈。
    /// </summary>
    private void BeginDeath()
    {
        IsAlive = false;
        Velocity = Vector2.Zero;
        _hurtFlashLeft = 0.0;
        _deathTimeLeft = Definition.ExplodesOnDeath ? 0.28 : 0.18;
        _visual?.ShowDefeated(Definition.ExplodesOnDeath);

        _collisionShape?.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
        _touchDamageShape?.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
        _touchedHealth = null;
        Defeated?.Invoke(GlobalPosition, Definition);
        if (Definition.ExplodesOnDeath)
        {
            Exploded?.Invoke();
        }
    }

    /// <summary>
    /// 玩家进入接触范围时缓存其生命组件，并立即尝试造成一点接触伤害。
    /// </summary>
    private void OnTouchBodyEntered(Node2D body)
    {
        if (!IsAlive)
        {
            return;
        }

        _touchedHealth = body.GetNodeOrNull<PlayerHealth>("Health");
        TryDealTouchDamage();
    }

    /// <summary>
    /// 当前玩家离开接触范围后清除缓存，停止后续冷却伤害尝试。
    /// </summary>
    private void OnTouchBodyExited(Node2D body)
    {
        if (_touchedHealth is not null && body == _touchedHealth.GetParent())
        {
            _touchedHealth = null;
        }
    }

    /// <summary>
    /// 递减半秒接触冷却，并在玩家仍然重叠时继续尝试伤害。
    /// </summary>
    private void UpdateTouchDamage(double delta)
    {
        _touchCooldownLeft = Math.Max(0.0, _touchCooldownLeft - delta);
        if (_touchCooldownLeft <= 0.0 && _touchedHealth is not null)
        {
            TryDealTouchDamage();
        }
    }

    /// <summary>
    /// 向玩家生命组件派发一点伤害，并无论是否被无敌帧拦截都重置接触冷却。
    /// </summary>
    private void TryDealTouchDamage()
    {
        if (_touchedHealth is null || !GodotObject.IsInstanceValid(_touchedHealth))
        {
            _touchedHealth = null;
            return;
        }

        _touchedHealth.ApplyDamage(1);
        _touchCooldownLeft = 0.5;
    }

    /// <summary>
    /// 递减受击显色时间，并通知视觉组件在纹理与文字两种模式下恢复正确颜色。
    /// </summary>
    private void UpdateHurtFlash(double delta)
    {
        if (_visual is null || _hurtFlashLeft <= 0.0)
        {
            return;
        }

        _hurtFlashLeft = Math.Max(0.0, _hurtFlashLeft - delta);
        _visual.SetHurt(_hurtFlashLeft > 0.0);
    }

    /// <summary>
    /// 递减文字死亡反馈时间，并在反馈结束后回收敌人节点。
    /// </summary>
    private void UpdateDeath(double delta)
    {
        _deathTimeLeft = Math.Max(0.0, _deathTimeLeft - delta);
        if (_deathTimeLeft <= 0.0)
        {
            QueueFree();
        }
    }
}
