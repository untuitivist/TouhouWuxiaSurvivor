using Godot;
using TouhouWuxiaSurvivor.Content;
using TouhouWuxiaSurvivor.World.Biomes;
using TouhouWuxiaSurvivor.World.Coordinates;
using TouhouWuxiaSurvivor.World.Generation;
using TouhouWuxiaSurvivor.World.Regions;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 验证无限世界宏域的确定性、连续边界、同作三层关联，以及区块群系语义存储契约。
/// </summary>
public partial class WorldGeographySmokeTest : Node
{
    private const ulong Seed = 20260812;

    /// <summary>
    /// 依次执行纯数据验证；任一空间契约失败都会让无头测试返回非零状态。
    /// </summary>
    public override void _Ready()
    {
        try
        {
            VerifyDeterminism();
            VerifyBoundaryContinuity();
            VerifyRelatedOfficialLayers();
            VerifyChunkBiomeStorage();
            GD.Print("World geography smoke test passed.");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    /// <summary>
    /// 用不同内容输入顺序和正负绝对坐标验证同种子结果完全相同且不依赖加载顺序。
    /// </summary>
    private static void VerifyDeterminism()
    {
        string first = ContentPackIds.EmbodimentOfScarletDevil;
        string second = ContentPackIds.ImperishableNight;
        var plannerA = new WorldRegionPlanner(Seed, new ContentPackSelection([first, second]));
        var plannerB = new WorldRegionPlanner(Seed, new ContentPackSelection([second, first]));
        (long X, long Y)[] samples =
        [
            (-4097, -3073), (-385, -384), (-1, -1), (0, 0),
            (383, 384), (4096, -8193), (12000, 7001),
        ];
        foreach ((long x, long y) in samples)
        {
            Require(plannerA.Sample(x, y) == plannerA.Sample(x, y),
                $"Repeated sample changed at {x},{y}.");
            Require(plannerA.Sample(x, y) == plannerB.Sample(x, y),
                $"Content input order changed geography at {x},{y}.");
        }
    }

    /// <summary>
    /// 搜索跨越规划网格线但仍归属同一 Voronoi 站点的坐标，证明边界不再被网格硬裁切。
    /// </summary>
    private static void VerifyBoundaryContinuity()
    {
        var planner = new WorldRegionPlanner(Seed, new ContentPackSelection(
            [ContentPackIds.EmbodimentOfScarletDevil, ContentPackIds.PerfectCherryBlossom]));
        bool crossesCellLine = false;
        for (long cell = -5; cell <= 5 && !crossesCellLine; cell++)
        {
            long boundary = cell * WorldRegionPlanner.CellSize;
            for (long axis = -1800; axis <= 1800; axis += 7)
            {
                WorldRegionSample left = planner.Sample(boundary - 1, axis);
                WorldRegionSample right = planner.Sample(boundary, axis);
                WorldRegionSample top = planner.Sample(axis, boundary - 1);
                WorldRegionSample bottom = planner.Sample(axis, boundary);
                crossesCellLine = SameSite(left, right) || SameSite(top, bottom);
                if (crossesCellLine)
                {
                    break;
                }
            }
        }

        Require(crossesCellLine, "No macro region crossed a planner cell boundary.");
    }

    /// <summary>
    /// 在单作世界中寻找一个正作宏域，并确认外围、内部、核心共享同一站点与作品来源。
    /// </summary>
    private static void VerifyRelatedOfficialLayers()
    {
        string packId = ContentPackIds.EmbodimentOfScarletDevil;
        var planner = new WorldRegionPlanner(
            Seed, new ContentPackSelection([packId]));
        WorldRegionSample anchor = FindOfficialAnchor(planner);
        var layers = new HashSet<WorldRegionLayer>();
        for (long y = anchor.Site.CenterY - 260; y <= anchor.Site.CenterY + 260; y += 4)
        {
            for (long x = anchor.Site.CenterX - 260; x <= anchor.Site.CenterX + 260; x += 4)
            {
                WorldRegionSample sample = planner.Sample(x, y);
                if (sample.IsOfficial && sample.PackId == packId && SameSite(sample, anchor))
                {
                    layers.Add(sample.Layer);
                }
            }
        }

        Require(layers.SetEquals(Enum.GetValues<WorldRegionLayer>()),
            "An official domain did not contain related outer, inner and core regions.");
    }

    /// <summary>
    /// 逐格比较区块存储语义与群系选择器，防止地图和渲染退回重复推导或读到默认值。
    /// </summary>
    private static void VerifyChunkBiomeStorage()
    {
        var selection = new ContentPackSelection([ContentPackIds.ImperishableNight]);
        var generator = new WorldGenerator(Seed, selection);
        var coordinate = new ChunkCoordinate(-3, 2);
        GeneratedChunk chunk = generator.Generate(coordinate);
        long originX = coordinate.X * WorldMetrics.ChunkTiles;
        long originY = coordinate.Y * WorldMetrics.ChunkTiles;
        for (int y = 0; y < WorldMetrics.ChunkTiles; y++)
        {
            for (int x = 0; x < WorldMetrics.ChunkTiles; x++)
            {
                Require(chunk.GetBiome(x, y) == generator.Biomes.Select(originX + x, originY + y),
                    $"Stored biome diverged at local tile {x},{y}.");
            }
        }
    }

    /// <summary>
    /// 枚举有限站点寻找其中心附近确实归属正作的稳定测试锚点。
    /// </summary>
    private static WorldRegionSample FindOfficialAnchor(WorldRegionPlanner planner)
    {
        for (long cellY = -8; cellY <= 8; cellY++)
        {
            for (long cellX = -8; cellX <= 8; cellX++)
            {
                WorldRegionSite site = planner.CreateSite(cellX, cellY);
                WorldRegionSample sample = planner.Sample(site.CenterX, site.CenterY);
                if (sample.IsOfficial && SameSite(sample, site))
                {
                    return sample;
                }
            }
        }

        throw new InvalidOperationException("Could not find an official macro-region anchor.");
    }

    /// <summary>
    /// 比较两个采样结果是否由同一绝对宏域站点拥有。
    /// </summary>
    private static bool SameSite(WorldRegionSample first, WorldRegionSample second) =>
        SameSite(first, second.Site);

    /// <summary>
    /// 比较采样结果与指定站点的网格身份，避免浮点中心位置参与相等判断。
    /// </summary>
    private static bool SameSite(WorldRegionSample sample, WorldRegionSite site) =>
        sample.Site.CellX == site.CellX && sample.Site.CellY == site.CellY;

    /// <summary>
    /// 将空间契约失败转为具备具体原因的异常，便于无头测试定位。
    /// </summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
