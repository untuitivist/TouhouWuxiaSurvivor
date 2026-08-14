using TouhouWuxiaSurvivor.Content;
using TouhouWuxiaSurvivor.Gameplay.Progression.Runtime;

namespace TouhouWuxiaSurvivor.Gameplay.Balance;

/// <summary>
/// 保存模拟中的等级、累计灵息与当前等级余量，并把每次升级严格转换为一次合法构筑选择。
/// </summary>
internal sealed class BalanceExperienceLedger
{
    private double _currentExperience;
    public int Level { get; private set; } = 1;
    public long TotalExperience { get; private set; }

    /// <summary>
    /// 加入一秒内获得的期望灵息，跨越多个阈值时逐级应用正式选择，且对极端输入做有限饱和。
    /// </summary>
    public void AddExperience(
        double amount,
        RunBuildState build,
        BalanceBuildKind buildKind,
        ContentPackSelection content)
    {
        double finiteAmount = double.IsFinite(amount) ? Math.Max(0.0, amount) : 0.0;
        TotalExperience = (long)Math.Min(long.MaxValue / 2.0,
            TotalExperience + finiteAmount);
        _currentExperience = Math.Min(int.MaxValue * 2.0,
            _currentExperience + finiteAmount);
        int safety = 0;
        while (Level < int.MaxValue - 1 &&
            _currentExperience >= RunLevelCurve.GetRequiredExperience(Level))
        {
            _currentExperience -= RunLevelCurve.GetRequiredExperience(Level);
            Level++;
            BalanceBuildPlanner.ApplyLevelChoice(build, buildKind, Level, content);
            if (++safety > 10000)
            {
                throw new InvalidOperationException("Balance experience step exceeded safety limit.");
            }
        }
    }
}
