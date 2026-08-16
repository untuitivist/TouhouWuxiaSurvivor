namespace TouhouWuxiaSurvivor.Gameplay.Encounters;

/// <summary>
/// 集中保存角色 Boss 的全局战斗预算；不同角色仍只通过自己的基础档案形成差异。
/// </summary>
public static class BossCombatBalancePolicy
{
    public const float HealthMultiplier = 6.0f;

    /// <summary>把角色基础耐久提升为正式 Boss 耐久，并在整数边界执行饱和。</summary>
    public static int ScaleHealth(float baseHealth) => (int)Math.Clamp(
        Math.Ceiling(Math.Max(1.0f, baseHealth) * HealthMultiplier),
        1.0,
        int.MaxValue / 2.0);
}
