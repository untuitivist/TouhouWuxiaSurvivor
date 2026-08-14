using Godot;
using TouhouWuxiaSurvivor.Demo;
using TouhouWuxiaSurvivor.Gameplay.Spawning;
using TouhouWuxiaSurvivor.Tests.Support;
using TouhouWuxiaSurvivor.Ui.Map;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 在普通 Windows 渲染器中打开正式旅行地图，保存真实探索、地标和鼠标群系标签的验收截图。
/// </summary>
public partial class WorldMapVisualAcceptanceTest : Node
{
    /// <summary>
    /// 实例化正式世界、等待分帧区块加载完成、打开地图并把指针放到玩家附近后捕获画面。
    /// </summary>
    public override async void _Ready()
    {
        WorldDemo? world = null;
        int exitCode = 0;
        try
        {
            GetWindow().Size = new Vector2I(1280, 720);
            world = GD.Load<PackedScene>("res://src/demo/WorldDemo.tscn")
                .Instantiate<WorldDemo>();
            world.PersistMetaProgression = false;
            world.GetNode<EnemySpawner>("EnemySpawner").InitialSpawnCount = 0;
            AddChild(world);
            await WaitForFrames(8);

            var map = world.GetNode<WorldMapOverlay>("MapLayer/WorldMapOverlay");
            map.Open();
            using var pointer = new InputEventMouseMotion { Position = map.Size * 0.5f };
            map._GuiInput(pointer);
            await WaitForFrames(2);
            Require(map.Visible && map.HasRenderedTexture,
                "Formal travel map did not become visible with a rendered texture.");
            Require(map.VisibleBiomeLabelCount == 1 && map.VisibleStructureLabelCount > 0,
                "Formal travel map omitted its pointer biome label or discovered structure label.");
            SaveScreenshot("visual-world-map-1280x720.png", 1280, 720);
            GD.Print("World map visual acceptance test passed.");
        }
        catch (Exception exception)
        {
            exitCode = 1;
            GD.PushError(exception.ToString());
        }
        finally
        {
            GetTree().Paused = false;
            if (world is not null && GodotObject.IsInstanceValid(world))
            {
                await WorldDemoTestCleanup.FreeAsync(this, world);
            }

            GetTree().Quit(exitCode);
        }
    }

    /// <summary>等待指定处理帧，使区块流送、地图布局和 Canvas 绘制形成稳定画面。</summary>
    private async Task WaitForFrames(int count)
    {
        for (int frame = 0; frame < count; frame++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
    }

    /// <summary>保存最近邻缩放后的视口 PNG；无窗口回归只执行布局与数据断言。</summary>
    private void SaveScreenshot(string fileName, int width, int height)
    {
        if (DisplayServer.GetName() == "headless")
        {
            GD.Print($"Visual screenshot skipped in headless mode: {fileName}");
            return;
        }

        Image image = GetViewport().GetTexture().GetImage();
        if (image.GetWidth() != width || image.GetHeight() != height)
        {
            image.Resize(width, height, Image.Interpolation.Nearest);
        }

        string path = ProjectSettings.GlobalizePath("user://" + fileName);
        Require(image.SavePng(path) == Error.Ok, $"Could not save screenshot: {path}.");
        GD.Print($"World map visual acceptance screenshot: {path} ({width}x{height})");
    }

    /// <summary>将视觉验收失败转换为带有具体原因的测试异常。</summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
