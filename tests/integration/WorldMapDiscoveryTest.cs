using System.Reflection;
using Godot;
using TouhouWuxiaSurvivor.Demo;
using TouhouWuxiaSurvivor.Tests.Support;
using TouhouWuxiaSurvivor.Ui.Map;
using TouhouWuxiaSurvivor.World.Biomes;
using TouhouWuxiaSurvivor.World.Coordinates;
using TouhouWuxiaSurvivor.World.Generation;
using TouhouWuxiaSurvivor.World.Map;
using TouhouWuxiaSurvivor.World.Streaming;
using TouhouWuxiaSurvivor.World.Structures;
using TouhouWuxiaSurvivor.World.Tiles;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 验证旅行地图把区块生成、玩家探索、结构发现和语义缩放保持为四个独立契约。
/// </summary>
public partial class WorldMapDiscoveryTest : Node
{
    /// <summary>
    /// 先运行无场景依赖的数据测试，再实例化正式游戏场景验证首帧揭图与地图交互。
    /// </summary>
    public override async void _Ready()
    {
        WorldDemo? demo = null;
        try
        {
            VerifyCircularRevealAndSemantics();
            VerifyLateGeneratedChunkReveal();
            VerifyDiscoveredStructureStore();
            VerifySemanticZoomBounds();
            demo = await InstantiateDemoAsync();
            await VerifyFormalSceneAsync(demo);
            GD.Print("World map discovery test passed.");
            await WorldDemoTestCleanup.FreeAsync(this, demo);
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Paused = false;
            if (demo is not null && IsInstanceValid(demo))
            {
                await WorldDemoTestCleanup.FreeAsync(this, demo);
            }

            GetTree().Quit(1);
        }
    }

    /// <summary>
    /// 证明登记生成结果不会泄露地图，并验证负坐标圆形边缘、方形角落及 Tile/群系语义。
    /// </summary>
    private static void VerifyCircularRevealAndSemantics()
    {
        var store = new ExploredMapStore(8);
        GeneratedChunk chunk = CreateChunk(
            new ChunkCoordinate(-1, -1), TileId.BambooFloorLeaves, BiomeId.BambooForest);
        store.RememberGenerated(chunk);
        Require(!store.TryGetCell(-4, -4, out _),
            "RememberGenerated exposed a tile before the player revealed it.");

        int revealed = store.RevealAround(-4, -4, 5);
        Require(revealed > 0, "Negative-coordinate reveal did not expose any stored tiles.");
        Require(store.TryGetCell(-4, -4, out ExploredMapCell center),
            "Negative-coordinate reveal did not expose its center.");
        Require(center.Tile == TileId.BambooFloorLeaves && center.Biome == BiomeId.BambooForest,
            "Explored cell lost the generated Tile or biome semantic.");
        Require(store.TryGetTile(-9, -4, out _),
            "A point on the circular reveal edge was not visible.");
        Require(!store.TryGetTile(-9, -9, out _),
            "The square corner outside the reveal circle became visible.");
    }

    /// <summary>
    /// 先记录视野再登记相交区块，确认异步流送不会让玩家身边留下永久地图缺口。
    /// </summary>
    private static void VerifyLateGeneratedChunkReveal()
    {
        var store = new ExploredMapStore(4);
        store.RevealAround(-1, 0, 3);
        store.RememberGenerated(CreateChunk(
            new ChunkCoordinate(-1, 0), TileId.MossDots, BiomeId.MagicForest));

        Require(store.TryGetBiome(-1, 0, out BiomeId center) && center == BiomeId.MagicForest,
            "A late-generated chunk did not inherit the most recent reveal circle.");
        Require(store.TryGetTile(-4, 0, out _),
            "The late-generated chunk omitted its exact circular edge.");
        Require(!store.TryGetTile(-5, 0, out _),
            "The late-generated chunk revealed a tile beyond the saved vision radius.");
    }

    /// <summary>
    /// 仅登记一个结构实例并检查范围查询与稳定身份去重，防止地图重新推导未发现地标。
    /// </summary>
    private static void VerifyDiscoveredStructureStore()
    {
        var store = new DiscoveredStructureStore();
        var discovered = new StructurePlacement(StructureId.HakureiShrine, 0, 0);
        var hidden = new StructurePlacement(StructureId.Crossroads, 12, 5);
        Require(store.Discover(discovered), "The first structure discovery was rejected.");
        Require(!store.Discover(discovered), "A repeated structure discovery was counted twice.");
        IReadOnlyList<StructurePlacement> visible = store.FindInBounds(-20, -20, 20, 20);
        Require(visible.Count == 1 && visible[0].InstanceId == discovered.InstanceId,
            "Structure map returned an instance that was never discovered.");
        Require(!store.Contains(hidden.InstanceId),
            "An undiscovered structure appeared in the discovered-instance set.");
    }

