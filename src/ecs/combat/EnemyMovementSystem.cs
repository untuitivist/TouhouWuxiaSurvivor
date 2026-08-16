using Godot;
using TouhouWuxiaSurvivor.Actors.Enemies;

namespace TouhouWuxiaSurvivor.Ecs.Combat;

/// <summary>
/// 批量执行追击、绕行射击、蓄势突进和角色 Boss 游走，并统一处理接触伤害与死亡回收。
/// </summary>
public sealed class EnemyMovementSystem
{
    /// <summary>
    /// 按定义中的固定速度推进 AI；阶段只改变敌群构成，不改变同种怪物的移动属性。
    /// </summary>
    public void Step(
        EnemyPool pool,
        Vector2 playerPosition,
        float delta,
        Action<int> damagePlayer)
    {
        for (int index = pool.Count - 1; index >= 0; index--)
        {
            EnemyComponent enemy = pool.Get(index);
            enemy.BeginPhysicsStep();
            enemy.HurtTime = Math.Max(0.0f, enemy.HurtTime - delta);
            if (!enemy.Alive)
            {
                if (AdvanceDeath(pool, index, ref enemy, delta))
                {
                    continue;
                }

                pool.Set(index, enemy);
                continue;
            }

            Move(ref enemy, playerPosition, delta);
            ResolveContact(ref enemy, playerPosition, delta, damagePlayer);
            pool.Set(index, enemy);
        }
    }

    /// <summary>
    /// 递减死亡反馈并在结束时用尾部交换删除实体；返回 true 表示调用方不能再写回旧索引。
    /// </summary>
    private static bool AdvanceDeath(
        EnemyPool pool,
        int index,
        ref EnemyComponent enemy,
        float delta)
    {
        enemy.DeathTime = Math.Max(0.0f, enemy.DeathTime - delta);
        if (enemy.DeathTime > 0.0f)
        {
            return false;
        }

        pool.RemoveSwap(index);
        pool.TrimLast();
        return true;
    }

    /// <summary>
    /// 根据不可变 AI 档案选择移动策略，运行组件只保存计时、方向和速度等短期状态。
    /// </summary>
    private static void Move(
        ref EnemyComponent enemy,
        Vector2 playerPosition,
        float delta)
    {
        switch (enemy.Definition.AiProfile.Kind)
        {
            case EnemyAiKind.OrbitShooter:
                MoveOrbit(ref enemy, playerPosition, delta);
                break;
            case EnemyAiKind.Charger:
                MoveCharger(ref enemy, playerPosition, delta);
                break;
            case EnemyAiKind.BossPhased:
                MoveBoss(ref enemy, playerPosition, delta);
                break;
            default:
                MoveChase(ref enemy, playerPosition, delta);
                break;
        }
    }

    /// <summary>沿玩家方向直接推进，作为低成本基础 AI 和未知档案的安全回退。</summary>
    private static void MoveChase(
        ref EnemyComponent enemy,
        Vector2 playerPosition,
        float delta)
    {
        enemy.Velocity = enemy.Position.DirectionTo(playerPosition) *
            enemy.Definition.MoveSpeed;
        enemy.Position += enemy.Velocity * delta;
    }

    /// <summary>根据偏好距离混合径向与切向速度，使射击敌人围绕玩家而非堆叠在脚下。</summary>
    private static void MoveOrbit(
        ref EnemyComponent enemy,
        Vector2 playerPosition,
        float delta)
    {
        EnemyAiProfile profile = enemy.Definition.AiProfile;
        Vector2 toPlayer = enemy.Position.DirectionTo(playerPosition);
        float distance = enemy.Position.DistanceTo(playerPosition);
        float radial = distance > profile.PreferredRange + 18.0f
            ? 0.75f
            : distance < profile.PreferredRange - 18.0f ? -0.75f : 0.0f;
        var tangent = new Vector2(-toPlayer.Y, toPlayer.X) * enemy.OrbitDirection;
        Vector2 direction = (toPlayer * radial + tangent * profile.TangentialWeight).Normalized();
        enemy.Velocity = direction * enemy.Definition.MoveSpeed;
        enemy.Position += enemy.Velocity * delta;
    }

    /// <summary>在低速追踪与锁定方向高速突进之间循环，突进开始后不会持续作弊式修正方向。</summary>
    private static void MoveCharger(
        ref EnemyComponent enemy,
        Vector2 playerPosition,
        float delta)
    {
        EnemyAiProfile profile = enemy.Definition.AiProfile;
        if (enemy.ChargeTimeLeft > 0.0f)
        {
            enemy.ChargeTimeLeft = Math.Max(0.0f, enemy.ChargeTimeLeft - delta);
            enemy.Position += enemy.Velocity * delta;
            return;
        }

        enemy.AiTimer -= delta;
        if (enemy.AiTimer <= 0.0f)
        {
            enemy.Velocity = enemy.Position.DirectionTo(playerPosition) *
                enemy.Definition.MoveSpeed * 3.2f;
            enemy.ChargeTimeLeft = profile.ChargeDuration;
            enemy.AiTimer = profile.ChargeInterval;
            enemy.Position += enemy.Velocity * delta;
            return;
        }

        enemy.Velocity = enemy.Position.DirectionTo(playerPosition) *
            enemy.Definition.MoveSpeed * profile.TangentialWeight;
        enemy.Position += enemy.Velocity * delta;
    }

    /// <summary>让 Boss 在较远半径游走，保留玩家穿越弹幕的空间并避免单纯贴身追击。</summary>
    private static void MoveBoss(
        ref EnemyComponent enemy,
        Vector2 playerPosition,
        float delta)
    {
        EnemyAiProfile profile = enemy.Definition.AiProfile;
        Vector2 toPlayer = enemy.Position.DirectionTo(playerPosition);
        float distance = enemy.Position.DistanceTo(playerPosition);
        float radial = Mathf.Clamp((distance - profile.PreferredRange) / 80.0f, -0.65f, 0.65f);
        var tangent = new Vector2(-toPlayer.Y, toPlayer.X) * enemy.OrbitDirection;
        Vector2 direction = (toPlayer * radial + tangent * profile.TangentialWeight).Normalized();
        enemy.Velocity = direction * enemy.Definition.MoveSpeed;
        enemy.Position += enemy.Velocity * delta;
    }

    /// <summary>统一处理接触半径、冷却和定义伤害，使 Boss 与普通敌人不再固定造成一点伤害。</summary>
    private static void ResolveContact(
        ref EnemyComponent enemy,
        Vector2 playerPosition,
        float delta,
        Action<int> damagePlayer)
    {
        enemy.TouchCooldown = Math.Max(0.0f, enemy.TouchCooldown - delta);
        float radius = enemy.Definition.CollisionRadius + 7.0f;
        if (enemy.TouchCooldown > 0.0f ||
            enemy.Position.DistanceSquaredTo(playerPosition) > radius * radius)
        {
            return;
        }

        damagePlayer(enemy.Definition.ContactDamage);
        enemy.TouchCooldown = enemy.Definition.IsBoss ? 0.8f : 0.5f;
    }
}
