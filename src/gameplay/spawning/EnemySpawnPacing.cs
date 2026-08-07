namespace TouhouWuxiaSurvivor.Gameplay.Spawning;

/// <summary>
/// 集中定义生存时间对应的刷怪批量、间隔和动态存活上限。
/// </summary>
public static class EnemySpawnPacing
{
    /// <summary>
    /// 在 120、240、420 秒提高单批数量，避免原先每 90 秒过快跳档。
    /// </summary>
    public static int GetBatchSize(double elapsedSeconds) => elapsedSeconds switch
    {
        < 120.0 => 1,
        < 240.0 => 2,
        < 420.0 => 3,
        _ => 4,
    };

    /// <summary>
    /// 十分钟内用平滑曲线把间隔从 0.85 秒缩至 0.32 秒，之后保持性能下限。
    /// </summary>
    public static double GetSpawnInterval(double elapsedSeconds)
    {
        double progress = Math.Clamp(elapsedSeconds / 600.0, 0.0, 1.0);
        double eased = progress * progress * (3.0 - 2.0 * progress);
        return 0.85 + (0.32 - 0.85) * eased;
    }

    /// <summary>
    /// 从 36 只开始每分钟增加 10 只，最终仍受场景配置硬上限约束。
    /// </summary>
    public static int GetAliveLimit(double elapsedSeconds, int hardLimit)
    {
        int stagedLimit = 36 + Math.Max(0, (int)(elapsedSeconds / 60.0)) * 10;
        return Math.Min(Math.Max(1, hardLimit), stagedLimit);
    }
}
