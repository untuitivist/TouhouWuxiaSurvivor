using Godot;
using TouhouWuxiaSurvivor.Content;
using TouhouWuxiaSurvivor.Gameplay.Progression.Definitions;
using TouhouWuxiaSurvivor.Gameplay.Progression.Runtime;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;
using TouhouWuxiaSurvivor.Ui.Progression;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 验证横向亲和三选一的固定种子、探索位、内容平行、前置互斥、特化和符卡界面契约。
/// </summary>
public partial class RunAffinityOfferTest : Node
{
    private readonly RunOfferGenerator _generator = new();

    /// <summary>
    /// 依次执行纯算法与真实界面断言，并用明确退出码报告任一构筑规则回归。
    /// </summary>
    public override void _Ready()
    {
        try
        {
            VerifyCatalogMetadata();
            VerifyDeterministicOffer();
            VerifyAffinityBiasAndExploration();
            VerifyMultiplePrerequisitesAndExclusion();
            VerifySpecialization();
            VerifyContentParallelism();
            VerifySpellCardUi();
            GD.Print("Run affinity offer test passed.");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GetTree().Paused = false;
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    /// <summary>
    /// 确认六项基础、六项无尽和五十一张符卡都有标签，且基础修行各有一组双分支特化。
    /// </summary>
    private static void VerifyCatalogMetadata()
    {
        RunUpgradeDefinition[] finite = RunUpgradeCatalog.All.Where(item =>
            item.Category != RunUpgradeCategory.SpellCard && !item.IsRepeatable).ToArray();
        RunUpgradeDefinition[] endless = RunUpgradeCatalog.All.Where(
            item => item.IsRepeatable).ToArray();
        RunUpgradeDefinition[] spells = RunUpgradeCatalog.All.Where(
            item => item.Category == RunUpgradeCategory.SpellCard).ToArray();
        Require(finite.Length == 6 && endless.Length == 6 && spells.Length == 51,
            "Unexpected upgrade category counts.");
        Require(RunUpgradeCatalog.All.All(item =>
                item.Affinities.Count is >= 1 and <= 2 &&
                Math.Abs(item.BaseOfferWeight - 1.0f) < 0.001f),
            "Every horizontal option must use one or two tags and the shared base weight.");
        Require(finite.All(item => item.Specializations.Count == 2) &&
            endless.Concat(spells).All(item => item.Specializations.Count == 0),
            "Specialization metadata is not limited to two equal-rank base branches.");
    }

    /// <summary>
    /// 使用两个相同种子的独立随机源确认候选身份、次序和探索标记均完全可复现。
    /// </summary>
    private void VerifyDeterministicOffer()
    {
        var build = new RunBuildState();
        build.Apply(RunUpgradeCatalog.FindById("needle_damage")!);
        var firstRandom = new RandomNumberGenerator { Seed = 20260812 };
        var secondRandom = new RandomNumberGenerator { Seed = 20260812 };
        string[] first = _generator.CreateOffer(
            firstRandom, build, ContentPackSelection.BaseOnly, 3, 3)
            .Select(DescribeChoice).ToArray();
        string[] second = _generator.CreateOffer(
            secondRandom, build, ContentPackSelection.BaseOnly, 3, 3)
            .Select(DescribeChoice).ToArray();
        Require(first.SequenceEqual(second),
            "A fixed seed did not reproduce the complete offer.");
    }

    /// <summary>
    /// 统计大量独立升级轮次，确认已有路线显著增权但不垄断，同时每轮恰有一个另辟路线候选。
    /// </summary>
    private void VerifyAffinityBiasAndExploration()
    {
        const int trials = 600;
        var neutral = new RunBuildState();
        var focused = new RunBuildState();
        RunUpgradeDefinition needle = RunUpgradeCatalog.FindById("needle_damage")!;
        for (int rank = 0; rank < 3; rank++)
        {
            focused.Apply(needle);
        }

        var dominant = new HashSet<RunUpgradeAffinity>
        {
            RunUpgradeAffinity.Force,
            RunUpgradeAffinity.Precision,
        };
        int neutralAlignedChoices = 0;
        int focusedAlignedChoices = 0;
        for (ulong seed = 1; seed <= trials; seed++)
        {
            var neutralRandom = new RandomNumberGenerator { Seed = seed };
            var focusedRandom = new RandomNumberGenerator { Seed = seed };
            IReadOnlyList<RunUpgradeChoice> neutralOffer = _generator.CreateOffer(
                neutralRandom, neutral, ContentPackSelection.BaseOnly, 1, 3);
            IReadOnlyList<RunUpgradeChoice> focusedOffer = _generator.CreateOffer(
                focusedRandom, focused, ContentPackSelection.BaseOnly, 1, 3);
            neutralAlignedChoices += neutralOffer.Count(item =>
                item.Affinities.Any(dominant.Contains));
            focusedAlignedChoices += focusedOffer.Count(item =>
                item.Affinities.Any(dominant.Contains));
            Require(focusedOffer.Count(item =>
                    item.OfferRole == RunUpgradeOfferRole.Momentum) == 1 &&
                focusedOffer.Count(item =>
                    item.OfferRole == RunUpgradeOfferRole.Complement) == 1 &&
                focusedOffer.Count(item => item.IsExploration) == 1,
                "An established build did not receive one momentum, complement, and exploration option.");
            RunUpgradeChoice momentum = focusedOffer.Single(item =>
                item.OfferRole == RunUpgradeOfferRole.Momentum);
            RunUpgradeChoice complement = focusedOffer.Single(item =>
                item.OfferRole == RunUpgradeOfferRole.Complement);
            RunUpgradeChoice exploration = focusedOffer.Single(item => item.IsExploration);
            Require(focused.GetRank(momentum.Definition.Id) > 0 &&
                complement.Affinities.Any(dominant.Contains) &&
                focused.GetRank(exploration.Definition.Id) == 0,
                "Continuation, formation, or supplement role did not match its build duty.");
        }

        Require(focusedAlignedChoices > neutralAlignedChoices + trials / 8,
            "Affinity did not significantly improve its complete route's appearance rate.");
        Require(focusedAlignedChoices < trials * 3,
            "Affinity occupied every choice and removed the alternate route.");
        IReadOnlyList<RunUpgradeChoice> opening = _generator.CreateOffer(
            new RandomNumberGenerator { Seed = 11 }, neutral,
            ContentPackSelection.BaseOnly, 1, 3);
        Require(opening.Count(choice => choice.IsExploration) == 1 &&
            opening.Count(choice => choice.OfferRole == RunUpgradeOfferRole.Opportunity) == 2,
            "A neutral opening did not preserve one supplement and two open choices.");
    }

    /// <summary>
    /// 使用合成定义确认多前置必须全部满足，且已选择项目会阻止显式互斥项目。
    /// </summary>
    private static void VerifyMultiplePrerequisitesAndExclusion()
    {
        var first = CreateSynthetic("test_first");
        var second = CreateSynthetic("test_second", maxRank: 2);
        var gated = new RunUpgradeDefinition(
            "test_gated", "双门", RunUpgradeKind.NeedleDamage,
            RunUpgradeCategory.MartialArt, 1, "测试",
            requirements:
            [
                new RunUpgradeRequirement(first.Id, 1),
                new RunUpgradeRequirement(second.Id, 2),
            ]);
        var excluded = new RunUpgradeDefinition(
            "test_excluded", "互斥", RunUpgradeKind.FireRate,
            RunUpgradeCategory.InnerArt, 1, "测试", excludedUpgradeIds: [first.Id]);
        var build = new RunBuildState();
        Require(!build.CanUpgrade(gated), "Multiple prerequisites were ignored.");
        build.Apply(first);
        build.Apply(second);
        Require(!build.CanUpgrade(gated) && build.CanUpgrade(excluded),
            "Partial prerequisites passed or the temporary no-exclusion rule was ignored.");
        build.Apply(second);
        Require(build.CanUpgrade(gated), "All prerequisite ranks did not unlock the option.");
    }

    /// <summary>
    /// 确认基础三重与境界八共同解锁特化，选定一支后另一支永久互斥且效果进入倍率投影。
    /// </summary>
    private static void VerifySpecialization()
    {
        var build = new RunBuildState();
        RunUpgradeDefinition needle = RunUpgradeCatalog.FindById("needle_damage")!;
        for (int rank = 0; rank < 2; rank++)
        {
            build.Apply(needle);
        }

        RunUpgradeSpecialization first = needle.Specializations[0];
        RunUpgradeSpecialization second = needle.Specializations[1];
        Require(!build.CanSpecialize(needle, first, 3) &&
            build.CanSpecialize(needle, first, 4),
            "Specialization level or rank gate is incorrect.");
        Require(build.ApplySpecialization(needle, first, 4) &&
            build.ApplySpecialization(needle, second, 4),
            "Temporarily parallel specializations blocked each other.");
        var modifiers = new RunModifierState();
        modifiers.Refresh(build);
        Require(modifiers.DamageBonus == 0 &&
            Mathf.IsEqualApprox(modifiers.AttackPowerMultiplier, 1.50f) &&
            modifiers.ProjectilePierceCount == 1 && modifiers.ExtraProjectiles == 2,
            "Chosen specialization did not enter the runtime modifier projection.");
    }

    /// <summary>
    /// 选取来自不同作品但标签相同的符卡，确认来源包不会改变相同构筑状态下的候选权重。
    /// </summary>
    private static void VerifyContentParallelism()
    {
        var crossPackPair = RunUpgradeCatalog.All
            .Where(item => item.RequiredContentPack is not null)
            .GroupBy(item => string.Join(',', item.Affinities))
            .Select(group => group.GroupBy(item => item.RequiredContentPack).Select(
                pack => pack.First()).Take(2).ToArray())
            .First(pair => pair.Length == 2);
        var build = new RunBuildState();
        build.Apply(RunUpgradeCatalog.FindById("needle_damage")!);
        double firstWeight = RunOfferGenerator.CalculateWeight(
            build, new RunUpgradeChoice(crossPackPair[0]));
        double secondWeight = RunOfferGenerator.CalculateWeight(
            build, new RunUpgradeChoice(crossPackPair[1]));
        Require(Math.Abs(firstWeight - secondWeight) < 0.0001,
            "Content-pack identity changed otherwise identical offer weight.");

        var matureBuild = new RunBuildState();
        foreach (RunUpgradeDefinition definition in RunUpgradeCatalog.All.Where(item =>
            item.Category != RunUpgradeCategory.SpellCard && !item.IsRepeatable))
        {
            while (matureBuild.CanUpgrade(definition))
            {
                matureBuild.Apply(definition);
            }
        }

        var allContent = new ContentPackSelection(
            ContentPackCatalog.All.Select(pack => pack.Id));
        double baseSpellPool = RunOfferWeightTable.GetSpellPoolWeight(
            matureBuild, CreateAvailableChoices(matureBuild, ContentPackSelection.BaseOnly));
        double allSpellPool = RunOfferWeightTable.GetSpellPoolWeight(
            matureBuild, CreateAvailableChoices(matureBuild, allContent));
        Require(Math.Abs(baseSpellPool - allSpellPool) < 0.0001 &&
            baseSpellPool > 0.0,
            "Enabling more content changed the total spell-card offer weight.");
    }

    /// <summary>
    /// 以正式内容、前置和槽位规则建立候选视图，供类别权重测试复用而不暴露生成器内部集合。
    /// </summary>
    private static IReadOnlyList<RunUpgradeChoice> CreateAvailableChoices(
        RunBuildState build,
        ContentPackSelection content) => RunUpgradeCatalog.All.Where(definition =>
            (definition.RequiredContentPack is null ||
                content.IsEnabled(definition.RequiredContentPack)) &&
            build.CanUpgrade(definition))
            .Select(definition => new RunUpgradeChoice(definition))
            .ToArray();

    /// <summary>
    /// 将真实符卡候选送入正式升级面板，确认界面按稳定 ID 查重数并能正常打开与关闭。
    /// </summary>
    private void VerifySpellCardUi()
    {
        RunUpgradeDefinition spell = RunUpgradeCatalog.All.First(
            item => item.Category == RunUpgradeCategory.SpellCard);
        LevelUpOverlay overlay = GD.Load<PackedScene>(
            "res://src/ui/progression/LevelUpOverlay.tscn").Instantiate<LevelUpOverlay>();
        AddChild(overlay);
        overlay.Present([new RunUpgradeChoice(spell)], new RunBuildState(), 2);
        RunUpgradeChoiceCard rendered = overlay.GetNode<RunUpgradeChoiceCard>(
            "Root/Panel/Padding/Layout/Choices/Choice0");
        SpellCardDefinition card = SpellCardCatalog.FindById(spell.SpellCardId!) ??
            throw new InvalidOperationException($"Unknown spell card: {spell.SpellCardId}.");
        Require(overlay.IsOpen && rendered.DisplayTitle.Contains(
                card.ShortName, StringComparison.Ordinal),
            "Spell-card choice did not render through the real level-up UI.");
        overlay.CloseAndRestore();
        overlay.Free();
    }

    /// <summary>
    /// 建立仅用于前置和互斥测试的有限定义，避免测试依赖正式目录额外身份。
    /// </summary>
    private static RunUpgradeDefinition CreateSynthetic(string id, int maxRank = 1) => new(
        id, id, RunUpgradeKind.NeedleDamage,
        RunUpgradeCategory.MartialArt, maxRank, "测试");

    /// <summary>
    /// 将候选身份与探索标记合并为稳定字符串，便于固定种子逐项比较。
    /// </summary>
    private static string DescribeChoice(RunUpgradeChoice choice) =>
        $"{choice.Id}:{choice.OfferRole}";

    /// <summary>
    /// 将任一策划契约失败转换为包含具体原因的测试异常。
    /// </summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
