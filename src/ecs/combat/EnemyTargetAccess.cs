using Godot;
using TouhouWuxiaSurvivor.Combat.Targeting;
using TouhouWuxiaSurvivor.Ecs.Core;

namespace TouhouWuxiaSurvivor.Ecs.Combat;

/// <summary>
/// 集中维护低频敌人查询和按身份访问，使世界生命周期节点不承担筛选、排序与句柄解析细节。
/// </summary>
public sealed class EnemyTargetAccess
{
    /// <summary>在指定射程内寻找距离最近的存活敌人，只返回当前坐标以兼容自动射击入口。</summary>
    public bool TryFindNearest(
        EnemyPool enemies,
        Vector2 origin,
        float range,
        out Vector2 position)
    {
        bool found = TryFindNearestMotion(enemies, origin, range, out TargetMotion motion);
        position = motion.Position;
        return found;
    }

    /// <summary>在指定射程内寻找距离最近的存活敌人，同时返回当前坐标和移动系统写入的权威速度。</summary>
    public bool TryFindNearestMotion(
        EnemyPool enemies,
        Vector2 origin,
        float range,
        out TargetMotion motion)
    {
        motion = default;
        float best = range * range;
        bool found = false;
        for (int index = 0; index < enemies.Count; index++)
        {
            EnemyComponent enemy = enemies.Get(index);
            if (!enemy.Alive)
            {
                continue;
            }

            float distance = origin.DistanceSquaredTo(enemy.Position);
            if (distance >= best)
            {
                continue;
            }

            best = distance;
            motion = new TargetMotion(enemy.Position, enemy.Velocity);
            found = true;
        }

        return found;
    }

    /// <summary>返回范围内按距离和实体号确定性排序的稳定目标，调用方决定是否只消费坐标。</summary>
    public IReadOnlyList<(EcsEntity Entity, Vector2 Position)> Select(
        EnemyPool enemies,
        Vector2 origin,
        float range,
        int maximum = int.MaxValue)
    {
        var result = new List<(float Distance, EcsEntity Entity, Vector2 Position)>();
        float squared = range * range;
        for (int index = 0; index < enemies.Count; index++)
        {
            EnemyComponent enemy = enemies.Get(index);
            float distance = origin.DistanceSquaredTo(enemy.Position);
            if (enemy.Alive && distance <= squared)
            {
                result.Add((distance, enemy.Entity, enemy.Position));
            }
        }

        return result.OrderBy(item => item.Distance)
            .ThenBy(item => item.Entity.Value)
            .Take(maximum)
            .Select(item => (item.Entity, item.Position))
            .ToArray();
    }

    /// <summary>按稳定句柄读取活体目标的最新坐标；死亡、回收或无效句柄统一返回 false。</summary>
    public bool TryGetPosition(
        EnemyPool enemies,
        EcsEntity entity,
        out Vector2 position)
    {
        if (enemies.TryGetAlive(entity, out _, out EnemyComponent enemy))
        {
            position = enemy.Position;
            return true;
        }

        position = default;
        return false;
    }

    /// <summary>按稳定句柄解析当前池索引并调用世界的统一伤害委托，失效目标不会重定向。</summary>
    public bool Damage(
        EnemyPool enemies,
        EcsEntity entity,
        int damage,
        Action<int, int> applyDamage)
    {
        if (damage <= 0 || !enemies.TryGetAlive(entity, out int index, out _))
        {
            return false;
        }

        applyDamage(index, damage);
        return true;
    }
}
