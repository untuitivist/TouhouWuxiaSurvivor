using Godot;
using TouhouWuxiaSurvivor.Content;
using TouhouWuxiaSurvivor.World.Biomes;
using TouhouWuxiaSurvivor.World.Coordinates;
using TouhouWuxiaSurvivor.World.Generation;
using TouhouWuxiaSurvivor.World.Structures;
using TouhouWuxiaSurvivor.World.StructureTemplates;
using TouhouWuxiaSurvivor.World.Tiles;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 验证十六类结构拥有不同分层轮廓、无整块方形填色，并可连续跨区块压印。
/// </summary>
public partial class StructureTemplateSemanticsTest : Node
{
    private const ulong Seed = 0x7E4D2UL;

    /// <summary>
    /// 运行模板签名、角色层和实际世界边界测试，再以退出码报告结果。
    /// </summary>
    public override void _Ready()
    {
        try
        {
            VerifyTemplateDiversity();
            VerifyCrossChunkStamp();
            GD.Print("Structure template semantics test passed.");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    /// <summary>
    /// 计算每类模板的完整语义签名，拒绝重复轮廓、单层图形和大正方形色块。
    /// </summary>
    private static void VerifyTemplateDiversity()
    {
        var signatures = new HashSet<string>(StringComparer.Ordinal);
        foreach (StructureTemplateKind kind in Enum.GetValues<StructureTemplateKind>())
        {
            var roles = new List<StructureTileRole>();
            for (int y = -16; y <= 16; y++)
            {
                for (int x = -16; x <= 16; x++)
                {
                    roles.Add(StructureTemplateSampler.Sample(kind, x, y, 16, 0, 0));
                }
            }

            int occupied = roles.Count(role => role != StructureTileRole.None);
            Require(occupied > 30 && occupied < roles.Count * 0.82,
                $"Template is empty or a large square block: {kind}/{occupied}.");
            Require(roles.Where(role => role != StructureTileRole.None).Distinct().Count() >= 3,
                $"Template does not contain three semantic layers: {kind}.");
            signatures.Add(string.Concat(roles.Select(role => (char)('A' + (int)role))));
        }

        Require(signatures.Count == Enum.GetValues<StructureTemplateKind>().Length,
            "Two semantic structure templates have identical footprints.");
    }

    /// <summary>
    /// 找到实际跨区块结构并逐格核对相邻区块结果，证明压印不依赖锚点所属区块。
    /// </summary>
    private static void VerifyCrossChunkStamp()
    {
        var generator = new WorldGenerator(Seed, ContentPackSelection.BaseOnly);
        StructurePlacement placement = generator.StructureLocations
            .FindInBounds(-1800, -1800, 1800, 1800)
            .First(item => CrossesChunkBoundary(item));
        StructureDefinition definition = StructureCatalog.GetRequired(placement.Id);
        for (int dy = -placement.FootprintRadius; dy <= placement.FootprintRadius; dy++)
        {
            for (int dx = -placement.FootprintRadius; dx <= placement.FootprintRadius; dx++)
            {
                StructureTileRole role = StructureTemplateSampler.Sample(
                    definition.Template, dx, dy, placement.FootprintRadius,
                    placement.QuarterTurns, placement.Variant);
                if (!StructureTilePalette.TryResolve(definition, role, out TileId expected))
                {
                    continue;
                }

                long worldX = placement.X + dx;
                long worldY = placement.Y + dy;
                var coordinate = new ChunkCoordinate(
                    GridMath.FloorDiv(worldX, WorldMetrics.ChunkTiles),
                    GridMath.FloorDiv(worldY, WorldMetrics.ChunkTiles));
                GeneratedChunk chunk = generator.Generate(coordinate);
                int localX = (int)GridMath.PositiveMod(worldX, WorldMetrics.ChunkTiles);
                int localY = (int)GridMath.PositiveMod(worldY, WorldMetrics.ChunkTiles);
                Require(chunk.Get(localX, localY) == expected,
                    $"Cross-chunk stamp broke at {worldX},{worldY} for {placement.Id}.");
            }
        }
    }

    /// <summary>
    /// 判断结构占地是否越过其锚点区块的至少一条边界。
    /// </summary>
    private static bool CrossesChunkBoundary(StructurePlacement placement)
    {
        long x = GridMath.PositiveMod(placement.X, WorldMetrics.ChunkTiles);
        long y = GridMath.PositiveMod(placement.Y, WorldMetrics.ChunkTiles);
        int radius = placement.FootprintRadius;
        return x < radius || y < radius ||
            x + radius >= WorldMetrics.ChunkTiles || y + radius >= WorldMetrics.ChunkTiles;
    }

    /// <summary>
    /// 将模板契约失败转换为包含具体结构与坐标的测试异常。
    /// </summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
