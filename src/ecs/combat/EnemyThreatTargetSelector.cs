using Godot;
using TouhouWuxiaSurvivor.Ecs.Core;

namespace TouhouWuxiaSurvivor.Ecs.Combat;

/// <summary>
/// 在连续敌人池中执行集中攻势的稳定威胁排序，不让世界生命周期类承担具体策划规则。
/// </summary>
public static class EnemyThreatTargetSelector
{
    /// <summary>
    /// 在射程内选择 Boss、当前生命、接触伤害和速度最高的敌人，并以距离和实体号稳定破同分。
    /// </summary>
    public static bool TrySelect(
        EnemyPool enemies,
        Vector2 origin,
        float range,
        out Vector2 position)
    {
        return TrySelect(enemies, origin, range, out _, out position);
    }

    /// <summary>
    /// 在射程内返回最高威胁敌人的稳定实体句柄和当前位置，供跨帧追踪而不保存易变池索引。
    /// </summary>
    public static bool TrySelect(
        EnemyPool enemies,
        Vector2 origin,
        float range,
        out EcsEntity entity,
        out Vector2 position)
    {
        ArgumentNullException.ThrowIfNull(enemies);
        entity = default;
        position = default;
        float squared = range * range;
        EnemyComponent? best = null;
        for (int index = 0; index < enemies.Count; index++)
        {
            EnemyComponent enemy = enemies.Get(index);
            if (!enemy.Alive || origin.DistanceSquaredTo(enemy.Position) > squared)
            {
                continue;
            }
            if (best is null || IsHigherThreat(enemy, best.Value, origin))
            {
                best = enemy;
            }
        }
        if (best is null)
        {
            return false;
        }
        entity = best.Value.Entity;
        position = best.Value.Position;
        return true;
    }

    /// <summary>使用字典序比较而非混合浮点评分，保证规则可解释且相同输入始终返回相同实体。</summary>
    private static bool IsHigherThreat(
        EnemyComponent candidate,
        EnemyComponent current,
        Vector2 origin)
    {
        int comparison = candidate.Definition.IsBoss.CompareTo(current.Definition.IsBoss);
        if (comparison != 0) return comparison > 0;
        comparison = candidate.Health.CompareTo(current.Health);
        if (comparison != 0) return comparison > 0;
        comparison = candidate.Definition.ContactDamage.CompareTo(
            current.Definition.ContactDamage);
        if (comparison != 0) return comparison > 0;
        comparison = candidate.Definition.MoveSpeed.CompareTo(current.Definition.MoveSpeed);
        if (comparison != 0) return comparison > 0;
        comparison = origin.DistanceSquaredTo(candidate.Position).CompareTo(
            origin.DistanceSquaredTo(current.Position));
        return comparison != 0
            ? comparison < 0
            : candidate.Entity.Value < current.Entity.Value;
    }
}
