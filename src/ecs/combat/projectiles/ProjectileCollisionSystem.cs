using Godot;
using TouhouWuxiaSurvivor.Actors.Enemies;
using TouhouWuxiaSurvivor.Ecs.Combat;

namespace TouhouWuxiaSurvivor.Ecs.Combat.Projectiles;

/// <summary>
/// 用距离检测替代每颗 Area2D 的碰撞节点，把命中判断集中在连续数据遍历中。
/// </summary>
public sealed class ProjectileCollisionSystem
{
    private readonly EnemySpatialHash _enemyIndex = new();

    public int LastCandidateChecks { get; private set; }

    public long LastNaiveComparisonUpperBound { get; private set; }

    /// <summary>
    /// 查询敌人容器中的存活敌人；命中后调用既有受伤入口并消费投射物。
    /// </summary>
    public void Resolve(ProjectilePool pool, Node2D enemyContainer)
    {
        LastCandidateChecks = 0;
        LastNaiveComparisonUpperBound = 0;
        for (int projectileIndex = pool.Count - 1; projectileIndex >= 0; projectileIndex--)
        {
            ProjectileComponent projectile = pool.Get(projectileIndex);
            if (projectile.Faction != ProjectileFaction.Player)
            {
                continue;
            }

            bool hit = false;
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

                ulong identity = enemy.GetInstanceId();
                if (identity == projectile.LastHitIdentity)
                {
                    continue;
                }

                if (projectile.Damage > 0)
                {
                    enemy.ReceiveDamage(projectile.Damage);
                }
                projectile.RemainingHits--;
                projectile.LastHitIdentity = identity;
                projectile.AdvanceHitDamage();
                hit = true;
                break;
            }

            if (hit && projectile.RemainingHits <= 0)
            {
                pool.RemoveSwap(projectileIndex);
                pool.TrimLast();
            }
            else if (hit)
            {
                pool.Set(projectileIndex, projectile);
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
        _enemyIndex.Build(enemies);
        LastCandidateChecks = 0;
        LastNaiveComparisonUpperBound =
            (long)projectiles.CountFaction(ProjectileFaction.Player) * _enemyIndex.AliveCount;
        for (int projectileIndex = projectiles.Count - 1; projectileIndex >= 0; projectileIndex--)
        {
            ProjectileComponent projectile = projectiles.Get(projectileIndex);
            bool consumed = projectile.Faction == ProjectileFaction.Player
                ? ResolvePlayerProjectile(ref projectile, enemies, damageEnemy)
                : ResolveEnemyProjectile(projectile, playerPosition, playerRadius, damagePlayer);
            if (!consumed)
            {
                projectiles.Set(projectileIndex, projectile);
                continue;
            }

            projectiles.RemoveSwap(projectileIndex);
            projectiles.TrimLast();
        }
    }

    /// <summary>查询玩家弹与存活敌人的圆形重叠，并把命中索引和伤害交回战斗世界处理死亡事件。</summary>
    private bool ResolvePlayerProjectile(
        ref ProjectileComponent projectile,
        EnemyPool enemies,
        Action<int, int> damageEnemy)
    {
        int candidateChecks = LastCandidateChecks;
        bool found = _enemyIndex.TryFindFirstOverlap(
            projectile, enemies, out int enemyIndex, ref candidateChecks);
        LastCandidateChecks = candidateChecks;
        if (!found)
        {
            return false;
        }

        if (projectile.Damage > 0)
        {
            damageEnemy(enemyIndex, projectile.Damage);
        }
        projectile.RemainingHits--;
        projectile.LastHitIdentity = (ulong)enemies.Get(enemyIndex).Entity.Value;
        projectile.AdvanceHitDamage();
        return projectile.RemainingHits <= 0;
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
