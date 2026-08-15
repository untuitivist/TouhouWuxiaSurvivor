using Godot;
using TouhouWuxiaSurvivor.Gameplay.Difficulty;

namespace TouhouWuxiaSurvivor.Combat.Weapons;

/// <summary>
/// 把弹幕计划转换成逐弹出生位置与方向；所有花型都必须保留可命中的目标交点。
/// </summary>
public static class PlayerVolleyPattern
{
    /// <summary>
    /// 普通扇形从中心向外展开；收束阵从玩家周身旋转出生并全部指向同一预判交点。
    /// </summary>
    public static ProjectileLaunchPlan Resolve(
        Vector2 origin,
        Vector2 aimedDirection,
        Vector2 interceptPoint,
        float spawnDistance,
        PlayerBarrageSnapshot barrage,
        int projectileIndex)
    {
        Vector2 aim = aimedDirection.IsZeroApprox()
            ? Vector2.Right
            : aimedDirection.Normalized();
        if (barrage.Mode == PlayerBarrageMode.ConvergingOrbit)
        {
            double step = Math.Tau / Math.Max(1, barrage.ProjectileCount);
            float angle = (float)(barrage.RotationRadians + step * projectileIndex);
            Vector2 offset = Vector2.Right.Rotated(angle) * Math.Max(0.0f, spawnDistance);
            Vector2 position = origin + offset;
            Vector2 direction = interceptPoint - position;
            return new ProjectileLaunchPlan(position,
                direction.IsZeroApprox() ? aim : direction.Normalized());
        }

        double center = (barrage.ProjectileCount - 1) * 0.5;
        float spreadAngle = (float)(barrage.RotationRadians +
            (projectileIndex - center) * barrage.AngularStepRadians);
        Vector2 spreadDirection = aim.Rotated(spreadAngle).Normalized();
        return new ProjectileLaunchPlan(
            origin + spreadDirection * Math.Max(0.0f, spawnDistance),
            spreadDirection);
    }
}
