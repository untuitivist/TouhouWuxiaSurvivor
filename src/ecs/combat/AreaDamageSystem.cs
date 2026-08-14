using Godot;

namespace TouhouWuxiaSurvivor.Ecs.Combat;

/// <summary>
/// 为低频范围技能选择最近目标并结算距离衰减，避免效果层遍历或修改连续敌人池。
/// </summary>
public sealed class AreaDamageSystem
{
    /// <summary>
    /// 对范围内最近的至多指定数量敌人结算伤害，中心为全伤且边缘不低于给定倍率。
    /// </summary>
    public int Apply(
        EnemyPool enemies,
        Vector2 origin,
        float range,
        int damage,
        int maximumTargets,
        float minimumMultiplier,
        Action<int, int> applyDamage)
    {
        if (!float.IsFinite(range) || range <= 0.0f || damage <= 0 || maximumTargets <= 0)
        {
            return 0;
        }

        float rangeSquared = range * range;
        var targets = new List<(int index, float distanceSquared)>();
        for (int index = 0; index < enemies.Count; index++)
        {
            EnemyComponent enemy = enemies.Get(index);
            float distanceSquared = origin.DistanceSquaredTo(enemy.Position);
            if (enemy.Alive && distanceSquared <= rangeSquared)
            {
                targets.Add((index, distanceSquared));
            }
        }

        targets.Sort((left, right) =>
        {
            int distanceOrder = left.distanceSquared.CompareTo(right.distanceSquared);
            return distanceOrder != 0 ? distanceOrder : left.index.CompareTo(right.index);
        });
        int count = Math.Min(maximumTargets, targets.Count);
        for (int targetIndex = 0; targetIndex < count; targetIndex++)
        {
            (int enemyIndex, float distanceSquared) = targets[targetIndex];
            int resolvedDamage = CalculateDamage(
                damage, MathF.Sqrt(distanceSquared), range, minimumMultiplier);
            applyDamage(enemyIndex, resolvedDamage);
        }

        return count;
    }

    /// <summary>
    /// 以线性曲线把中心到边缘映射为全伤到最低倍率，并用远离零取整保证正伤害至少为一。
    /// </summary>
    public static int CalculateDamage(
        int damage,
        float distance,
        float range,
        float minimumMultiplier)
    {
        if (damage <= 0 || !float.IsFinite(range) || range <= 0.0f)
        {
            return 0;
        }

        float minimum = Math.Clamp(minimumMultiplier, 0.0f, 1.0f);
        float ratio = Math.Clamp(distance / range, 0.0f, 1.0f);
        float multiplier = Mathf.Lerp(1.0f, minimum, ratio);
        return Math.Max(1, (int)MathF.Round(damage * multiplier,
            MidpointRounding.AwayFromZero));
    }
}
