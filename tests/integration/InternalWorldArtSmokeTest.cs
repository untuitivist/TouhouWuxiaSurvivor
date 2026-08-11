using Godot;
using TouhouWuxiaSurvivor.Actors.Player;
using TouhouWuxiaSurvivor.Demo;
using TouhouWuxiaSurvivor.Gameplay.Spawning;
using TouhouWuxiaSurvivor.Tests.Support;
using TouhouWuxiaSurvivor.Visuals.Internal;
using TouhouWuxiaSurvivor.World.Biomes;
using TouhouWuxiaSurvivor.World.Rendering;
using TouhouWuxiaSurvivor.World.Structures;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 验证内部原作素材进入正式世界、结构和玩家节点，而不是只显示在图鉴或独立预览框中。
/// </summary>
public partial class InternalWorldArtSmokeTest : Node
{
    /// <summary>
    /// 实例化真实游戏场景并检查地区格、结构精灵、灵梦视觉与无边框声明的局内契约。
    /// </summary>
    public override async void _Ready()
    {
        try
        {
            VerifyEveryBiomeAndStructure();
            var world = GD.Load<PackedScene>("res://src/demo/WorldDemo.tscn")
                .Instantiate<WorldDemo>();
            world.PersistMetaProgression = false;
            world.GetNode<EnemySpawner>("EnemySpawner").InitialSpawnCount = 0;
            AddChild(world);

            var biomeLayer = world.GetNode<TileMapLayer>("InternalBiomeGround");
            var structures = world.GetNode<Node2D>("InternalStructures");
            var playerVisual = world.GetNode<PlayerVisualController>("Player/Visual");
            CanvasLayer hud = world.GetNode<CanvasLayer>("WorldDebugHud");
            Label notice = hud.GetNode<Label>("InternalAssetNotice");

            Require(biomeLayer.GetUsedCells().Count > 0,
                "Internal biome scenes were not painted into the gameplay map.");
            Require(structures.GetChildCount() > 0 &&
                structures.GetChildren().All(node => node is Sprite2D sprite && sprite.Texture is not null),
                "Internal structure scenes were not placed at gameplay structure anchors.");
            Require(playerVisual.UsesSprite &&
                playerVisual.GetNode<Sprite2D>("Sprite").Texture is not null,
                "Reimu internal art did not replace the gameplay text placeholder.");
            Require(!hud.HasNode("TravelAssets") &&
                notice.Text.Contains("内部素材", StringComparison.Ordinal) &&
                notice.GetParent() == hud,
                "Gameplay retained the preview panel or omitted the plain internal-use notice.");

            GD.Print("Internal world art smoke test passed.");
            await WorldDemoTestCleanup.FreeAsync(this, world);
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
    /// 逐项走正式世界的来源解析与纹理派生入口，确认所有作品群系和结构均能生成非空像素资源。
    /// </summary>
    private static void VerifyEveryBiomeAndStructure()
    {
        var catalog = new InternalVisualCatalog();
        foreach (BiomeId biome in Enum.GetValues<BiomeId>())
        {
            string sourceId = InternalContentSourceResolver.GetSourceId(biome);
            string name = BiomeNames.GetChinese(biome);
            Texture2D texture = RequireSceneTexture(
                catalog, sourceId, InternalVisualCategory.Biome, name);
            Image atlas = InternalBiomeTextureFactory.CreateAtlas(texture).GetImage();
            Require(atlas.GetSize() == new Vector2I(32, 32) && atlas.GetUsedRect().HasArea(),
                $"Biome atlas is empty or malformed: {sourceId}/{name}.");
        }

        foreach (StructureId structure in Enum.GetValues<StructureId>())
        {
            string sourceId = InternalContentSourceResolver.GetSourceId(structure);
            string name = StructureNames.GetChinese(structure);
            Texture2D texture = RequireSceneTexture(
                catalog, sourceId, InternalVisualCategory.Structure, name);
            Image marker = InternalStructureTextureFactory.CreateMarker(texture, structure).GetImage();
            Require(marker.GetSize() == new Vector2I(128, 128) && marker.GetUsedRect().HasArea(),
                $"Structure marker is empty or malformed: {sourceId}/{name}.");
        }
    }

    /// <summary>
    /// 按正式复合键读取场景纹理，并在映射缺失、类型错误或纹理无法加载时立即给出具体身份。
    /// </summary>
    private static Texture2D RequireSceneTexture(
        InternalVisualCatalog catalog,
        string sourceId,
        InternalVisualCategory category,
        string name)
    {
        Require(catalog.TryGet(sourceId, category, name, out InternalVisualDefinition definition),
            $"World art mapping is missing: {sourceId}/{category}/{name}.");
        Require(definition.Kind == InternalVisualKind.Scene,
            $"World art mapping is not a scene: {sourceId}/{category}/{name}.");
        Require(catalog.TryGetTexture(definition, out Texture2D texture),
            $"World art texture cannot be loaded: {sourceId}/{category}/{name}.");
        return texture;
    }

    /// <summary>
    /// 将世界素材接入失败转换为包含具体原因的测试异常。
    /// </summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
