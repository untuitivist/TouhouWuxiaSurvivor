using Godot;
using TouhouWuxiaSurvivor.Actors.Enemies;

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
}