    /// <summary>
    /// 遍历全部缩放级别，确认远景按多 Tile 聚合且纹理分辨率不会反向膨胀超过视口。
    /// </summary>
    private static void VerifySemanticZoomBounds()
    {
        var view = new MapViewState();
        while (view.ChangeZoom(-1))
        {
        }

        MapRenderScale far = view.Scale;
        Require(far.TilesPerSample > 1 && far.PixelsPerSample == 1,
            "The far map level is not aggregating multiple world tiles per pixel.");
        var builder = new WorldMapTextureBuilder();
        var viewport = new Vector2(320, 180);
        builder.Rebuild(new ExploredMapStore(1), 0, 0, viewport, far);
        Require(builder.Width <= viewport.X && builder.Height <= viewport.Y,
            "Far zoom expanded the texture beyond viewport pixel dimensions.");

        while (view.ChangeZoom(1))
        {
        }

        Require(view.Scale.TilesPerSample == 1 && view.Scale.PixelsPerSample > 1,
            "The nearest map level did not preserve crisp enlarged Tile pixels.");
    }

    /// <summary>
    /// 加载正式 WorldDemo 并等待一个处理帧，让 Canvas 尺寸和首帧探索状态稳定后返回。
    /// </summary>
    private async Task<WorldDemo> InstantiateDemoAsync()
    {
        PackedScene scene = GD.Load<PackedScene>("res://src/demo/WorldDemo.tscn");
        var demo = scene.Instantiate<WorldDemo>();
        demo.PersistMetaProgression = false;
        AddChild(demo);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        return demo;
    }

    /// <summary>
    /// 验证 Prime 仅同步中心 3×3、外围按预算补齐且加载全过程都不会扩大玩家真实探索范围。
    /// </summary>
    private async Task VerifyFormalSceneAsync(WorldDemo demo)
    {
        FieldInfo? field = typeof(WorldDemo).GetField("_streamer",
            BindingFlags.Instance | BindingFlags.NonPublic);
        var streamer = field?.GetValue(demo) as ChunkStreamer;
        Require(streamer is not null, "Formal WorldDemo did not create its chunk streamer.");
        if (streamer is null)
        {
            return;
        }

        Require(streamer.ActiveCount >= 9 && streamer.ActiveCount < 25 && streamer.PendingCount > 0,
            $"Formal WorldDemo did not stage its 3x3 synchronous Prime window: " +
            $"active={streamer.ActiveCount}, pending={streamer.PendingCount}.");
        Require(streamer.ExploredMap.RevealedTileCount > 0 &&
            streamer.ExploredMap.RevealedTileCount < 4L * WorldMetrics.ChunkTiles * WorldMetrics.ChunkTiles,
            "Prime exposed too much of its loading window as explored terrain.");

        for (int frame = 0; frame < 8 && streamer.ActiveCount < 25; frame++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }

        Require(streamer.ActiveCount == 25 && streamer.PendingCount == 0,
            $"Budgeted streaming did not finish the 5x5 window: " +
            $"active={streamer.ActiveCount}, pending={streamer.PendingCount}.");
        Require(streamer.ExploredMap.RevealedTileCount <
            4L * WorldMetrics.ChunkTiles * WorldMetrics.ChunkTiles,
            "Background chunk completion incorrectly expanded the explored area.");

        WorldMapOverlay map = demo.GetNode<WorldMapOverlay>("MapLayer/WorldMapOverlay");
        map.Open();
        Require(map.Visible && map.HasRenderedTexture, "Formal map opened without a rendered texture.");
        using var motion = new InputEventMouseMotion { Position = map.Size * 0.5f };
        map._GuiInput(motion);
        Require(map.VisibleBiomeLabelCount == 1,
            "The formal map did not resolve the hovered saved-biome semantic.");
        map.Close();
        Require(!GetTree().Paused, "Closing the formal map did not restore the running tree.");
    }

    /// <summary>
    /// 构造内容统一的完整区块，使测试坐标变化只验证探索逻辑而不引入生成噪声。
    /// </summary>
    private static GeneratedChunk CreateChunk(
        ChunkCoordinate coordinate,
        TileId tile,
        BiomeId biome)
    {
        var chunk = new GeneratedChunk(coordinate);
        for (int y = 0; y < WorldMetrics.ChunkTiles; y++)
        {
            for (int x = 0; x < WorldMetrics.ChunkTiles; x++)
            {
                chunk.Set(x, y, tile);
                chunk.SetBiome(x, y, biome);
            }
        }

        return chunk;
    }

    /// <summary>
    /// 将布尔契约失败转换成包含原因的异常，供 Godot 无头进程返回非零状态。
    /// </summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
