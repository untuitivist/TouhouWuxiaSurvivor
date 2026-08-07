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
    /// 确认首级需要八点灵息、需求单调增长，并能一次正确跨越多个等级。
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
        Require(state.AddExperience(18) == 2 && state.Level == 3 &&
            state.Experience == 0 && state.PendingChoices == 2,
            "Multi-level experience accounting is incorrect.");
        Require(state.ResolveChoice() && state.PendingChoices == 1,
            "Pending choice resolution is incorrect.");
    }

    /// <summary>
    /// 确认所有敌人的灵息处于一至八点，并且目录中最高耐久敌人的奖励不低于最低耐久敌人。
    /// </summary>
    private static void VerifySpiritValues()
    {
        foreach (EnemyDefinition enemy in EnemyCatalog.All)
        {
            Require(SpiritValueCalculator.Calculate(enemy) is >= 1 and <= 8,
                $"Spirit value is outside 1-8: {enemy.DisplayName}");
        }

        EnemyDefinition weakest = EnemyCatalog.All.MinBy(enemy => enemy.MaxHealth)!;
        EnemyDefinition strongest = EnemyCatalog.All.MaxBy(enemy => enemy.MaxHealth)!;
        Require(SpiritValueCalculator.Calculate(strongest) >=
            SpiritValueCalculator.Calculate(weakest),
            "Durable enemies must not award less spirit than opening enemies.");
    }

    /// <summary>
    /// 确认六项基础修炼与两张符卡的 ID、类型、分类和重数契约保持唯一且明确。
    /// </summary>
    private static void VerifyUpgradeCatalog()
    {
        RunUpgradeDefinition[] cultivation = RunUpgradeCatalog.All
            .Where(item => item.Category != RunUpgradeCategory.SpellCard).ToArray();
        RunUpgradeDefinition[] spellCards = RunUpgradeCatalog.All
            .Where(item => item.Category == RunUpgradeCategory.SpellCard).ToArray();
        Require(cultivation.Length == 6 && spellCards.Length == 2,
            "The pool must contain six cultivations and two spell cards.");
        Require(RunUpgradeCatalog.All.Select(item => item.Id).Distinct().Count() == 8 &&
            RunUpgradeCatalog.All.Select(item => item.Kind).Distinct().Count() == 8,
            "Upgrade IDs and kinds must be unique.");
        Require(cultivation.All(item => item.MaxRank == 5) &&
            spellCards.All(item => item.MaxRank == 1 && item.Requirement is not null),
            "Cultivations must use five ranks and spell cards one required rank.");
    }

    /// <summary>
    /// 各应用一重升级并核对六项倍率，再确认满重定义拒绝第六次应用。
    /// </summary>
    private static void VerifyBuildAndModifiers()
    {
        var build = new RunBuildState();
        foreach (RunUpgradeDefinition definition in RunUpgradeCatalog.All.Where(
            item => item.Category != RunUpgradeCategory.SpellCard))
        {
            Require(build.Apply(definition), $"Upgrade could not be applied: {definition.Id}");
        }

        var modifiers = new RunModifierState();
        modifiers.Refresh(build);
        Require(modifiers.DamageBonus == 1 &&
            Mathf.IsEqualApprox(modifiers.FireRateMultiplier, 1.12f) &&
            Mathf.IsEqualApprox(modifiers.MoveSpeedMultiplier, 1.08f) &&
            Mathf.IsEqualApprox(modifiers.TargetRangeMultiplier, 1.10f) &&
            Mathf.IsEqualApprox(modifiers.ProjectileSpeedMultiplier, 1.12f) &&
            Mathf.IsEqualApprox(modifiers.SpiritAttractionMultiplier, 1.25f),
            "First-rank runtime modifiers are incorrect.");

        RunUpgradeDefinition damage = RunUpgradeCatalog.All[0];
        for (int rank = 1; rank < damage.MaxRank; rank++)
        {
            Require(build.Apply(damage), "Damage upgrade did not reach rank five.");
        }

        Require(!build.Apply(damage), "Upgrade exceeded its five-rank maximum.");

        modifiers.ConfigureBase(2, 1.04f, 1.16f);
        modifiers.Refresh(build);
        Require(modifiers.DamageBonus == 7 &&
            Mathf.IsEqualApprox(modifiers.MoveSpeedMultiplier, 1.04f * 1.08f) &&
            Mathf.IsEqualApprox(modifiers.SpiritAttractionMultiplier, 1.16f * 1.25f),
            "Permanent and in-run modifiers did not compose from stable bases.");
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
