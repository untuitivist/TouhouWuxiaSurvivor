namespace TouhouWuxiaSurvivor.Gameplay.Difficulty;

using TouhouWuxiaSurvivor.Gameplay.Pacing;

/// <summary>
/// 以无状态纯函数投影敌群压力；只改变刷新率与种类配比，不修改任何怪物基础属性。
/// </summary>
public static class EndlessDifficultyCurve
{
    /// <summary>
    /// 把非负压力秒数转换为不可变快照；强度由刷新率和四档种类配比共同构成。
    /// </summary>
    public static EndlessDifficultySnapshot EvaluateSeconds(double elapsedSeconds)
    {
        double minutes = NormalizeMinutes(elapsedSeconds);
        EnemyPressureSnapshot pressure = EnemyPressureCurve.Evaluate(elapsedSeconds);
        double tierWeight = pressure.TierMix.Common * 1.0 +
            pressure.TierMix.Veteran * 1.35 +
            pressure.TierMix.Elite * 1.8 +
            pressure.TierMix.Champion * 2.5;
        double intensity = pressure.SpawnRatePerSecond * tierWeight /
            Math.Max(0.01, RunPacingTimeline.AdaptiveRules[0].SpawnRatePerSecond);
        return new EndlessDifficultySnapshot(minutes, intensity,
            pressure.SpawnRatePerSecond, pressure.TierMix);
    }

    /// <summary>保留旧调用形状但明确忽略历史硬上限；任何数值都不能再阻断普通敌人生成。</summary>
    public static EndlessDifficultySnapshot EvaluateSeconds(
        double elapsedSeconds,
        int ignoredAliveLimit) => EvaluateSeconds(elapsedSeconds);

    /// <summary>
    /// 将外部时间整理为有限非负分钟，避免暂停误差、非法存档或极端输入破坏曲线单调性。
    /// </summary>
    public static double NormalizeMinutes(double elapsedSeconds)
    {
        if (double.IsNaN(elapsedSeconds) || elapsedSeconds <= 0.0)
        {
            return 0.0;
        }

        return double.IsPositiveInfinity(elapsedSeconds)
            ? double.MaxValue / 60.0
            : elapsedSeconds / 60.0;
    }

}
