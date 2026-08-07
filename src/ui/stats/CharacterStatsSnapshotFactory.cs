using TouhouWuxiaSurvivor.Actors.Player;
using TouhouWuxiaSurvivor.Combat.Weapons;
using TouhouWuxiaSurvivor.Gameplay.Meta.Runtime;
using TouhouWuxiaSurvivor.Gameplay.Progression.Runtime;
using TouhouWuxiaSurvivor.Gameplay.Spawning;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Runtime;

namespace TouhouWuxiaSurvivor.Ui.Stats;

/// <summary>
/// 从明确注入的角色、武器和成长组件计算属性面板的最终实效数值。
/// </summary>
public static class CharacterStatsSnapshotFactory
{
    /// <summary>
    /// 合成临时强化、局外整备与本局武学，并同时生成两类来源摘要。
    /// </summary>
    public static CharacterStatsSnapshot Create(
        PlayerController player,
        PlayerHealth health,
        AutoShooter shooter,
        PlayerBuffController buffs,
        SpiritDropSpawner spiritSpawner,
        RunProgressionCoordinator progression,
        ProfileRunBonuses permanent,
        SpellCardCoordinator spellCards)
    {
        RunModifierState modifiers = progression.Modifiers;
        float fireRate = Math.Max(0.1f,
            buffs.FireRateMultiplier * modifiers.FireRateMultiplier);
        float moveSpeed = player.MoveSpeed * buffs.SpeedMultiplier *
            modifiers.MoveSpeedMultiplier;
        return new CharacterStatsSnapshot(
            "博丽灵梦",
            health.CurrentHealth,
            health.MaxHealth,
            progression.State.Level,
            progression.State.Experience,
            progression.State.ExperienceToNext,
            progression.State.TotalExperience,
            shooter.Damage + modifiers.DamageBonus,
            shooter.BaseFireInterval / fireRate,
            moveSpeed,
            shooter.TargetRange * modifiers.TargetRangeMultiplier,
            shooter.ProjectileSpeed * modifiers.ProjectileSpeedMultiplier,
            spiritSpawner.BaseAttractionRange * modifiers.SpiritAttractionMultiplier,
            FormatPermanentSummary(permanent),
            progression.Build.Describe(),
            spellCards.CreateSnapshot());
    }

    /// <summary>
    /// 把四项博丽神社整备投影为紧凑来源文字，没有加成时明确显示尚未整备。
    /// </summary>
    private static string FormatPermanentSummary(ProfileRunBonuses bonuses)
    {
        int movePercent = (int)MathF.Round((bonuses.MoveSpeedMultiplier - 1.0f) * 100.0f);
        int attractionPercent = (int)MathF.Round(
            (bonuses.SpiritAttractionMultiplier - 1.0f) * 100.0f);
        if (bonuses.MaxHealthBonus == 0 && bonuses.DamageBonus == 0 &&
            movePercent == 0 && attractionPercent == 0)
        {
            return "尚未整备";
        }

        return $"护身结界 +{bonuses.MaxHealthBonus}生命 · " +
            $"封魔针 +{bonuses.DamageBonus}伤害 · " +
            $"飘浮 +{movePercent}% · 阴阳玉 +{attractionPercent}%";
    }
}
