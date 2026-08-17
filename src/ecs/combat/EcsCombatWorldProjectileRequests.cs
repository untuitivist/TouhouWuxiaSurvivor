using TouhouWuxiaSurvivor.Ecs.Combat.Projectiles;

namespace TouhouWuxiaSurvivor.Ecs.Combat;

/// <summary>
/// 保存完整敌弹请求的世界桥接入口，使战斗主循环不展开复合弹型和运动档案的字段列表。
/// </summary>
public partial class EcsCombatWorld
{
    /// <summary>写入一枚完整敌弹并更新累计诊断值；容量不足时保持请求无副作用。</summary>
    private bool SpawnEnemyProjectile(EnemyProjectileSpawnRequest request)
    {
        bool spawned = EcsProjectileSpawner.TrySpawnEnemy(_projectiles, request);
        if (spawned) TotalEnemyProjectilesSpawned++;
        return spawned;
    }
}
