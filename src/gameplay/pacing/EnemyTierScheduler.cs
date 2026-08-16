using TouhouWuxiaSurvivor.Actors.Enemies;

namespace TouhouWuxiaSurvivor.Gameplay.Pacing;

/// <summary>
/// 用配额债务而非独立随机数选择敌人强度，使短窗口也逐步逼近策划占比且不会连续成团。
/// </summary>
public sealed class EnemyTierScheduler
{
    private readonly double[] _debts = new double[4];
    private int _gear = -1;

    /// <summary>消费当前档位占比并返回一档；换档时清空旧债务，防止旧比例污染新阶段。</summary>
    public EnemyStrengthTier Select(int gear, EnemyTierMix mix)
    {
        if (_gear != gear)
        {
            Array.Clear(_debts);
            _gear = gear;
        }

        EnemyStrengthTier selected = EnemyStrengthTier.Common;
        double bestDebt = double.NegativeInfinity;
        foreach (EnemyStrengthTier tier in Enum.GetValues<EnemyStrengthTier>())
        {
            int index = (int)tier;
            _debts[index] += mix.GetWeight(tier);
            if (_debts[index] > bestDebt)
            {
                bestDebt = _debts[index];
                selected = tier;
            }
        }

        _debts[(int)selected] -= 1.0;
        return selected;
    }
}
