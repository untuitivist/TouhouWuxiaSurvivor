using TouhouWuxiaSurvivor.Ecs.Combat.Projectiles;
using TouhouWuxiaSurvivor.Gameplay.Difficulty;

namespace TouhouWuxiaSurvivor.Combat.Weapons;

/// <summary>
/// 先确定共享单弹伤害，再按自瞄与弹幕数量分别汇总，保证两类弹丸数值完全一致。
/// </summary>
public static class PlayerAttackDamageProjector
{
    /// <summary>
    /// 自瞄通道允许消费贯穿次数；弹幕只改变数量，不能获得独立伤害倍率。
    /// </summary>
    public static PlayerAttackDamageSnapshot Project(
        double baseDamage,
        PlayerBarrageSnapshot pattern,
        float sharedDamageMultiplier,
        int aimedMaximumHits)
    {
        if (pattern.ProjectileCount <= 0)
        {
            return default;
        }

        int sharedDamage = ProjectileDamageBudget.CalculateVolleyDamage(
            baseDamage, NormalizeMultiplier(sharedDamageMultiplier), 1);
        ProjectileVolleyDamageSnapshot aimed = ProjectileDamageBudget.Project(
            sharedDamage, pattern.AimedProjectileCount,
            pattern.AimedProjectileCount, Math.Max(1, aimedMaximumHits));
        ProjectileVolleyDamageSnapshot barrage = pattern.BarrageProjectileCount <= 0
            ? default
            : ProjectileDamageBudget.Project(
                sharedDamage, pattern.BarrageProjectileCount,
                pattern.BarrageProjectileCount, 1);
        return new PlayerAttackDamageSnapshot(aimed, barrage);
    }

    /// <summary>整理非法倍率为零，防止构筑数据污染整数伤害投影。</summary>
    private static float NormalizeMultiplier(float multiplier) =>
        float.IsFinite(multiplier) ? Math.Max(0.0f, multiplier) : 0.0f;
}
