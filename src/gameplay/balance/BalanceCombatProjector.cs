using TouhouWuxiaSurvivor.Actors.Enemies;
using TouhouWuxiaSurvivor.Content.Characters;
using TouhouWuxiaSurvivor.Gameplay.Difficulty;
using TouhouWuxiaSurvivor.Gameplay.Progression.Definitions;
using TouhouWuxiaSurvivor.Gameplay.Progression.Runtime;
using TouhouWuxiaSurvivor.Ecs.Combat.Projectiles;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Balance;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Contracts;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;
using TouhouWuxiaSurvivor.Combat.Weapons;

namespace TouhouWuxiaSurvivor.Gameplay.Balance;

/// <summary>
/// 把正式角色、构筑和奥义投影为策划伤害与生存准备度，不复制战斗实体的帧级实现。
/// </summary>
internal static class BalanceCombatProjector
{
    private const double BaseTargetRange = 460.0;
    private const double BaseProjectileSpeed = 360.0;
    private const double BaseSpawnDistance = 18.0;

    /// <summary>
    /// 计算自动武器的单目标预算、已装备奥义的期望周期伤害，以及移动、覆盖和经济准备度。
    /// </summary>
    public static BalanceCombatMetrics Evaluate(
        double elapsedSeconds,
        CharacterDefinition character,
        RunBuildState build,
        BalanceBuildKind buildKind)
    {
        var modifiers = new RunModifierState();
        modifiers.Refresh(build);
        PlayerBarrageSnapshot barrage = PlayerBarrageCurve.EvaluateSeconds(
            elapsedSeconds, modifiers.UsesConvergingBarrage, 0L, 0,
            modifiers.BarrageProjectileBonus, modifiers.AimedProjectileBonus);
        double baseAttack = EnemyBalanceProfile.BaseWeaponDamage *
            character.PlayableProfile.AttackMultiplier * GetExpectedAttackMultiplier(modifiers);
        PlayerAttackDamageSnapshot damage = PlayerAttackDamageProjector.Project(
            baseAttack, barrage, modifiers.ProjectileDamageMultiplier,
            1 + modifiers.ProjectilePierceCount);
        double fireRate = modifiers.FireRateMultiplier *
            GetExpectedFireRateMultiplier(modifiers) /
            (EnemyBalanceProfile.BaseWeaponInterval *
                character.PlayableProfile.AttackIntervalMultiplier);
        double aimedContribution = damage.PredictiveAim.PrimaryTotalDamage *
            CalculateAimedHitRate(buildKind) +
            damage.PredictiveAim.SecondaryTotalDamage * 0.72;
        double barrageContribution = damage.Barrage.PrimaryTotalDamage *
            CalculateBarrageHitRate(barrage, buildKind);
        double weaponDps = (aimedContribution + barrageContribution) * fireRate;
        double spellAttackPower = baseAttack * modifiers.AttackPowerMultiplier;
        double spellDps = CalculateSpellContribution(
            character, build, modifiers, spellAttackPower);
        int offensive = SpellCardSlotPolicy.CountOccupied(build, SpellCardSlotKind.Offensive);
        int support = SpellCardSlotPolicy.CountOccupied(build, SpellCardSlotKind.Support);
        int endless = RunUpgradeCatalog.All.Where(item => item.IsRepeatable)
            .Sum(item => build.GetRank(item.Id));
        double readiness = CalculateReadiness(
            weaponDps + spellDps, modifiers, character, support);
        return new BalanceCombatMetrics(weaponDps, spellDps, weaponDps + spellDps,
            modifiers.MoveSpeedMultiplier * GetExpectedMoveMultiplier(modifiers),
            modifiers.TargetRangeMultiplier,
            modifiers.SpiritAttractionMultiplier, modifiers.SpiritYieldMultiplier,
            readiness, offensive, support, endless);
    }

    /// <summary>
    /// 预判弹采用高可靠命中率；路线只提供小幅差异，单弹伤害仍来自共享攻势倍率。
    /// </summary>
    private static double CalculateAimedHitRate(BalanceBuildKind buildKind) => buildKind switch
    {
        BalanceBuildKind.Assault => 0.94,
        BalanceBuildKind.Rapid => 0.88,
        BalanceBuildKind.Utility => 0.92,
        _ => 0.91,
    };

    /// <summary>按定向弹幕密度估算覆盖率；弹数既提高覆盖，也按共享单弹伤害增加总输出。</summary>
    private static double CalculateBarrageHitRate(
        PlayerBarrageSnapshot barrage,
        BalanceBuildKind buildKind)
    {
        double coverage = Math.Min(0.20, barrage.BarrageProjectileCount * 0.025);
        double route = buildKind == BalanceBuildKind.Utility ? 0.03 : 0.0;
        return Math.Clamp(0.62 + coverage + route, 0.58, 0.88);
    }

