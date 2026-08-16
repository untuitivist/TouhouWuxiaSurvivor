using TouhouWuxiaSurvivor.Actors.Player;
using TouhouWuxiaSurvivor.Combat.Weapons;
using TouhouWuxiaSurvivor.Gameplay.Meta.Runtime;
using TouhouWuxiaSurvivor.Gameplay.Progression.Runtime;
using TouhouWuxiaSurvivor.Gameplay.Spawning;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Runtime;
using TouhouWuxiaSurvivor.Content;
using TouhouWuxiaSurvivor.Content.Characters;
using TouhouWuxiaSurvivor.Ui.Stats.Build;
using TouhouWuxiaSurvivor.Ecs.Combat.Projectiles;
using TouhouWuxiaSurvivor.Gameplay.Difficulty;

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
        SpellCardCoordinator spellCards,
        ContentPackSelection content)
    {
        RunModifierState modifiers = progression.Modifiers;
        string characterName = player.GetNode<PlayerVisualController>("Visual").DisplayName;
        CharacterCombatRole combatRole = CharacterCatalog.GetRequiredByDisplayName(
            characterName).CombatRole;
        float fireRate = Math.Max(0.1f,
            buffs.FireRateMultiplier * modifiers.FireRateMultiplier *
            shooter.PassiveFireRateMultiplier);
        float moveSpeed = player.MoveSpeed * buffs.SpeedMultiplier *
            modifiers.MoveSpeedMultiplier *
            (player.PassiveSpecializations?.MoveSpeedMultiplier ?? 1.0f);
        PlayerBarrageSnapshot barrage = PlayerBarrageCurve.EvaluateSeconds(
            0.0,
            modifiers.UsesConvergingBarrage || buffs.IsSpiralActive,
            0,
            0,
            modifiers.BarrageProjectileBonus,
            modifiers.AimedProjectileBonus);
        PlayerAttackDamageSnapshot attack = shooter.ProjectAttackDamage(barrage);
        ProjectileVolleyDamageSnapshot volley = attack.CreateSummary();
        return new CharacterStatsSnapshot(
            characterName,
            combatRole,
            health.CurrentHealth,
            health.MaxHealth,
            progression.State.Level,
            progression.State.Experience,
            progression.State.ExperienceToNext,
            progression.State.TotalExperience,
            volley.PrimaryTotalDamage,
            barrage.AimedProjectileCount,
            attack.PredictiveAim.PrimaryTotalDamage,
            barrage.BarrageProjectileCount,
            attack.Barrage.PrimaryTotalDamage,
            volley.ProjectileCount,
            volley.MinimumPrimaryDamage,
            volley.MaximumPrimaryDamage,
            volley.SecondaryTotalDamage,
            (float)AutoAttackCadence.CalculateInterval(
                shooter.BaseFireInterval,
                shooter.CharacterAttackIntervalMultiplier,
                fireRate),
            moveSpeed,
            shooter.TargetRange * modifiers.TargetRangeMultiplier,
            shooter.GetEffectiveProjectileSpeed(),
            spiritSpawner.BaseAttractionRange * modifiers.SpiritAttractionMultiplier,
            FormatPermanentSummary(permanent),
            CharacterBuildViewModelFactory.Create(
                progression.Build, content, progression.State.Level, combatRole),
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
