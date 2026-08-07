using Godot;
using TouhouWuxiaSurvivor.Actors.Enemies;
using TouhouWuxiaSurvivor.Actors.Player;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 在真实世界场景中排列九类本体敌人并保存截图，验证局内像素尺寸、遮挡和最近邻表现。
/// </summary>
public partial class GameplayEnemyVisualTest : Node
{
    /// <summary>
    /// 加载正式世界、在玩家周围生成九类敌人、验证精灵启用并于普通渲染器下保存截图。
    /// </summary>
    public override async void _Ready()
    {
        try
        {
            Node world = GD.Load<PackedScene>("res://src/demo/WorldDemo.tscn").Instantiate();
            AddChild(world);
            var player = world.GetNode<PlayerController>("Player");
            var container = world.GetNode<Node2D>("CombatEntities/Enemies");
            PackedScene enemyScene = GD.Load<PackedScene>("res://src/actors/enemies/EnemyActor.tscn");
            EnemyDefinition[] baseEnemies = EnemyCatalog.All
                .Where(definition => definition.RequiredContentPack is null)
                .Take(9)
                .ToArray();
            Require(baseEnemies.Length == 9, $"Expected 9 base enemies, found {baseEnemies.Length}.");
            for (int index = 0; index < baseEnemies.Length; index++)
            {
                EnemyActor enemy = enemyScene.Instantiate<EnemyActor>();
                enemy.Configure(baseEnemies[index], player);
                container.AddChild(enemy);
                enemy.GlobalPosition = player.GlobalPosition + GetDisplayOffset(index);
                Require(enemy.GetNode<EnemyVisualController>("Visual").UsesSprite,
                    $"Gameplay visual is not active for {baseEnemies[index].DisplayName}.");
            }

            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            SaveScreenshot();
            GD.Print("Gameplay enemy visual test passed.");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    /// <summary>
    /// 返回避开玩家中心的上下两排布局，确保不同尺寸敌人在同一镜头内可比较且不重叠。
    /// </summary>
    private static Vector2 GetDisplayOffset(int index)
    {
        if (index < 5)
        {
            return new Vector2((index - 2) * 58.0f, -58.0f);
        }

        return new Vector2((index - 6.5f) * 70.0f, 58.0f);
    }

    /// <summary>
    /// 普通渲染器保存真实世界截图；headless 环境只执行节点和视觉状态断言。
    /// </summary>
    private void SaveScreenshot()
    {
        if (DisplayServer.GetName() == "headless")
        {
            return;
        }

        Image image = GetViewport().GetTexture().GetImage();
        string path = ProjectSettings.GlobalizePath("user://gameplay-enemy-visuals-640x360.png");
        Require(image.SavePng(path) == Error.Ok, $"Could not save gameplay screenshot: {path}.");
        GD.Print($"Gameplay screenshot: {path}");
    }

    /// <summary>
    /// 将实际世界视觉失败转换为包含具体原因的测试异常。
    /// </summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
