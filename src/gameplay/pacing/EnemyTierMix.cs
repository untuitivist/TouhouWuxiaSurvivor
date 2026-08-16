using TouhouWuxiaSurvivor.Actors.Enemies;

namespace TouhouWuxiaSurvivor.Gameplay.Pacing;

/// <summary>
/// 保存一个刷新档位的四档敌人占比；总和在构造时归一化，避免配置误差改变总刷新率。
/// </summary>
public readonly record struct EnemyTierMix
{
    public double Common { get; }
    public double Veteran { get; }
    public double Elite { get; }
    public double Champion { get; }

    /// <summary>建立非负占比并归一化；全零配置直接失败，防止刷怪器无法选档。</summary>
    public EnemyTierMix(double common, double veteran, double elite, double champion)
    {
        double[] values = [common, veteran, elite, champion];
        if (values.Any(value => !double.IsFinite(value) || value < 0.0))
        {
            throw new ArgumentOutOfRangeException(nameof(common),
                "Enemy tier weights must be finite and non-negative.");
        }

        double total = values.Sum();
        if (total <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(common),
                "Enemy tier weights cannot all be zero.");
        }

        Common = common / total;
        Veteran = veteran / total;
        Elite = elite / total;
        Champion = champion / total;
    }

    /// <summary>按稳定枚举返回指定档占比，供调度器避免复制四分支数据。</summary>
    public double GetWeight(EnemyStrengthTier tier) => tier switch
    {
        EnemyStrengthTier.Common => Common,
        EnemyStrengthTier.Veteran => Veteran,
        EnemyStrengthTier.Elite => Elite,
        EnemyStrengthTier.Champion => Champion,
        _ => 0.0,
    };

    /// <summary>在两个已归一化配比之间线性过渡，供换档前三秒避免强度占比瞬间跳变。</summary>
    public static EnemyTierMix Lerp(EnemyTierMix from, EnemyTierMix to, double weight)
    {
        double t = Math.Clamp(weight, 0.0, 1.0);
        return new EnemyTierMix(
            from.Common + (to.Common - from.Common) * t,
            from.Veteran + (to.Veteran - from.Veteran) * t,
            from.Elite + (to.Elite - from.Elite) * t,
            from.Champion + (to.Champion - from.Champion) * t);
    }
}
