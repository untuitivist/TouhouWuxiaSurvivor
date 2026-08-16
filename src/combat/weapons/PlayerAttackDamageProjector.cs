using TouhouWuxiaSurvivor.Ecs.Combat.Projectiles;
using TouhouWuxiaSurvivor.Gameplay.Difficulty;

namespace TouhouWuxiaSurvivor.Combat.Weapons;

/// <summary>
/// 先确定共享单弹伤害，再按普通弹与中心弹幕数量分别汇总，保证两类数值一致。
/// </summary>
public static class PlayerAttackDamageProjector
{
    /// <summary>
    /// 普通弹允许消费贯穿次数；弹幕只改变数量和演出，不能获得独立伤害倍率。
    /// </summary>
    public static PlayerAttackDamageSnapshot Project(
        double baseDamage,
        PlayerBarrageSnapshot pattern,
        float sharedDamageMultiplier,
        int ordinaryMaximumHits)
    {
        if (pattern.ProjectileCount <= 0)
        {
            return default;
        }

        int sharedDamage = ProjectileDamageBudget.CalculateVolleyDamage(
            baseDamage, NormalizeMultiplier(sharedDamageMultiplier), 1);
        ProjectileVolleyDamageSnapshot ordinary = ProjectileDamageBudget.Project(
            sharedDamage, pattern.OrdinaryProjectileCount,
            pattern.OrdinaryProjectileCount, Math.Max(1, ordinaryMaximumHits));
        ProjectileVolleyDamageSnapshot barrage = pattern.BarrageProjectileCount <= 0
            ? default
            : ProjectileDamageBudget.Project(
                sharedDamage, pattern.BarrageProjectileCount,
                pattern.BarrageProjectileCount, 1);
        return new PlayerAttackDamageSnapshot(ordinary, barrage);
    }

    /// <summary>整理非法倍率为零，防止构筑数据污染整数伤害投影。</summary>
    private static float NormalizeMultiplier(float multiplier) =>
        float.IsFinite(multiplier) ? Math.Max(0.0f, multiplier) : 0.0f;
}
