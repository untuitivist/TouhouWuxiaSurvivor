using Godot;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;

namespace TouhouWuxiaSurvivor.Ecs.Combat.Projectiles;

/// <summary>
/// 从弹型语义与当前速度推导绘制姿态，避免在 ECS 中保存会和运动方向漂移的重复角度状态。
/// </summary>
public static class ProjectileVisualPosePolicy
{
    private const float AtlasForwardAngle = Mathf.Pi * 0.5f;

    /// <summary>判断该轮廓是否具有明确前后方向，需要随飞行向量旋转。</summary>
    public static bool IsDirectional(SpellBulletStyleKind style) => style is
        SpellBulletStyleKind.Amulet or
        SpellBulletStyleKind.Needle or
        SpellBulletStyleKind.Knife or
        SpellBulletStyleKind.Flame or
        SpellBulletStyleKind.Butterfly or
        SpellBulletStyleKind.Laser or
        SpellBulletStyleKind.Shard;

    /// <summary>把图集中朝下的原始弹型旋转到速度方向；对称弹与零速度保持零角度。</summary>
    public static float ResolveRotation(SpellBulletStyleKind style, Vector2 velocity)
    {
        if (!IsDirectional(style) || velocity.IsZeroApprox()) return 0.0f;
        return Mathf.Wrap(velocity.Angle() - AtlasForwardAngle, -Mathf.Pi, Mathf.Pi);
    }
}
