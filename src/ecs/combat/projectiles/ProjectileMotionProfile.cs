using Godot;

namespace TouhouWuxiaSurvivor.Ecs.Combat.Projectiles;

/// <summary>
/// 保存一枚弹丸的归一化运动参数；不包含内容包或符卡身份，可由任何声明式内容安全复用。
/// </summary>
public readonly record struct ProjectileMotionProfile(
    ProjectileMotionKind Kind,
    float TransitionSeconds = 0.0f,
    float HoldSeconds = 0.0f,
    float AngularVelocity = 0.0f,
    float TurnAngle = 0.0f,
    Vector2 RedirectTarget = default)
{
    /// <summary>清理非有限或负时长参数，避免错误内容把 NaN 写入高频物理状态。</summary>
    public ProjectileMotionProfile Normalize() => new(
        Kind,
        NormalizeDuration(TransitionSeconds),
        NormalizeDuration(HoldSeconds),
        float.IsFinite(AngularVelocity) ? AngularVelocity : 0.0f,
        float.IsFinite(TurnAngle) ? TurnAngle : 0.0f,
        RedirectTarget.IsFinite() ? RedirectTarget : Vector2.Zero);

    /// <summary>把运动阶段时长限制在单颗弹丸的合理生命周期内。</summary>
    private static float NormalizeDuration(float value) =>
        float.IsFinite(value) ? Math.Clamp(value, 0.0f, 6.0f) : 0.0f;
}
