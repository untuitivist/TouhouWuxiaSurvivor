using TouhouWuxiaSurvivor.Actors.Enemies;

namespace TouhouWuxiaSurvivor.Gameplay.Difficulty;

/// <summary>
/// 保留旧存档与测试调用形状的固定属性边界；阶段系统不得借此修改怪物定义。
/// </summary>
public static class EnemyDifficultyScaler
{
    public const double TierSeconds = 10.0;

    /// <summary>
    /// 将本局时间整理为十秒档位；非法时间按开局处理，极端长局在长整型边界饱和。
    /// </summary>
    public static long GetTier(double elapsedSeconds)
    {
        if (double.IsNaN(elapsedSeconds) || elapsedSeconds <= 0.0)
        {
            return 0L;
        }

        double tier = Math.Floor(elapsedSeconds / TierSeconds);
        return tier >= long.MaxValue ? long.MaxValue : (long)tier;
    }

    /// <summary>
    /// 返回原始定义本身；档位参数只为兼容旧调用，明确不参与任何怪物数值计算。
    /// </summary>
    public static EnemyDefinition Scale(EnemyDefinition definition, long tier)
    {
        ArgumentNullException.ThrowIfNull(definition);
        _ = tier;
        return definition;
    }

    /// <summary>
    /// 将作者填写的基础接触伤害取最近正整数；不接受阶段倍率，避免重新引入全局成长。
    /// </summary>
    public static int NormalizeContactDamage(double baseDamage)
    {
        double normalizedBase = double.IsFinite(baseDamage) ? Math.Max(1.0, baseDamage) : 1.0;
        return (int)Math.Clamp(
            Math.Round(normalizedBase, MidpointRounding.AwayFromZero),
            1.0,
            int.MaxValue / 2.0);
    }
}
