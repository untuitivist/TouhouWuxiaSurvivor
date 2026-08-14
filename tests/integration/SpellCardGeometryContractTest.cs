using Godot;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Geometry;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 审计四十六张卡的显式几何覆盖、策略注册和空间差异，防止内容退化回单一追踪或范围模板。
/// </summary>
public partial class SpellCardGeometryContractTest : Node
{
    /// <summary>运行内容覆盖与五种几何规划断言，并以明确退出码交给自动化环境。</summary>
    public override void _Ready()
    {
        try
        {
            VerifyCatalogCoverage();
            VerifyDistinctPlans();
            VerifyFocusedBudget();
            GD.Print("Spell card geometry contract test passed.");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    /// <summary>确认每张卡都解析出已注册机制，五种几何均跨多个内容包且没有孤立样本。</summary>
    private static void VerifyCatalogCoverage()
    {
        Require(SpellCardCatalog.All.Count == 46,
            "Geometry audit requires the complete 46-card catalog.");
        Require(SpellCardGeometryCatalog.All.Count ==
            Enum.GetValues<SpellCardGeometryKind>().Length,
            "Every geometry enum must own one executable strategy.");
        foreach (SpellCardGeometryKind kind in Enum.GetValues<SpellCardGeometryKind>())
        {
            SpellCardDefinition[] cards = SpellCardCatalog.All
                .Where(card => card.GeometryKind == kind)
                .ToArray();
            Require(cards.Length >= 7,
                $"{kind} needs at least seven cards instead of a token assignment.");
            Require(cards.Select(card => card.SourcePackId).Distinct().Count() >= 6,
                $"{kind} must be distributed across multiple horizontal packs.");
        }
    }

    /// <summary>以同一候选集运行全部策略，确认选敌、起手或弯曲轨迹至少有一项真实不同。</summary>
    private static void VerifyDistinctPlans()
    {
        var request = new SpellCardGeometryRequest(
            Vector2.Zero,
            [new(30, 0), new(55, 14), new(22, 48), new(-38, 8), new(70, -35)],
            3,
            100.0f,
            12.0f,
            false);
        string[] signatures = SpellCardGeometryCatalog.All
            .Select(strategy => Signature(strategy.CreatePlan(request)))
            .ToArray();
        Require(signatures.Distinct(StringComparer.Ordinal).Count() == signatures.Length,
            "Every geometry strategy must produce a distinct spatial plan.");
        Require(SpellCardGeometryCatalog.All.All(strategy =>
                strategy.CreatePlan(request).ImpactTargets.Count == 3),
            "Geometry must not change the shared target budget.");
    }

    /// <summary>确认集中投射仍只命中一个落点，但生成完整弹丸数，防止新几何暗增伤害目标。</summary>
    private static void VerifyFocusedBudget()
    {
        var request = new SpellCardGeometryRequest(
            Vector2.Zero,
            [new(32, 0), new(48, 18), new(60, -12)],
            4,
            100.0f,
            10.0f,
            true);
        foreach (ISpellCardGeometryStrategy strategy in SpellCardGeometryCatalog.All)
        {
            SpellCardGeometryPlan plan = strategy.CreatePlan(request);
            Require(plan.ImpactTargets.Count == 1 && plan.Projectiles.Count == 4,
                $"{strategy.Kind} changed focused volley impact or projectile budget.");
        }
    }

    /// <summary>把规划的目标、起点和曲率压成稳定签名，用于比较实际输出而非类名。</summary>
    private static string Signature(SpellCardGeometryPlan plan) => string.Join(";",
        plan.ImpactTargets.Select(position => $"I{position.X:0.#},{position.Y:0.#}")
        .Concat(plan.Projectiles.Select(projectile =>
            $"P{projectile.SpawnPosition.X:0.#},{projectile.SpawnPosition.Y:0.#}," +
            $"{projectile.Curvature:0.##}")));

    /// <summary>将策划契约失败转换为包含具体机制的测试异常。</summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
