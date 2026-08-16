using Godot;
using TouhouWuxiaSurvivor.Gameplay.Difficulty;

namespace TouhouWuxiaSurvivor.Combat.Weapons;

/// <summary>
/// 把双通道计划转换成逐弹出生位置与方向；普通弹锁敌，弹幕从自机向四周展开。
/// </summary>
public static class PlayerVolleyPattern
{
    /// <summary>
    /// 前部普通弹围绕预判点排布，后部弹幕按辐射或多重螺旋从自机中心发出。
    /// </summary>
    public static ProjectileLaunchPlan Resolve(
        Vector2 origin,
        Vector2 targetDirection,
        Vector2 interceptPoint,
        float spawnDistance,
        PlayerBarrageSnapshot barrage,
        int projectileIndex)
    {
        Vector2 aim = targetDirection.IsZeroApprox()
            ? Vector2.Right
            : targetDirection.Normalized();
        if (projectileIndex < barrage.OrdinaryProjectileCount)
        {
            return ResolveOrdinary(origin, aim, interceptPoint,
                spawnDistance, barrage, projectileIndex);
        }

        int barrageIndex = projectileIndex - barrage.OrdinaryProjectileCount;
        return ResolveCenteredBarrage(origin, spawnDistance, barrage, barrageIndex);
    }

    /// <summary>
    /// 普通弹默认形成预测扇面；取得收束特化后改为两翼出生并共同瞄准预测交点。
    /// </summary>
    private static ProjectileLaunchPlan ResolveOrdinary(
        Vector2 origin,
        Vector2 aim,
        Vector2 interceptPoint,
        float spawnDistance,
        PlayerBarrageSnapshot plan,
        int index)
    {
        float center = (plan.OrdinaryProjectileCount - 1) * 0.5f;
        if (plan.OrdinaryMode == PlayerOrdinaryShotMode.ConvergingFormation)
        {
            Vector2 perpendicular = new(-aim.Y, aim.X);
            Vector2 position = origin + aim * Math.Max(0.0f, spawnDistance) +
                perpendicular * (index - center) * 3.0f;
            Vector2 direction = interceptPoint - position;
            return new ProjectileLaunchPlan(position,
                direction.IsZeroApprox() ? aim : direction.Normalized(),
                PlayerProjectileChannel.Ordinary);
        }

        float angle = (index - center) * (float)plan.OrdinaryAngularStepRadians;
        Vector2 directionFan = aim.Rotated(angle).Normalized();
        return new ProjectileLaunchPlan(
            origin + directionFan * Math.Max(0.0f, spawnDistance),
            directionFan, PlayerProjectileChannel.Ordinary);
    }

    /// <summary>
    /// 辐射把弹丸等角铺满圆周；螺旋按二至四条臂分组，并让后续层逐步偏转形成旋线。
    /// </summary>
    private static ProjectileLaunchPlan ResolveCenteredBarrage(
        Vector2 origin,
        float spawnDistance,
        PlayerBarrageSnapshot plan,
        int index)
    {
        int count = Math.Max(1, plan.BarrageProjectileCount);
        double angle = plan.BarrageMode == PlayerBarrageMode.Spiral
            ? GetSpiralAngle(plan, index)
            : plan.BarrageRotationRadians + Math.Tau * index / count;
        Vector2 direction = Vector2.Right.Rotated((float)angle).Normalized();
        float layerOffset = plan.BarrageMode == PlayerBarrageMode.Spiral
            ? index / Math.Max(2, plan.BarrageSpiralArmCount) * 1.5f
            : 0.0f;
        return new ProjectileLaunchPlan(
            origin + direction * (Math.Max(0.0f, spawnDistance) + layerOffset),
            direction, PlayerProjectileChannel.Barrage);
    }

    /// <summary>返回指定弹丸在多重螺旋中的臂角和层偏移角，臂数始终限制在二至四。</summary>
    private static double GetSpiralAngle(PlayerBarrageSnapshot plan, int index)
    {
        int arms = Math.Clamp(plan.BarrageSpiralArmCount, 2, 4);
        int arm = index % arms;
        int layer = index / arms;
        return plan.BarrageRotationRadians + Math.Tau * arm / arms +
            layer * Math.PI / 14.0;
    }
}
