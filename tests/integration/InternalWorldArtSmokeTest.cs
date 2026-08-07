using Godot;
using TouhouWuxiaSurvivor.Actors.Player;
using TouhouWuxiaSurvivor.Demo;
using TouhouWuxiaSurvivor.Gameplay.Spawning;

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
            world.QueueFree();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
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
