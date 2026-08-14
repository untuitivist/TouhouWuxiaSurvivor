namespace TouhouWuxiaSurvivor.Ecs.Combat.Projectiles;

/// <summary>
/// 保存一轮弹幕的首击、贯穿与单弹范围投影，供运行时、状态面板和数值模拟共享。
/// </summary>
public readonly record struct ProjectileVolleyDamageSnapshot(
    int ProjectileCount,
    int PrimaryTotalDamage,
    int SecondaryTotalDamage,
    int MinimumPrimaryDamage,
    int MaximumPrimaryDamage,
    int MinimumSecondaryDamage,
    int MaximumSecondaryDamage)
{
    /// <summary>获取两名目标都承受完整可用命中时的整轮总伤害。</summary>
    public int TwoTargetTotalDamage => PrimaryTotalDamage + SecondaryTotalDamage;

    /// <summary>按弹丸索引获取首名目标伤害，分配结果之和恒等于首击总预算。</summary>
    public int GetPrimaryDamage(int projectileIndex) =>
        ProjectileDamageBudget.Distribute(
            PrimaryTotalDamage, ProjectileCount, projectileIndex);

    /// <summary>按弹丸索引获取次名目标伤害，零预算弹不会获得隐含的一点伤害。</summary>
    public int GetSecondaryDamage(int projectileIndex) =>
        ProjectileDamageBudget.Distribute(
            SecondaryTotalDamage, ProjectileCount, projectileIndex);
}
