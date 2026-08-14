namespace TouhouWuxiaSurvivor.Ecs.Combat.Projectiles;

/// <summary>
/// 统一计算齐射整数伤害与贯穿衰减，避免多弹拆分后的逐弹取整放大总预算。
/// </summary>
public static class ProjectileDamageBudget
{
    public const float SecondaryHitMultiplier = 0.30f;

    /// <summary>
    /// 一次性投影整轮首击、次击和单弹范围；最大命中不足二时不会生成贯穿预算。
    /// </summary>
    public static ProjectileVolleyDamageSnapshot Project(
        double singleHitDamage,
        double volleyMultiplier,
        int projectileCount,
        int maximumHits)
    {
        int count = Math.Max(1, projectileCount);
        int primary = CalculateVolleyDamage(singleHitDamage, volleyMultiplier, count);
        int secondary = maximumHits > 1
            ? CalculateSecondaryVolleyDamage(primary)
            : 0;
        return new ProjectileVolleyDamageSnapshot(
            count,
            primary,
            secondary,
            primary / count,
            (primary + count - 1) / count,
            secondary / count,
            (secondary + count - 1) / count);
    }

    /// <summary>
    /// 将单发基础伤害和齐射倍率转换为可分配整数，并保证每颗实际弹丸至少造成一点伤害。
    /// </summary>
    public static int CalculateVolleyDamage(
        double singleHitDamage,
        double volleyMultiplier,
        int projectileCount)
    {
        int minimum = Math.Max(1, projectileCount);
        double raw = Math.Max(0.0, singleHitDamage) * Math.Max(0.0, volleyMultiplier);
        return (int)Math.Clamp(
            Math.Round(raw, MidpointRounding.AwayFromZero), minimum, int.MaxValue);
    }

    /// <summary>
    /// 先对整轮首击总伤计算次级预算，使五发低伤弹不会因每发最低一点而把贯穿收益放大。
    /// </summary>
    public static int CalculateSecondaryVolleyDamage(int primaryVolleyDamage) =>
        ScaleDamage(primaryVolleyDamage, SecondaryHitMultiplier);

    /// <summary>
    /// 把总伤稳定拆给指定弹丸；不能整除的余数依次交给前部弹丸，总和始终不变。
    /// </summary>
    public static int Distribute(int totalDamage, int projectileCount, int projectileIndex)
    {
        if (totalDamage <= 0 || projectileCount <= 0 ||
            projectileIndex < 0 || projectileIndex >= projectileCount)
        {
            return 0;
        }

        int quotient = totalDamage / projectileCount;
        int remainder = totalDamage % projectileCount;
        return quotient + (projectileIndex < remainder ? 1 : 0);
    }

    /// <summary>
    /// 按给定比例四舍五入后续命中；零预算保持为零，防止无伤害弹产生虚假受击事件。
    /// </summary>
    public static int ScaleDamage(int damage, float multiplier)
    {
        if (damage <= 0 || !float.IsFinite(multiplier) || multiplier <= 0.0f)
        {
            return 0;
        }

        return (int)Math.Clamp(
            Math.Round(damage * (double)multiplier, MidpointRounding.AwayFromZero),
            1.0,
            int.MaxValue);
    }
}
