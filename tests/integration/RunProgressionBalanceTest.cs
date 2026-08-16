using Godot;
using TouhouWuxiaSurvivor.Actors.Enemies;
using TouhouWuxiaSurvivor.Gameplay.Progression.Definitions;
using TouhouWuxiaSurvivor.Gameplay.Progression.Runtime;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 验证局内经验曲线、灵息价值、升级目录、重数上限和运行时倍率满足策划契约。
/// </summary>
public partial class RunProgressionBalanceTest : Node
{
    /// <summary>
    /// 依次执行纯数据断言，并以明确退出码报告任何数值或目录回归。
    /// </summary>
    public override void _Ready()
    {
        try
        {
            VerifyLevelCurve();
            VerifySpiritValues();
            VerifyUpgradeCatalog();
            VerifyBuildAndModifiers();
            VerifyEndlessCultivation();
            VerifyOffers();
            GD.Print("Run progression balance test passed.");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    /// <summary>
    /// 确认首级需要八点灵息、后续需求单调增长，并能一次正确跨越多个等级。
    /// </summary>
    private static void VerifyLevelCurve()
    {
        Require(RunLevelCurve.GetRequiredExperience(1) == 8,
            "Opening level must require exactly eight spirit XP.");
        for (int level = 1; level < 30; level++)
        {
            Require(RunLevelCurve.GetRequiredExperience(level + 1) >=
                RunLevelCurve.GetRequiredExperience(level),
                "Experience requirements must never decrease.");
        }

        var state = new RunProgressionState();
        Require(state.AddExperience(21) == 2 && state.Level == 3 &&
            state.Experience == 0 && state.PendingChoices == 2,
            "Multi-level experience accounting is incorrect.");
        Require(state.ResolveChoice() && state.PendingChoices == 1,
            "Pending choice resolution is incorrect.");
    }

    /// <summary>
    /// 确认普通敌人的灵息处于安全范围，并且更耐久敌人的奖励不会反向降低。
    /// </summary>
    private static void VerifySpiritValues()
    {
        foreach (EnemyDefinition enemy in EnemyCatalog.All)
        {
            Require(SpiritValueCalculator.Calculate(enemy) is >= 1 and <= 16,
                $"Spirit value is outside 1-16: {enemy.DisplayName}");
        }

        EnemyDefinition weakest = EnemyCatalog.All.MinBy(enemy => enemy.MaxHealth)!;
        EnemyDefinition strongest = EnemyCatalog.All.MaxBy(enemy => enemy.MaxHealth)!;
        Require(SpiritValueCalculator.Calculate(strongest) >=
            SpiritValueCalculator.Calculate(weakest),
            "Durable enemies must not award less spirit than opening enemies.");
    }

    /// <summary>
    /// 确认有限、无尽与全作符卡三类升级的 ID、分类和重数契约保持唯一且明确。
    /// </summary>
    private static void VerifyUpgradeCatalog()
    {
        RunUpgradeDefinition[] finite = RunUpgradeCatalog.All.Where(item =>
            item.Category != RunUpgradeCategory.SpellCard && !item.IsRepeatable).ToArray();
        RunUpgradeDefinition[] endless = RunUpgradeCatalog.All.Where(item =>
            item.IsRepeatable).ToArray();
        RunUpgradeDefinition[] spellCards = RunUpgradeCatalog.All
            .Where(item => item.Category == RunUpgradeCategory.SpellCard).ToArray();
        Require(finite.Length == 6 && endless.Length == 6 && spellCards.Length == 51,
            "The pool does not contain the expected finite, endless, and spell upgrades.");
        Require(RunUpgradeCatalog.All.Select(item => item.Id).Distinct().Count() ==
                RunUpgradeCatalog.All.Count &&
            finite.Concat(endless).Select(item => item.Kind).Distinct().Count() == 12,
            "Upgrade IDs or cultivation effect kinds are duplicated.");
        Require(finite.Select(item => item.MaxRank).SequenceEqual([4, 4, 3, 3, 3, 3]) &&
            endless.All(item => item.MaxRank == int.MaxValue && item.Requirement is not null) &&
            spellCards.All(item => item.MaxRank == 2 && item.Requirement is not null &&
                item.SpellCardId is not null) &&
            spellCards.Count(item => item.RequiredContentPack is null) == 6,
            "Upgrade rank, requirement, or content ownership contract is incorrect.");
    }

    /// <summary>
    /// 先确认无尽修行受各自满重前置约束，再应用六项基础升级核对强反馈首重和满重上限。
    /// </summary>
    private static void VerifyBuildAndModifiers()
    {
        var build = new RunBuildState();
        RunUpgradeDefinition[] endless = RunUpgradeCatalog.All.Where(
            item => item.IsRepeatable).ToArray();
        Require(endless.All(definition =>
                !build.CanUpgrade(definition) && !build.Apply(definition)),
            "Endless cultivation ignored its finite-rank prerequisite.");

        foreach (RunUpgradeDefinition definition in RunUpgradeCatalog.All.Where(item =>
            item.Category != RunUpgradeCategory.SpellCard && !item.IsRepeatable))
        {
            Require(build.Apply(definition), $"Upgrade could not be applied: {definition.Id}");
        }

        var modifiers = new RunModifierState();
        modifiers.Refresh(build);
        Require(modifiers.DamageBonus == 0 &&
            Mathf.IsEqualApprox(modifiers.AttackPowerMultiplier, 1.35f) &&
            Mathf.IsEqualApprox(modifiers.FireRateMultiplier, 1.18f) &&
            Mathf.IsEqualApprox(modifiers.MoveSpeedMultiplier, 1.15f) &&
            Mathf.IsEqualApprox(modifiers.TargetRangeMultiplier, 1.25f) &&
            Mathf.IsEqualApprox(modifiers.ProjectileSpeedMultiplier, 1.12f) &&
            Mathf.IsEqualApprox(modifiers.SpiritAttractionMultiplier, 1.50f) &&
            Mathf.IsEqualApprox(modifiers.SpiritYieldMultiplier, 1.10f) &&
            modifiers.OrdinaryProjectileBonus == 1 &&
            modifiers.BarrageProjectileBonus == 4,
            "First-rank runtime modifiers are incorrect.");

        RunUpgradeDefinition damage = RunUpgradeCatalog.All[0];
        for (int rank = 1; rank < damage.MaxRank; rank++)
        {
            Require(build.Apply(damage), "Damage upgrade did not reach its maximum rank.");
        }

        Require(!build.Apply(damage), "Upgrade exceeded its authored maximum.");

        modifiers.ConfigureBase(2, 1.04f, 1.16f);
        modifiers.Refresh(build);
        Require(modifiers.DamageBonus == 2 &&
            Mathf.IsEqualApprox(modifiers.AttackPowerMultiplier, 2.40f) &&
            Mathf.IsEqualApprox(modifiers.MoveSpeedMultiplier, 1.04f * 1.15f) &&
            Mathf.IsEqualApprox(modifiers.SpiritAttractionMultiplier, 1.16f * 1.50f),
            "Permanent and in-run modifiers did not compose from stable bases.");
    }

    /// <summary>
    /// 达到五重前置后反复修行并确认数值持续增长，证明有限武学全满后升级仍有意义。
    /// </summary>
    private static void VerifyEndlessCultivation()
    {
        var build = new RunBuildState();
        RunUpgradeDefinition damage = RunUpgradeCatalog.GetRequiredByKind(
            RunUpgradeKind.NeedleDamage);
        RunUpgradeDefinition endless = RunUpgradeCatalog.GetRequiredByKind(
            RunUpgradeKind.EndlessDamage);
        for (int rank = 0; rank < damage.MaxRank; rank++)
        {
            Require(build.Apply(damage), "Could not reach endless damage prerequisite.");
        }

        var modifiers = new RunModifierState();
        modifiers.Refresh(build);
        float previous = modifiers.AttackPowerMultiplier;
        for (int rank = 0; rank < 1000; rank++)
        {
            Require(build.Apply(endless), "Repeatable cultivation rejected a valid rank.");
        }

        modifiers.Refresh(build);
        Require(modifiers.AttackPowerMultiplier > previous && build.CanUpgrade(endless),
            "Endless cultivation stopped growing or became unavailable.");
    }

    /// <summary>
    /// 使用固定随机种子确认三选一互不重复，并排除已经满重的升级。
    /// </summary>
    private static void VerifyOffers()
    {
        var build = new RunBuildState();
        RunUpgradeDefinition maxed = RunUpgradeCatalog.All[0];
        for (int rank = 0; rank < maxed.MaxRank; rank++)
        {
            build.Apply(maxed);
        }

        var random = new RandomNumberGenerator { Seed = 20260730 };
        IReadOnlyList<RunUpgradeDefinition> offer =
            RunUpgradeCatalog.CreateOffer(random, build, 3);
        Require(offer.Count == 3 && offer.Distinct().Count() == 3 && !offer.Contains(maxed),
            "Upgrade offer is duplicated or contains a maxed definition.");
    }

    /// <summary>
    /// 将策划契约失败转换为包含具体原因的测试异常。
    /// </summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
