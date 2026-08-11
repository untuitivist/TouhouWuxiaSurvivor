using Godot;
using TouhouWuxiaSurvivor.Actors.Enemies;
using TouhouWuxiaSurvivor.Ecs.Combat;

namespace TouhouWuxiaSurvivor.Ecs.Combat.Projectiles;

/// <summary>
/// 用距离检测替代每颗 Area2D 的碰撞节点，把命中判断集中在连续数据遍历中。
/// </summary>
public sealed class ProjectileCollisionSystem
{
    /// <summary>
    /// 查询敌人容器中的存活敌人；命中后调用既有受伤入口并消费投射物。
    /// </summary>
    public void Resolve(ProjectilePool pool, Node2D enemyContainer)
    {
        for (int projectileIndex = pool.Count - 1; projectileIndex >= 0; projectileIndex--)
        {
            ProjectileComponent projectile = pool.Get(projectileIndex);
            if (projectile.Faction != ProjectileFaction.Player)
            {
                continue;
            }

            bool consumed = false;
            foreach (Node child in enemyContainer.GetChildren())
            {
                if (child is not EnemyActor enemy || !enemy.IsAlive)
                {
                    continue;
                }

                float hitRadius = projectile.Radius + enemy.Definition.CollisionRadius;
                if (projectile.Position.DistanceSquaredTo(enemy.GlobalPosition) > hitRadius * hitRadius)
                {
                    continue;
                }

                enemy.ReceiveDamage(projectile.Damage);
                consumed = true;
                break;
            }

            if (consumed)
            {
                pool.RemoveSwap(projectileIndex);
                pool.TrimLast();
            }
        }
    }

    /// <summary>
    /// 在纯 ECS 池中按阵营解析碰撞：玩家弹遍历敌人，敌弹只检测玩家圆形碰撞，命中后统一消费。
    /// </summary>
    public void Resolve(
        ProjectilePool projectiles,
        EnemyPool enemies,
        Vector2 playerPosition,
        float playerRadius,
        Action<int, int> damageEnemy,
        Action<int> damagePlayer)
    {
        for (int projectileIndex = projectiles.Count - 1; projectileIndex >= 0; projectileIndex--)
        {
            ProjectileComponent projectile = projectiles.Get(projectileIndex);
            bool consumed = projectile.Faction == ProjectileFaction.Player
                ? ResolvePlayerProjectile(projectile, enemies, damageEnemy)
                : ResolveEnemyProjectile(projectile, playerPosition, playerRadius, damagePlayer);
            if (!consumed)
            {
                continue;
            }

            projectiles.RemoveSwap(projectileIndex);
            projectiles.TrimLast();
        }
    }

    /// <summary>查询玩家弹与存活敌人的圆形重叠，并把命中索引和伤害交回战斗世界处理死亡事件。</summary>
    private static bool ResolvePlayerProjectile(
        ProjectileComponent projectile,
        EnemyPool enemies,
        Action<int, int> damageEnemy)
    {
        for (int enemyIndex = 0; enemyIndex < enemies.Count; enemyIndex++)
        {
            EnemyComponent enemy = enemies.Get(enemyIndex);
            float radius = projectile.Radius + enemy.Definition.CollisionRadius;
            if (!enemy.Alive ||
                projectile.Position.DistanceSquaredTo(enemy.Position) > radius * radius)
            {
                continue;
            }

            damageEnemy(enemyIndex, projectile.Damage);
            return true;
        }

        return false;
    }

    /// <summary>查询敌弹与玩家圆形碰撞；阵营已经由上层分派，因此不会误伤其他敌人。</summary>
    private static bool ResolveEnemyProjectile(
        ProjectileComponent projectile,
        Vector2 playerPosition,
        float playerRadius,
        Action<int> damagePlayer)
    {
        float radius = projectile.Radius + Math.Max(1.0f, playerRadius);
        if (projectile.Position.DistanceSquaredTo(playerPosition) > radius * radius)
        {
            return false;
        }

        damagePlayer(projectile.Damage);
        return true;
    }
}
