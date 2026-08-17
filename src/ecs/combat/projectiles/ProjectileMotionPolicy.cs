using Godot;

namespace TouhouWuxiaSurvivor.Ecs.Combat.Projectiles;

/// <summary>
/// 以纯函数式状态推进冻结、转向与弯曲弹道，供 ECS 敌弹和低频玩家奥义节点共同调用。
/// </summary>
public static class ProjectileMotionPolicy
{
    /// <summary>
    /// 推进一帧运动阶段并修改速度；返回 false 表示当前处在停止时间内，本帧不应产生位移。
    /// </summary>
    public static bool Step(
        ref Vector2 velocity,
        ref float age,
        ref bool transitionApplied,
        Vector2 position,
        ProjectileMotionProfile profile,
        float delta)
    {
        float safeDelta = Math.Max(0.0f, delta);
        float previousAge = age;
        age += safeDelta;
        float holdStart = profile.TransitionSeconds;
        float holdEnd = holdStart + profile.HoldSeconds;

        if (profile.Kind == ProjectileMotionKind.CurvedStream && age <= holdStart)
        {
            velocity = velocity.Rotated(profile.AngularVelocity * safeDelta);
        }

        if (!transitionApplied && previousAge < holdEnd && age >= holdEnd)
        {
            ApplyTransition(ref velocity, position, profile);
            transitionApplied = true;
        }

        return profile.HoldSeconds <= 0.0f || age <= holdStart || age >= holdEnd;
    }

    /// <summary>在停止阶段结束的唯一边界改变弹道，避免同一弹丸每帧重复旋转或重定向。</summary>
    private static void ApplyTransition(
        ref Vector2 velocity,
        Vector2 position,
        ProjectileMotionProfile profile)
    {
        float speed = velocity.Length();
        if (profile.Kind == ProjectileMotionKind.RedirectOnce)
        {
            Vector2 direction = position.DirectionTo(profile.RedirectTarget);
            if (!direction.IsZeroApprox()) velocity = direction * speed;
        }
        else if (profile.Kind == ProjectileMotionKind.FreezeResume &&
                 !Mathf.IsZeroApprox(profile.TurnAngle))
        {
            velocity = velocity.Rotated(profile.TurnAngle);
        }
    }
}
