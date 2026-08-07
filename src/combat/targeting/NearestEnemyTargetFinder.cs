using Godot;
using TouhouWuxiaSurvivor.Actors.Enemies;

namespace TouhouWuxiaSurvivor.Combat.Targeting;

/// <summary>
/// 在指定敌人容器中执行无状态最近目标查询，供不同自动武器复用统一索敌规则。
/// </summary>
public sealed class NearestEnemyTargetFinder
{
    private readonly Node2D _enemyContainer;

    /// <summary>
    /// 绑定当前游戏页面拥有的敌人容器，使查询不会扫描无关场景节点。
    /// </summary>
    public NearestEnemyTargetFinder(Node2D enemyContainer) => _enemyContainer = enemyContainer;

    /// <summary>
    /// 返回射程内距离发射点最近的存活敌人；不存在合法目标时返回 null。
    /// </summary>
    public EnemyActor? FindNearest(Vector2 origin, float maximumRange)
    {
        EnemyActor? nearest = null;
        float nearestDistanceSquared = maximumRange * maximumRange;
        foreach (Node child in _enemyContainer.GetChildren())
        {
            if (child is not EnemyActor enemy || !enemy.IsAlive)
            {
                continue;
            }

            float distanceSquared = origin.DistanceSquaredTo(enemy.GlobalPosition);
            if (distanceSquared >= nearestDistanceSquared)
            {
                continue;
            }

            nearest = enemy;
            nearestDistanceSquared = distanceSquared;
        }

        return nearest;
    }
}
