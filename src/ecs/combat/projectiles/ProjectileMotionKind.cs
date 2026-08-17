namespace TouhouWuxiaSurvivor.Ecs.Combat.Projectiles;

/// <summary>
/// 定义投射物可复用的跨常运动语法；普通弹、符卡弹与未来内容包共享这些高频 ECS 行为。
/// </summary>
public enum ProjectileMotionKind
{
    Linear,
    FreezeResume,
    RedirectOnce,
    CurvedStream,
}