    /// <summary>
    /// 以统一贡献模型投影构筑中全部奥义，避免模拟器与内容契约分别维护触发、范围和护身权重。
    /// </summary>
    private static double CalculateSpellContribution(
        CharacterDefinition character,
        RunBuildState build,
        RunModifierState modifiers,
        double attackPower)
    {
        var attributes = new SpellCardBaseAttributes(
            (float)attackPower,
            EnemyBalanceProfile.BaseWeaponInterval / Math.Max(0.1f, modifiers.FireRateMultiplier),
            (float)(BaseTargetRange * modifiers.TargetRangeMultiplier),
            ProjectileKinematicsPolicy.NormalizeSpeed(
                (float)(BaseProjectileSpeed * modifiers.ProjectileSpeedMultiplier)),
            1.0f,
            character.PlayableProfile.UltimateIntervalSeconds /
                Math.Max(0.1f, modifiers.FireRateMultiplier *
                    (float)GetExpectedFireRateMultiplier(modifiers)),
            character.PlayableProfile.UltimateTargetCapacity,
            (float)BaseSpawnDistance);
        double result = 0.0;
        foreach (SpellCardDefinition card in SpellCardCatalog.All.Where(item =>
            build.GetRank(item.UnlockUpgradeId) > 0))
        {
            result += ProjectSpellCard(card, attributes);
        }

        return result;
    }

    /// <summary>
    /// 投影一张奥义的每秒伤害等价贡献，公开给同程序集契约测试逐卡核对模拟与策划预算的一致性。
    /// </summary>
    internal static double ProjectSpellCard(
        SpellCardDefinition card,
        SpellCardBaseAttributes attributes) =>
        SpellCardContributionModel.ProjectPerSecond(card, attributes);

    /// <summary>
    /// 用对数压缩伤害后合并移动、索敌、吸附、资质生命和护持槽，形成跨时段可比较的综合准备度。
    /// </summary>
    private static double CalculateReadiness(
        double totalDps,
        RunModifierState modifiers,
        CharacterDefinition character,
        int supportSpellCount)
    {
        double offense = 1.0 + Math.Log(1.0 + Math.Max(0.0, totalDps) /
            (EnemyBalanceProfile.BaseWeaponDamage /
                EnemyBalanceProfile.BaseWeaponInterval));
        double mobility = Math.Pow(modifiers.MoveSpeedMultiplier *
            GetExpectedMoveMultiplier(modifiers) *
            character.PlayableProfile.MoveSpeedMultiplier, 0.45);
        double coverage = Math.Pow(modifiers.TargetRangeMultiplier, 0.25);
        double economy = Math.Pow(modifiers.SpiritAttractionMultiplier *
            modifiers.SpiritYieldMultiplier, 0.18);
        double survival = Math.Pow(character.PlayableProfile.MaxHealth / 6.0, 0.15) *
            (1.0 + supportSpellCount * 0.08);
        return offense * mobility * coverage * economy * survival;
    }

    /// <summary>按自动射击持续有目标的常见战况折算疾息期望射速，正式峰值仍由蓄势状态控制。</summary>
    private static double GetExpectedFireRateMultiplier(RunModifierState modifiers) =>
        modifiers.UsesContinuousFireMomentum
            ? 1.0 + PassiveSpecializationPolicy.ContinuousFireBonus *
                PassiveSpecializationPolicy.ExpectedContinuousFireUptime
            : 1.0;

    /// <summary>按走位与停步混合时长折算凝神期望攻势，避免模拟把条件峰值视为永久收益。</summary>
    private static double GetExpectedAttackMultiplier(RunModifierState modifiers) =>
        modifiers.UsesStationaryFocus
            ? 1.0 + PassiveSpecializationPolicy.StationaryFocusBonus *
                PassiveSpecializationPolicy.ExpectedFocusUptime
            : 1.0;

    /// <summary>合并逐风与流云的独立期望覆盖率，用于横向路线准备度而非正式帧级状态。</summary>
    private static double GetExpectedMoveMultiplier(RunModifierState modifiers)
    {
        double movement = modifiers.UsesMovementMomentum
            ? 1.0 + PassiveSpecializationPolicy.MovementMomentumBonus *
                PassiveSpecializationPolicy.ExpectedMovementUptime
            : 1.0;
        double flow = modifiers.UsesSpiritFlow
            ? 1.0 + PassiveSpecializationPolicy.SpiritFlowBonus *
                PassiveSpecializationPolicy.ExpectedSpiritFlowUptime
            : 1.0;
        return movement * flow;
    }
}
