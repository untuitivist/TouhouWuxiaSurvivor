using Godot;
using TouhouWuxiaSurvivor.Gameplay.Difficulty;

namespace TouhouWuxiaSurvivor.Combat.Weapons;

/// <summary>
/// 把双通道计划转换成逐弹出生位置与方向；所有弹幕都围绕有效目标展开。
/// </summary>
public static class PlayerVolleyPattern
{
    /// <summary>
    /// 前部自瞄弹组指向预判点；后部弹幕组成对称扇面或从两翼向同一交点收束。
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
        if (projectileIndex < barrage.AimedProjectileCount)
        {
            float center = (barrage.AimedProjectileCount - 1) * 0.5f;
            Vector2 perpendicular = new(-aim.Y, aim.X);
            Vector2 position = origin + aim * Math.Max(0.0f, spawnDistance) +
                perpendicular * (projectileIndex - center) * 3.0f;
            Vector2 direction = interceptPoint - position;
            return new ProjectileLaunchPlan(
                position,
                direction.IsZeroApprox() ? aim : direction.Normalized(),
                PlayerProjectileChannel.PredictiveAim);
        }

        int barrageIndex = projectileIndex - barrage.AimedProjectileCount;
        if (barrage.Mode == PlayerBarrageMode.ConvergingFormation)
        {
            return ResolveConverging(origin, aim, interceptPoint,
                spawnDistance, barrage.BarrageProjectileCount, barrageIndex);
        }

        int pair = barrageIndex / 2 + 1;
        float sign = barrageIndex % 2 == 0 ? -1.0f : 1.0f;
        float spreadAngle = sign * pair * (float)barrage.AngularStepRadians;
        Vector2 spreadDirection = aim.Rotated(spreadAngle).Normalized();
        return new ProjectileLaunchPlan(
            origin + spreadDirection * Math.Max(0.0f, spawnDistance),
            spreadDirection,
            PlayerProjectileChannel.Barrage);
    }

    /// <summary>
    /// 沿瞄准方向前方排布对称两翼，所有方向重新指向预判点，避免环绕弹向外空转。
    /// </summary>
    private static ProjectileLaunchPlan ResolveConverging(
        Vector2 origin,
        Vector2 aim,
        Vector2 interceptPoint,
        float spawnDistance,
        int barrageCount,
        int barrageIndex)
    {
        Vector2 perpendicular = new(-aim.Y, aim.X);
        float center = (Math.Max(1, barrageCount) - 1) * 0.5f;
        float spacing = Math.Clamp(spawnDistance * 0.38f, 3.0f, 7.0f);
        Vector2 position = origin + aim * Math.Max(0.0f, spawnDistance) +
            perpendicular * (barrageIndex - center) * spacing;
        Vector2 direction = interceptPoint - position;
        return new ProjectileLaunchPlan(position,
            direction.IsZeroApprox() ? aim : direction.Normalized(),
            PlayerProjectileChannel.Barrage);
    }
}
