using Godot;

namespace TouhouWuxiaSurvivor.Ecs.Combat;

/// <summary>批量执行敌人追踪、接触伤害和死亡反馈计时。</summary>
public sealed class EnemyMovementSystem
{
    /// <summary>让存活敌人追踪玩家，并回收死亡反馈结束的数据项。</summary>
    public void Step(EnemyPool pool, Vector2 playerPosition, float delta, Action<int> damagePlayer)
    {
        for (int index = pool.Count - 1; index >= 0; index--)
        {
            EnemyComponent enemy = pool.Get(index);
            enemy.HurtTime = Math.Max(0.0f, enemy.HurtTime - delta);
            if (!enemy.Alive)
            {
                enemy.DeathTime = Math.Max(0.0f, enemy.DeathTime - delta);
                if (enemy.DeathTime <= 0.0f)
                {
                    pool.RemoveSwap(index);
                    pool.TrimLast();
                    continue;
                }

                pool.Set(index, enemy);
                continue;
            }

            Vector2 direction = enemy.Position.DirectionTo(playerPosition);
            enemy.Position += direction * enemy.Definition.MoveSpeed * delta;
            enemy.TouchCooldown = Math.Max(0.0f, enemy.TouchCooldown - delta);
            if (enemy.Position.DistanceSquaredTo(playerPosition) <=
                MathF.Pow(enemy.Definition.CollisionRadius + 7.0f, 2.0f) &&
                enemy.TouchCooldown <= 0.0f)
            {
                damagePlayer(1);
                enemy.TouchCooldown = 0.5f;
            }

            pool.Set(index, enemy);
        }
    }
}
