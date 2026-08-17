using Godot;

namespace TouhouWuxiaSurvivor.Ecs.Combat.Projectiles;

/// <summary>
/// 封装一次敌弹生成请求，让普通怪和 Boss 可以共享发射入口而不继续扩张多参数委托。
/// </summary>
public readonly record struct EnemyProjectileSpawnRequest(
    Vector2 Position,
    Vector2 Direction,
    float Speed,
    int Damage,
    int VisualVariant,
    int VisualStyleId = 0,
    int VisualSourceId = 0,
    int VisualBulletStyleId = -1,
    ProjectileMotionProfile Motion = default);
