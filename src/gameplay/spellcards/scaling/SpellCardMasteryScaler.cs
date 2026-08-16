using TouhouWuxiaSurvivor.Gameplay.SpellCards.Contracts;

namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Scaling;

/// <summary>
/// 把奥义第二重投影为统一化境收益；首重保持内容原值，第二重提高伤害与护持而不占新槽位。
/// </summary>
public static class SpellCardMasteryScaler
{
    public const float MasteryMultiplier = 1.35f;

    /// <summary>按当前重数复制最终参数；未悟或首重不增益，二重以上统一钳制为一次化境。</summary>
    public static ResolvedSpellCardCombat Apply(
        ResolvedSpellCardCombat combat,
        int rank)
    {
        ArgumentNullException.ThrowIfNull(combat);
        if (rank <= 1)
        {
            return combat;
        }

        int damage = (int)Math.Clamp(
            Math.Round(combat.Damage * (double)MasteryMultiplier,
                MidpointRounding.AwayFromZero),
            1.0,
            int.MaxValue);
        return new ResolvedSpellCardCombat(
            combat.IntervalSeconds,
            combat.EffectRange,
            damage,
            combat.TargetCount,
            combat.ActivationThreshold,
            Math.Min(float.MaxValue, combat.DefenseSeconds * MasteryMultiplier),
            combat.ProjectileSpeed,
            combat.ImpactRange,
            combat.TravelDurationSeconds,
            combat.SpawnDistance,
            combat.CastLockSeconds);
    }
}
