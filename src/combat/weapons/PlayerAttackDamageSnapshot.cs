using TouhouWuxiaSurvivor.Ecs.Combat.Projectiles;

namespace TouhouWuxiaSurvivor.Combat.Weapons;

/// <summary>
/// 保存一轮普通弹与中心弹幕的分项整数账目；两者共享同一单弹数值。
/// </summary>
public readonly record struct PlayerAttackDamageSnapshot(
    ProjectileVolleyDamageSnapshot Ordinary,
    ProjectileVolleyDamageSnapshot Barrage)
{
    public int ProjectileCount => GetCount(Ordinary) + GetCount(Barrage);
    public int PrimaryTotalDamage =>
        Ordinary.PrimaryTotalDamage + Barrage.PrimaryTotalDamage;
    public int SecondaryTotalDamage =>
        Ordinary.SecondaryTotalDamage + Barrage.SecondaryTotalDamage;

    /// <summary>把两个通道压成只用于显示的齐射摘要，不用于逐弹伤害回查。</summary>
    public ProjectileVolleyDamageSnapshot CreateSummary()
    {
        int count = ProjectileCount;
        if (count <= 0)
        {
            return default;
        }

        int minimum = MinimumPositive(
            Ordinary.MinimumPrimaryDamage,
            Barrage.MinimumPrimaryDamage);
        int maximum = Math.Max(
            Ordinary.MaximumPrimaryDamage,
            Barrage.MaximumPrimaryDamage);
        return new ProjectileVolleyDamageSnapshot(
            count, PrimaryTotalDamage, SecondaryTotalDamage,
            minimum, maximum,
            MinimumPositive(Ordinary.MinimumSecondaryDamage,
                Barrage.MinimumSecondaryDamage),
            Math.Max(Ordinary.MaximumSecondaryDamage,
                Barrage.MaximumSecondaryDamage));
    }

    /// <summary>只把拥有正伤害的快照计为有效通道，默认结构不会虚增一颗弹丸。</summary>
    private static int GetCount(ProjectileVolleyDamageSnapshot snapshot) =>
        snapshot.PrimaryTotalDamage > 0 ? snapshot.ProjectileCount : 0;

    /// <summary>返回两个非负数中的最小正数；两者均为零时稳定返回零。</summary>
    private static int MinimumPositive(int first, int second)
    {
        if (first <= 0) return Math.Max(0, second);
        if (second <= 0) return first;
        return Math.Min(first, second);
    }
}
