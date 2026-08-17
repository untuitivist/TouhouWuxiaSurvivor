using Godot;

namespace TouhouWuxiaSurvivor.Ecs.Combat.Projectiles;

/// <summary>
/// 集中执行玩家与敌方投射物的容量、寿命、阵营和视觉来源写入，使 ECS 世界只负责编排事件。
/// </summary>
public static class EcsProjectileSpawner
{
    /// <summary>按玩家弹统一生命周期写入一颗投射物，并保留贯穿预算与内容来源。</summary>
    public static bool TrySpawnPlayer(
        ProjectilePool projectiles,
        Vector2 position,
        Vector2 direction,
        float speed,
        int damage,
        int maximumHits,
        int secondaryHitDamage,
        int visualVariant,
        int visualSourceId) => projectiles.TryAdd(
            position, direction, speed, damage, ProjectileFaction.Player,
            ProjectileKinematicsPolicy.PlayerLifetimeSeconds, 4.0f, visualVariant,
            out _, maximumHits, secondaryHitDamage, visualSourceId: visualSourceId);

    /// <summary>在敌方与全局容量预算内写入敌弹，并完整保存符卡及内容包视觉编号。</summary>
    public static bool TrySpawnEnemy(
        ProjectilePool projectiles,
        Vector2 position,
        Vector2 direction,
        float speed,
        int damage,
        int visualVariant,
        int visualStyleId,
        int visualSourceId)
    {
        if (projectiles.CountFaction(ProjectileFaction.Enemy) >=
                ProjectilePool.MaximumEnemyActive ||
            projectiles.Count >= ProjectilePool.MaximumActive)
        {
            return false;
        }

        return projectiles.TryAdd(position, direction, speed, damage,
            ProjectileFaction.Enemy, 7.0f, 3.5f, visualVariant, out _,
            visualStyleId: visualStyleId, visualSourceId: visualSourceId);
    }
}
