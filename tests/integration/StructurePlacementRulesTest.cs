using Godot;
using TouhouWuxiaSurvivor.Content;
using TouhouWuxiaSurvivor.World.Biomes;
using TouhouWuxiaSurvivor.World.Structures;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 验证结构目录、随机扩散网格、负坐标确定性、窗口无关性和实例硬间距。
/// </summary>
public partial class StructurePlacementRulesTest : Node
{
    private const ulong Seed = 0x20260812UL;

    /// <summary>
    /// 运行全部纯数据断言并以进程退出码向测试脚本报告结果。
    /// </summary>
    public override void _Ready()
    {
        try
        {
            VerifyCatalog();
            VerifyDeterminismAndQueryWindows();
            VerifyHardSeparation();
            GD.Print("Structure placement rules test passed.");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    /// <summary>
    /// 要求每个枚举拥有唯一完整定义，并且不再退回旧统一 96 格配置。
    /// </summary>
    private static void VerifyCatalog()
    {
        Require(StructureCatalog.All.Count == Enum.GetValues<StructureId>().Length,
            "Every structure id must have exactly one definition.");
        Require(StructureCatalog.All.Select(item => item.DefinitionId).Distinct().Count() ==
            StructureCatalog.All.Count, "Structure definition ids must be unique.");
        Require(StructureCatalog.All.Where(item => !item.IsSpawnStructure)
            .Select(item => (item.Placement.Spacing, item.Placement.Separation,
                item.Placement.Chance)).Distinct().Count() >= 4,
            "Structure definitions still share one global placement lottery.");
        Require(StructureCatalog.All.All(item => item.Placement.Spacing >
            item.Placement.Separation && item.Placement.FootprintRadius > 0),
            "A structure placement profile is invalid.");
    }

    /// <summary>
    /// 比较重复查询及大窗口筛选结果，覆盖负坐标向下取整和加载顺序无关契约。
    /// </summary>
    private static void VerifyDeterminismAndQueryWindows()
    {
        var locator = new StructureLocator(Seed,
            new BiomeSelector(Seed, ContentPackSelection.BaseOnly));
        StructurePlacement[] first = locator.FindInBounds(-2400, -2300, 2350, 2450).ToArray();
        StructurePlacement[] second = locator.FindInBounds(-2400, -2300, 2350, 2450).ToArray();
        Require(first.SequenceEqual(second), "Repeated structure queries are not deterministic.");
        Require(first.Select(item => item.InstanceId).Distinct().Count() == first.Length,
            "Structure instance ids are not unique in the sampled world.");

        StructurePlacement[] inner = locator.FindInBounds(-900, -850, 870, 930).ToArray();
        StructurePlacement[] filtered = first.Where(item => item.X is >= -900 and <= 870 &&
            item.Y is >= -850 and <= 930).ToArray();
        Require(inner.SequenceEqual(filtered),
            "Structure result changed when the query window changed.");
        Require(first.Any(item => item.X < 0) && first.Any(item => item.Y < 0),
            "Negative-coordinate structure coverage is missing.");
    }

    /// <summary>
    /// 逐对核对同类 separation 与异类 footprint 冲突距离，防止结构重叠或成团。
    /// </summary>
    private static void VerifyHardSeparation()
    {
        var locator = new StructureLocator(Seed,
            new BiomeSelector(Seed, ContentPackSelection.BaseOnly));
        StructurePlacement[] placements = locator.FindInBounds(-3000, -3000, 3000, 3000).ToArray();
        Require(placements.Length >= 8, "Sample world produced too few base structures.");
        for (int i = 0; i < placements.Length; i++)
        {
            for (int j = i + 1; j < placements.Length; j++)
            {
                StructurePlacement left = placements[i];
                StructurePlacement right = placements[j];
                double distance = Distance(left, right);
                if (left.Id == right.Id)
                {
                    int required = StructureCatalog.GetRequired(left.Id).Placement.Separation;
                    Require(distance >= required,
                        $"Same structure violated separation: {left.Id}/{distance:F1}.");
                }

                int footprintDistance = left.FootprintRadius + right.FootprintRadius + 3;
                Require(distance >= footprintDistance,
                    $"Structure footprints overlap: {left.Id}/{right.Id}/{distance:F1}.");
            }
        }
    }

    /// <summary>
    /// 以双精度计算两个世界锚点距离，避免长坐标平方溢出。
    /// </summary>
    private static double Distance(StructurePlacement left, StructurePlacement right)
    {
        double dx = (double)left.X - right.X;
        double dy = (double)left.Y - right.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>
    /// 将结构契约失败转换为包含上下文的测试异常。
    /// </summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
