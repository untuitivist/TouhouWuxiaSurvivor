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
    /// 返回射程内全部存活敌人，供封魔阵等范围效果逐一走正常伤害流程。
    /// </summary>
    public static IReadOnlyList<EnemyActor> SelectInRange(
        Node2D enemyContainer,
        Vector2 origin,
        float range)
    {
        if (range <= 0.0f)
        {
            return [];
        }

        float rangeSquared = range * range;
        return enemyContainer.GetChildren()
            .OfType<EnemyActor>()
            .Where(enemy => enemy.IsAlive &&
                enemy.GlobalPosition.DistanceSquaredTo(origin) <= rangeSquared)
            .ToArray();
    }
}
