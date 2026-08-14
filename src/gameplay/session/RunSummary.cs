namespace TouhouWuxiaSurvivor.Gameplay.Session;

/// <summary>
/// 保存一局结束时的不可变统计快照，使结算界面不依赖世界、刷怪器或玩家节点。
/// </summary>
public sealed class RunSummary
{
    public RunEndReason EndReason { get; }
    public double SurvivalSeconds { get; }
    public int DefeatedEnemies { get; }
    public long TileX { get; }
    public long TileY { get; }
    public string BiomeName { get; }
    public string ActiveContent { get; }
    public ulong WorldSeed { get; }
    public int FinalLevel { get; }
    public long TotalExperience { get; }
    public string BuildSummary { get; }
    public int RewardEarned { get; }
    public int MoneyBalance { get; }

    /// <summary>
    /// 接收本局最终数据并建立一次性快照，防止结算显示随后台节点继续变化。
    /// </summary>
    public RunSummary(
        RunEndReason endReason,
        double survivalSeconds,
        int defeatedEnemies,
        long tileX,
        long tileY,
        string biomeName,
        string activeContent,
        ulong worldSeed,
        int finalLevel,
        long totalExperience,
        string buildSummary,
        int rewardEarned,
        int moneyBalance)
    {
        EndReason = endReason;
        SurvivalSeconds = Math.Max(0.0, survivalSeconds);
        DefeatedEnemies = Math.Max(0, defeatedEnemies);
        TileX = tileX;
        TileY = tileY;
        BiomeName = biomeName;
        ActiveContent = activeContent;
        WorldSeed = worldSeed;
        FinalLevel = Math.Max(1, finalLevel);
        TotalExperience = Math.Max(0, totalExperience);
        BuildSummary = string.IsNullOrWhiteSpace(buildSummary) ? "尚未修习" : buildSummary;
        RewardEarned = Math.Max(0, rewardEarned);
        MoneyBalance = Math.Max(0, moneyBalance);
    }
}
