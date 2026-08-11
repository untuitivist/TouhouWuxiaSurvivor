using Godot;
using TouhouWuxiaSurvivor.Actors.Enemies;
using TouhouWuxiaSurvivor.Actors.Pickups;
using TouhouWuxiaSurvivor.Actors.Player;
using TouhouWuxiaSurvivor.Ecs.Combat;
using TouhouWuxiaSurvivor.Tests.Support;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 在真实世界中排列 ECS 敌人、道具、灵息与弹幕并保存截图，验证正式批量视觉链。
/// </summary>
public partial class GameplayEnemyVisualTest : Node
{
    /// <summary>
    /// 加载正式世界、在玩家周围生成九类敌人与原作道具，并于普通渲染器下保存截图。
    /// </summary>
    public override async void _Ready()
    {
        try
        {
            Node world = GD.Load<PackedScene>("res://src/demo/WorldDemo.tscn").Instantiate();
            AddChild(world);
            var player = world.GetNode<PlayerController>("Player");
            var ecs = world.GetNode<EcsCombatWorld>("CombatEntities/EcsCombatWorld");
            EnemyDefinition[] baseEnemies = EnemyCatalog.All
                .Where(definition => definition.RequiredContentPack is null)
                .Take(9)
                .ToArray();
            Require(baseEnemies.Length == 9, $"Expected 9 base enemies, found {baseEnemies.Length}.");
            for (int index = 0; index < baseEnemies.Length; index++)
            {
                ecs.SpawnEnemy(player.GlobalPosition + GetDisplayOffset(index), baseEnemies[index]);
            }
            AddOriginalItems(ecs, player.GlobalPosition);

            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Require(ecs.MappedEnemyVisualCount >= 9,
                "Formal ECS did not draw all nine base enemy actor strips.");
            Require(ecs.PickupIconVisualCount >= 3 && ecs.SpiritIconVisualCount >= 1 &&
                ecs.ProjectileIconVisualCount >= 1,
                "Formal ECS original item/bullet visuals were not all active.");
            SaveScreenshot();
            GD.Print("Gameplay enemy visual test passed.");
            await WorldDemoTestCleanup.FreeAsync(this, world);
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    /// <summary>
    /// 将三种构筑强化、灵息和静止弹幕布置在玩家下方，便于一次截图核对图标裁切与区分度。
    /// </summary>
    private static void AddOriginalItems(EcsCombatWorld ecs, Vector2 origin)
    {
        ecs.SpawnPickup(PickupKind.MoveSpeed, origin + new Vector2(-90.0f, 110.0f));
        ecs.SpawnPickup(PickupKind.RapidFire, origin + new Vector2(-30.0f, 110.0f));
        ecs.SpawnPickup(PickupKind.SpiralShot, origin + new Vector2(30.0f, 110.0f));
        ecs.SpawnSpirit(origin + new Vector2(90.0f, 110.0f), 3);
        ecs.SpawnProjectile(origin + new Vector2(130.0f, 110.0f), Vector2.Right, 0.0f, 1);
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
