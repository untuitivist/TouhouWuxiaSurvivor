using Godot;
using TouhouWuxiaSurvivor.Actors.Enemies;

namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Effects;

/// <summary>
/// 从显式敌人容器中选择距离施法者最近的存活目标，供追踪与范围符卡共用。
/// </summary>
public static class SpellCardTargetSelector
{
    /// <summary>
    /// 返回射程内至多指定数量的不同敌人，并按距离从近到远保持确定顺序。
    /// </summary>
    public static IReadOnlyList<EnemyActor> SelectNearest(
        Node2D enemyContainer,
        Vector2 origin,
        float range,
        int maximumCount)
    {
        if (range <= 0.0f || maximumCount <= 0)
        {
            return [];
        }

        float rangeSquared = range * range;
        return enemyContainer.GetChildren()
            .OfType<EnemyActor>()
            .Where(enemy => enemy.IsAlive &&
                enemy.GlobalPosition.DistanceSquaredTo(origin) <= rangeSquared)
            .OrderBy(enemy => enemy.GlobalPosition.DistanceSquaredTo(origin))
            .Take(maximumCount)
            .ToArray();
    }

    /// <summary>
    /// 返回射程内至多指定数量的最近敌人，供范围效果按统一命中预算逐一走正常伤害流程。
    /// </summary>
    public static IReadOnlyList<EnemyActor> SelectInRange(
        Node2D enemyContainer,
        Vector2 origin,
        float range,
        int maximumCount)
    {
        return SelectNearest(enemyContainer, origin, range, maximumCount);
    }

    /// <summary>
    /// 为集中重招选择最高威胁兼容节点，排序与 ECS 一致并以场景实例号形成稳定同分结果。
    /// </summary>
    public static EnemyActor? SelectHighestThreat(
        Node2D enemyContainer,
        Vector2 origin,
        float range)
    {
        float squared = range * range;
        return enemyContainer.GetChildren()
            .OfType<EnemyActor>()
            .Where(enemy => enemy.IsAlive &&
                enemy.GlobalPosition.DistanceSquaredTo(origin) <= squared)
            .OrderByDescending(enemy => enemy.Definition.IsBoss)
            .ThenByDescending(enemy => enemy.CurrentHealth)
            .ThenByDescending(enemy => enemy.Definition.ContactDamage)
            .ThenByDescending(enemy => enemy.Definition.MoveSpeed)
            .ThenBy(enemy => enemy.GlobalPosition.DistanceSquaredTo(origin))
            .ThenBy(enemy => enemy.GetInstanceId())
            .FirstOrDefault();
    }
}
