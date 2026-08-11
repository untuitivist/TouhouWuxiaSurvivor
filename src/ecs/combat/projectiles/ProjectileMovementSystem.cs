namespace TouhouWuxiaSurvivor.Ecs.Combat.Projectiles;

/// <summary>
/// 只负责推进位置和投射物寿命，不读取敌人或玩家规则，便于单独调参与测试。
/// </summary>
public sealed class ProjectileMovementSystem
{
    /// <summary>
    /// 按固定物理步长更新所有投射物，并回收寿命耗尽的数据项。
    /// </summary>
    public void Step(ProjectilePool pool, float delta)
    {
        for (int index = pool.Count - 1; index >= 0; index--)
        {
            var projectile = pool.Get(index);
            projectile.Position += projectile.Velocity * delta;
            projectile.Lifetime -= delta;
            if (projectile.Lifetime <= 0.0f)
            {
                pool.RemoveSwap(index);
                pool.TrimLast();
                continue;
            }

            pool.Set(index, projectile);
        }
    }
}
