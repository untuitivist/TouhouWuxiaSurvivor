using TouhouWuxiaSurvivor.World.Biomes;
using TouhouWuxiaSurvivor.Content;
using TouhouWuxiaSurvivor.World.Coordinates;
using TouhouWuxiaSurvivor.World.Generation;
using TouhouWuxiaSurvivor.World.Map;
using TouhouWuxiaSurvivor.World.Structures;
using TouhouWuxiaSurvivor.World.Tiles;
using System.Buffers.Binary;

namespace TouhouWuxiaSurvivor.Tools.WorldVerifier;

/// <summary>
/// 在不启动 Godot 场景的情况下验证无限世界数学、确定性、群系、结构和 Tile 资源契约。
/// </summary>
internal static class Program
{
    private const ulong Seed = 20260728;

    /// <summary>
    /// 依次运行全部世界验证；契约失败时打印原因并返回非零退出码。
    /// </summary>
    private static int Main()
    {
        try
        {
            VerifyFloorDivision();
            VerifyDeterminism();
            VerifyFarCoordinates();
            VerifyBiomeCoverage();
            OfficialContentLeakVerifier.Verify(Seed);
            VerifySpawnStructure();
            VerifyBaseStructureCoverage();
            VerifyExploredMapStore();
            VerifyTileAssets();
            Console.WriteLine("World verification passed.");
            return 0;
        }
        catch (InvalidOperationException exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    /// <summary>
    /// 验证正负坐标和区块边界均采用向负无穷取整。
    /// </summary>
    private static void VerifyFloorDivision()
    {
        Require(GridMath.FloorDiv(31, 32) == 0, "Positive floor division failed.");
        Require(GridMath.FloorDiv(32, 32) == 1, "Positive boundary failed.");
        Require(GridMath.FloorDiv(-1, 32) == -1, "Negative floor division failed.");
        Require(GridMath.FloorDiv(-32, 32) == -1, "Negative boundary failed.");
        Require(GridMath.FloorDiv(-33, 32) == -2, "Negative overflow failed.");
    }

    /// <summary>
    /// 对近处、负坐标和十亿区块距离样本重复生成并比较完整 Tile 摘要。
    /// </summary>
    private static void VerifyDeterminism()
    {
        var generator = new WorldGenerator(Seed);
        ChunkCoordinate[] samples =
        [
            new(0, 0),
            new(-1, -1),
            new(43, -27),
            new(1_000_000_000, -1_000_000_000)
        ];

        foreach (ChunkCoordinate coordinate in samples)
        {
            ulong first = Digest(generator.Generate(coordinate));
            ulong second = Digest(generator.Generate(coordinate));
            Require(first == second, $"Chunk {coordinate} is not deterministic.");
        }
    }

    /// <summary>
    /// 在接近 long 可用范围的坐标生成区块，确认坐标算法不会产生无效 TileId。
    /// </summary>
    private static void VerifyFarCoordinates()
    {
        var generator = new WorldGenerator(Seed);
        var coordinate = new ChunkCoordinate(long.MaxValue / 64, long.MinValue / 64);
        GeneratedChunk chunk = generator.Generate(coordinate);
        Require(Enum.IsDefined(chunk.Get(31, 31)), "Far coordinate created an invalid tile.");
    }

    /// <summary>
    /// 在大范围采样中确认五种本体群系完整，且正作专属的雾之湖与迷途竹林不会泄漏。
    /// </summary>
    private static void VerifyBiomeCoverage()
    {
        var baseSelector = new BiomeSelector(Seed, ContentPackSelection.BaseOnly);
        var eosdSelector = new BiomeSelector(
            Seed,
            new ContentPackSelection([ContentPackIds.EmbodimentOfScarletDevil]));
        var baseFound = new HashSet<BiomeId>();
        var eosdFound = new HashSet<BiomeId>();
        for (long y = -2200; y <= 2200; y += 73)
        {
            for (long x = -2200; x <= 2200; x += 73)
            {
                baseFound.Add(baseSelector.Select(x, y));
                eosdFound.Add(eosdSelector.Select(x, y));
            }
        }

        BiomeId[] expectedBase =
        [
            BiomeId.Common,
            BiomeId.HakureiShrine,
            BiomeId.HumanVillage,
            BiomeId.MagicForest,
            BiomeId.YoukaiMountain,
        ];
        Require(expectedBase.All(baseFound.Contains),
            $"Base biomes are incomplete: {string.Join(", ", baseFound)}");
        Require(!baseFound.Contains(BiomeId.MistyLake) &&
            !baseFound.Contains(BiomeId.BambooForest),
            $"Base world leaked package biomes: {string.Join(", ", baseFound)}");
        Require(eosdFound.Contains(BiomeId.MistyLake),
            "TH06 selection did not produce Misty Lake.");
    }

    /// <summary>
    /// 验证出生区块固定神社的中心道路与结界边缘没有被生成规则破坏。
    /// </summary>
    private static void VerifySpawnStructure()
    {
        var generator = new WorldGenerator(Seed);
        GeneratedChunk spawn = generator.Generate(new ChunkCoordinate(0, 0));
        Require(spawn.Get(0, 0) == TileId.ShrinePathPebbles,
            "Spawn shrine court is missing its central path.");
        Require(spawn.Get(10, 10) == TileId.BoundarySoilSparkles,
            "Spawn shrine court boundary is missing.");
    }

    /// <summary>
    /// 在大范围结构网格中确认六类本体地标都有实例，同时没有正作专属结构。
    /// </summary>
    private static void VerifyBaseStructureCoverage()
    {
        var biomes = new BiomeSelector(Seed, ContentPackSelection.BaseOnly);
        var locator = new StructureLocator(Seed, biomes);
        HashSet<StructureId> found = locator.FindInBounds(-6000, -6000, 6000, 6000)
            .Select(item => item.Id)
            .ToHashSet();
        StructureId[] expected =
        [
            StructureId.HakureiShrine,
            StructureId.ShrineCourt,
            StructureId.HumanVillage,
            StructureId.MagicCircle,
            StructureId.MountainTerrace,
            StructureId.Crossroads,
        ];
        Require(expected.All(found.Contains),
            $"Base structures are incomplete: {string.Join(", ", found)}");
        Require(!found.Contains(StructureId.LakeIsland) &&
            !found.Contains(StructureId.ScarletDevilMansion) &&
            !found.Contains(StructureId.BambooTrail),
            "Base structure grid leaked official-game landmarks.");
    }

    /// <summary>
    /// 验证探索地图保存真实结构 Tile、正确换算负坐标，并按配置容量淘汰最早区块。
    /// </summary>
    private static void VerifyExploredMapStore()
    {
        var generator = new WorldGenerator(Seed);
        var exploredMap = new ExploredMapStore(1);
        GeneratedChunk spawn = generator.Generate(new ChunkCoordinate(0, 0));
        exploredMap.Remember(spawn);
        Require(exploredMap.TryGetTile(10, 10, out TileId structureTile) &&
            structureTile == TileId.BoundarySoilSparkles,
            "Explored map did not preserve structure tile detail.");

        GeneratedChunk negative = generator.Generate(new ChunkCoordinate(-1, -1));
        exploredMap.Remember(negative);
        Require(!exploredMap.ContainsChunk(new ChunkCoordinate(0, 0)),
            "Explored map did not enforce its chunk capacity.");
        Require(exploredMap.TryGetTile(-1, -1, out TileId negativeTile) &&
            negativeTile == negative.Get(31, 31),
            "Explored map negative coordinate lookup failed.");
    }

    /// <summary>
    /// 检查每个 TileId 的资源存在、具有 PNG 签名并严格符合统一 Tile 尺寸。
    /// </summary>
    private static void VerifyTileAssets()
    {
        string projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        foreach (TileId tile in Enum.GetValues<TileId>())
        {
            string resourcePath = TileCatalog.GetResourcePath(tile);
            string filePath = Path.Combine(
                projectRoot,
                resourcePath.Replace("res://", string.Empty).Replace('/', Path.DirectorySeparatorChar));
            Require(File.Exists(filePath), $"Missing tile asset: {resourcePath}");

            byte[] header = File.ReadAllBytes(filePath)[..24];
            Require(header.AsSpan(0, 8).SequenceEqual(new byte[]
                { 137, 80, 78, 71, 13, 10, 26, 10 }), $"Invalid PNG: {resourcePath}");
            int width = BinaryPrimitives.ReadInt32BigEndian(header.AsSpan(16, 4));
            int height = BinaryPrimitives.ReadInt32BigEndian(header.AsSpan(20, 4));
            Require(width == WorldMetrics.TilePixels && height == WorldMetrics.TilePixels,
                $"Unexpected tile size {width}x{height}: {resourcePath}");
        }
    }

    /// <summary>
    /// 使用 FNV-1a 顺序汇总区块所有 TileId，得到适合确定性比较的轻量摘要。
    /// </summary>
    private static ulong Digest(GeneratedChunk chunk)
    {
        ulong digest = 14695981039346656037UL;
        for (int y = 0; y < WorldMetrics.ChunkTiles; y++)
        {
            for (int x = 0; x < WorldMetrics.ChunkTiles; x++)
            {
                digest ^= (byte)chunk.Get(x, y);
                digest *= 1099511628211UL;
            }
        }

        return digest;
    }

    /// <summary>
    /// 在验证条件失败时抛出带具体原因的异常，由主入口统一转换为退出码。
    /// </summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
