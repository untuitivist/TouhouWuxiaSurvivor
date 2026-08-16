using Godot;
using TouhouWuxiaSurvivor.Content;
using TouhouWuxiaSurvivor.Gameplay.Balance;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Balance;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Contracts;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Geometry;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 以和内容迁移工具一致的预算公式审计四十六张奥义，防止新增作品形成纵向数值膨胀。
/// </summary>
public partial class SpellCardBudgetContractTest : Node
{
    /// <summary>运行目录规模、类型语义、预算上下界与代表性重招断言，并返回明确退出码。</summary>
    public override void _Ready()
    {
        try
        {
            VerifyCatalogSize();
            VerifyGeometryCoverage();
            VerifyEffectSemantics();
            VerifyContributionPolicy();
            VerifyBudgets();
            VerifyProjectionParity();
            VerifySignatureTechniques();
            GD.Print("Spell card budget contract test passed.");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    /// <summary>锁定范围面积积分、投射可靠度和三类自动触发可用率，防止共享模型内部发生无审计漂移。</summary>
    private static void VerifyContributionPolicy()
    {
        double expectedArea = SpellCardContributionModel.ExpectedTargetDamageMultiplier(
            SpellCardEffectKind.AreaBurst);
        Require(Math.Abs(expectedArea - 0.6333333333333333) <= 0.000000001 &&
            Math.Abs(SpellCardContributionModel.ExpectedTargetDamageMultiplier(
                SpellCardEffectKind.GuardField) - expectedArea) <= 0.000000001 &&
            SpellCardContributionModel.ExpectedTargetDamageMultiplier(
                SpellCardEffectKind.HomingVolley) == 1.0,
            "Area and guard contribution no longer integrate the shared 45% edge curve.");
        Require(SpellCardContributionModel.ActivationAvailability(
                SpellCardActivationKind.Periodic) == 1.0 &&
            SpellCardContributionModel.ActivationAvailability(
                SpellCardActivationKind.Crowd) == 0.9 &&
            SpellCardContributionModel.ActivationAvailability(
                SpellCardActivationKind.OnDamaged) == 0.58,
            "Spell activation availability policy changed without a budget migration.");
        Require(Enum.GetValues<SpellCardEffectKind>().All(effect =>
                SpellCardContributionModel.DeliveryWeight(effect) > 0.0),
            "An effect kind has no positive delivery weight.");
    }

    /// <summary>确认几何是完整的正交数据维度，全部五类均有实现且覆盖多张卡。</summary>
    private static void VerifyGeometryCoverage()
    {
        Require(SpellCardGeometryCatalog.All.Count == 5,
            "The runtime must register all five spell geometry strategies.");
        foreach (SpellCardGeometryKind kind in Enum.GetValues<SpellCardGeometryKind>())
        {
            Require(SpellCardCatalog.All.Count(card => card.GeometryKind == kind) >= 7,
                $"{kind} geometry is not meaningfully represented in the catalog.");
        }
    }

    /// <summary>确认二十个正作包与本体共同提供恰好四十六张不重号奥义。</summary>
    private static void VerifyCatalogSize()
    {
        Require(ContentPackCatalog.All.Count == 20,
            "The official content catalog must contain 20 packs.");
        Require(SpellCardCatalog.All.Count == 51 &&
            SpellCardCatalog.All.Select(card => card.Id).Distinct().Count() == 51,
            "The spell catalog must contain 51 unique cards across base and packs.");
        SpellCardDefinition[] baseCards = SpellCardCatalog.All.Where(
            card => card.SourcePackId == "base").ToArray();
        Require(baseCards.Length == 6 &&
            baseCards.Count(card => SpellCardSlotPolicy.Classify(card) ==
                SpellCardSlotKind.Offensive) == SpellCardSlotPolicy.MaximumOffensiveSlots &&
            baseCards.Count(card => SpellCardSlotPolicy.Classify(card) ==
                SpellCardSlotKind.Support) == SpellCardSlotPolicy.MaximumSupportSlots,
            "The base game must independently provide the complete 4+2 spell loadout.");
        Require(baseCards.Any(card => card.OwnerName == "博丽灵梦") &&
            baseCards.Any(card => card.OwnerName == "雾雨魔理沙"),
            "The base spell loadout must represent both permanent playable characters.");
        Require(ContentPackCatalog.All.All(pack =>
                SpellCardCatalog.All.Count(card => card.SourcePackId == pack.Id) ==
                    (pack.Number == 6 ? 7 : 2)),
            "Every official pack must retain its declared horizontal spell choices.");
    }

    /// <summary>确认四类投射方式使用各自的伤害、目标与护身区间，不再依赖零目标哨兵。</summary>
    private static void VerifyEffectSemantics()
    {
        foreach (SpellCardDefinition card in SpellCardCatalog.All)
        {
            (float damageMin, float damageMax, float targetMin, float targetMax) =
                EffectRanges(card.EffectKind);
            Require(InRange(card.Combat.DamageScale, damageMin, damageMax),
                $"{card.Id} damage scale violates its effect range.");
            Require(InRange(card.Combat.TargetScale, targetMin, targetMax),
                $"{card.Id} target scale violates its effect range.");
            Require(card.EffectKind == SpellCardEffectKind.GuardField
                    ? card.Combat.DefenseScale > 0.0f
                    : card.Combat.DefenseScale == 0.0f,
                $"{card.Id} guard duration does not match its effect type.");
        }
    }

    /// <summary>按命中可靠度、触发可用率和护身收益折算持续贡献，并限制每种效果的公平区间。</summary>
    private static void VerifyBudgets()
    {
        foreach (SpellCardDefinition card in SpellCardCatalog.All)
        {
            double score = SpellCardContributionModel.CalculateBudget(card);
            (double minimum, double maximum) = BudgetRange(card.EffectKind);
            Require(double.IsFinite(score) && InRange(score, minimum, maximum),
                $"{card.Id} budget {score:0.###} is outside {minimum:0.##}..{maximum:0.##}.");
        }
    }

    /// <summary>
    /// 用同一组基础属性逐卡比较契约预算和正式时间线单卡投影，防止四十六张卡在两个调用入口产生口径漂移。
    /// </summary>
    private static void VerifyProjectionParity()
    {
        var attributes = new SpellCardBaseAttributes(
            17.0f, 0.28f, 520.0f, 480.0f, 1.0f, 5.4f, 7, 18.0f);
        double baseContribution = attributes.AttackPower *
            attributes.UltimateTargetCapacity / attributes.UltimateIntervalSeconds;
        foreach (SpellCardDefinition card in SpellCardCatalog.All)
        {
            double contract = SpellCardContributionModel.CalculateBudget(card);
            double projected = BalanceCombatProjector.ProjectSpellCard(card, attributes);
            Require(Math.Abs(projected / baseContribution - contract) <= 0.000000001,
                $"{card.Id} simulation projection diverged from its contract budget.");
        }
    }

    /// <summary>确认高辨识度重招位于本类型伤害顶档，但没有通过旧十倍倍率越过公共预算。</summary>
    private static void VerifySignatureTechniques()
    {
        RequireDamage("th06_flandre_laevatein", 1.55f);
        RequireDamage("marisa_master_spark", 1.55f);
        RequireDamage("th11_utsuho_petaflare", 2.013f);
        RequireDamage("th15_junko_pure_bullet_hell", 2.013f);
        Require(SpellCardCatalog.All.All(card => card.Combat.TargetScale > 0.0f),
            "Every area and guard spell must author an explicit finite target scale.");
    }

    /// <summary>返回四类效果各自的伤害与目标区间，未知枚举立即暴露为测试失败。</summary>
    private static (float, float, float, float) EffectRanges(
        SpellCardEffectKind effect) => effect switch
    {
        SpellCardEffectKind.HomingVolley => (0.65f, 1.0f, 0.75f, 1.15f),
        SpellCardEffectKind.FocusedVolley => (1.1f, 1.55f, 0.45f, 0.6f),
        SpellCardEffectKind.AreaBurst => (1.313f, 2.013f, 0.8f, 1.1f),
        SpellCardEffectKind.GuardField => (0.55f, 0.85f, 0.55f, 0.8f),
        _ => throw new InvalidOperationException($"Unknown effect kind: {effect}"),
    };

    /// <summary>返回各效果在统一贡献公式下的建议预算窗口，容纳触发方式带来的合理离散。</summary>
    private static (double, double) BudgetRange(SpellCardEffectKind effect) => effect switch
    {
        SpellCardEffectKind.HomingVolley => (0.35f, 0.85f),
        SpellCardEffectKind.FocusedVolley => (0.34f, 0.76f),
        SpellCardEffectKind.AreaBurst => (0.29f, 0.68f),
        SpellCardEffectKind.GuardField => (0.16f, 0.27f),
        _ => throw new InvalidOperationException($"Unknown effect kind: {effect}"),
    };

    /// <summary>检查代表卡伤害倍率与策划顶档完全一致，避免机械迁移损失招牌强度。</summary>
    private static void RequireDamage(string id, float expected)
    {
        SpellCardDefinition card = SpellCardCatalog.All.Single(item => item.Id == id);
        Require(Mathf.IsEqualApprox(card.Combat.DamageScale, expected),
            $"{id} must preserve its signature damage tier {expected:0.##}.");
    }

    /// <summary>使用小误差比较 JSON 单精度倍率，避免文本小数与二进制表示差异导致假失败。</summary>
    private static bool InRange(float value, float minimum, float maximum) =>
        value >= minimum - 0.0001f && value <= maximum + 0.0001f;

    /// <summary>使用相同容差审计双精度贡献预算，避免工具与运行时因表示精度产生假失败。</summary>
    private static bool InRange(double value, double minimum, double maximum) =>
        value >= minimum - 0.0001 && value <= maximum + 0.0001;

    /// <summary>将任一策划违约转换为包含具体卡号和预算的测试异常。</summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
