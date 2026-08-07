using Godot;
using TouhouWuxiaSurvivor.Actors.Enemies;

namespace TouhouWuxiaSurvivor.Combat.Projectiles;

/// <summary>
/// 表示玩家武器发射的直线投射物，负责移动、寿命控制和单次敌人伤害结算。
/// </summary>
public partial class PlayerProjectile : Area2D
{
    private Vector2 _direction = Vector2.Right;
    private float _speed = 360.0f;
    private int _damage = 1;
    private double _lifetimeLeft = 2.0;
    private bool _consumed;

    /// <summary>
    /// 注入归一化飞行方向、速度和伤害；零方向会安全回退为向右飞行。
    /// </summary>
    public void Configure(Vector2 direction, float speed, int damage)
    {
        _direction = direction.IsZeroApprox() ? Vector2.Right : direction.Normalized();
        _speed = Math.Max(0.0f, speed);
        _damage = Math.Max(1, damage);
        Rotation = _direction.Angle();
    }

    /// <summary>
    /// 订阅 Area2D 对物理体的进入通知，使敌人碰撞统一由投射物一侧处理。
    /// </summary>
    public override void _Ready() => BodyEntered += OnBodyEntered;

    /// <summary>
    /// 按物理步长推进投射物，并在最大寿命耗尽后自动释放离屏实例。
    /// </summary>
    public override void _PhysicsProcess(double delta)
    {
        GlobalPosition += _direction * _speed * (float)delta;
        _lifetimeLeft -= delta;
        if (_lifetimeLeft <= 0.0)
        {
            QueueFree();
        }
    }

    /// <summary>
    /// 命中存活敌人时派发一次伤害并立即消费自己，避免同一子弹穿透多个目标。
    /// </summary>
    private void OnBodyEntered(Node2D body)
    {
        if (_consumed || body is not EnemyActor enemy || !enemy.ReceiveDamage(_damage))
        {
            return;
        }

        _consumed = true;
        QueueFree();
    }
}
