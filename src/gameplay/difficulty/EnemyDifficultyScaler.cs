using TouhouWuxiaSurvivor.Actors.Enemies;

namespace TouhouWuxiaSurvivor.Gameplay.Difficulty;

/// <summary>
/// 把共享无尽难度快照投影为普通敌人的出生数值，并用短时间档位支持刷怪器低分配缓存。
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
    /// 按指定档位复制普通敌人定义，持续提高生命、移动与接触伤害；Boss 已由专属工厂缩放，不重复处理。
    /// </summary>
    public static EnemyDefinition Scale(EnemyDefinition definition, long tier)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (definition.IsBoss)
        {
            return definition;
        }

        double elapsedSeconds = Math.Max(0L, tier) * TierSeconds;
        EndlessDifficultySnapshot snapshot = EndlessDifficultyCurve.EvaluateSeconds(
            elapsedSeconds, int.MaxValue);
        return new EnemyDefinition(
            definition.Archetype,
            definition.DisplayName,
            SaturatingPositiveInt(definition.MaxHealth * snapshot.EnemyHealthMultiplier),
            (float)Math.Min(280.0, definition.MoveSpeed * snapshot.EnemySpeedMultiplier),
            definition.CollisionRadius,
            definition.SpawnWeight,
            definition.UnlockTime,
            definition.DropChance,
            definition.AllowedBiomes,
            definition.ExplodesOnDeath,
            definition.RequiredContentPack,
            ScaleContactDamage(definition.ContactDamage, snapshot.EnemyDamageMultiplier),
            definition.AiProfile,
            definition.ProjectileProfile,
            definition.IsBoss,
            definition.CharacterId,
            definition.BaseMaxHealth);
    }

    /// <summary>
    /// 将基础接触伤害按共享倍率取最近整数；这样 1 点伤害不会因十秒档位的微小增长立刻跳成 2 点，
    /// 同时倍率跨过半整数阈值后仍会持续提高，普通敌人与角色 Boss 共用完全相同的离散规则。
    /// </summary>
    public static int ScaleContactDamage(double baseDamage, double multiplier)
    {
        double normalizedBase = double.IsFinite(baseDamage) ? Math.Max(1.0, baseDamage) : 1.0;
        double normalizedMultiplier = double.IsFinite(multiplier) ? Math.Max(1.0, multiplier) : 1.0;
        double scaled = normalizedBase * normalizedMultiplier;
        return (int)Math.Clamp(
            Math.Round(scaled, MidpointRounding.AwayFromZero),
            1.0,
            int.MaxValue / 2.0);
    }

    /// <summary>把持续增长的正数倍率安全转换为战斗整数，避免极端长局发生符号翻转。</summary>
    private static int SaturatingPositiveInt(double value) =>
        (int)Math.Clamp(Math.Ceiling(value), 1.0, int.MaxValue / 2.0);
}
